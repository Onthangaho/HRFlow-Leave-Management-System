using HRFlow.Domain.Models.Auth;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Common;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Exceptions;
using HRFlow.Domain.Interfaces.Services.Employees;
using HRFlow.Domain.Models.Employees;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRFlow.Infrastructure.Services.Employees;

/// <summary>
/// Coordinates Identity and profile writes in the same reserved SQLite transaction. Relationship and
/// last-administrator checks must run after acquiring protection, because they span multiple records.
/// </summary>
public sealed class EmployeeManagementService(
    HRFlowDbContext dbContext,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole<Guid>> roleManager,
    IEmployeeManagementTransaction writeTransaction,
    IRequestCorrelationContext correlation,
    ILeaveNotificationOutbox notifications,
    IAccountActivationService activation,
    ILogger<EmployeeManagementService> logger) : IEmployeeManagementService
{
    /// <inheritdoc />
    public async Task<EmployeeManagementResult> CreateEmployeeAsync(
        Guid actorIdentityUserId, string fullName, string email,
        Guid departmentId, IReadOnlyCollection<string> roles, Guid? managerId, string? employeeNumber, DateOnly? employmentStartDate,
        CancellationToken cancellationToken)
    {
        activation.EnsureDeliveryConfigured();
        Guid accountId = default;
        string invitation = "";
        EmployeeManagementResult? result = null;
        await writeTransaction.ExecuteAsync(async token =>
        {
            await EnsureCurrentHrAsync(actorIdentityUserId);
            var effectiveRoles = await ValidateRolesAsync(roles);
            await ValidateDepartmentAsync(departmentId, token);
            await EnsureEmailAvailableAsync(email.Trim(), null);
            await ValidateManagerAssignmentAsync(null, departmentId, managerId, token);
            var number = await ValidateEmploymentAsync(null, employeeNumber, employmentStartDate, token);

            var identity = new ApplicationUser { UserName = email.Trim(), Email = email.Trim(), RequiresActivation = true };
            EnsureIdentitySucceeded(await userManager.CreateAsync(identity));
            EnsureIdentitySucceeded(await userManager.AddToRolesAsync(identity, effectiveRoles));

            accountId = identity.Id;
            invitation = activation.Prepare(identity);
            var employee = Employee.Create(fullName, email, departmentId);
            employee.ConfirmEmployment(number, employmentStartDate!.Value);
            employee.SetIdentityUser(identity.Id.ToString());
            employee.AssignManager(managerId);
            dbContext.Employees.Add(employee);
            result = new EmployeeManagementResult { EmployeeId = employee.Id, Version = employee.Version };
        }, cancellationToken);
        logger.LogInformation("Employee created. EmployeeId: {EmployeeId}; ActorIdentityUserId: {ActorIdentityUserId}",
            result!.EmployeeId, actorIdentityUserId);
        result!.InvitationDeliveryState = await activation.DeliverAsync(accountId, invitation, cancellationToken);
        return result;
    }

    /// <inheritdoc />
    public async Task<EmployeeManagementResult> UpdateEmployeeAsync(
        Guid actorIdentityUserId, Guid employeeId, Guid expectedVersion, string fullName, string email,
        Guid departmentId, IReadOnlyCollection<string> roles, ManagerAssignmentOperation managerAssignment,
        Guid? managerId, bool confirmEmploymentFacts, string? employeeNumber, DateOnly? employmentStartDate, CancellationToken cancellationToken)
    {
        EmployeeManagementResult? result = null;
        await writeTransaction.ExecuteAsync(async token =>
        {
            await EnsureCurrentHrAsync(actorIdentityUserId);
            var employee = await dbContext.Employees.SingleOrDefaultAsync(e => e.Id == employeeId, token)
                ?? throw new NotFoundException("Employee was not found.");
            if (employee.Version != expectedVersion)
            {
                throw new WriteConflictException("This employee changed since you opened the form. Reload the record before saving.");
            }
            var effectiveRoles = await ValidateRolesAsync(roles);
            await ValidateDepartmentAsync(departmentId, token);
            if (!employee.IsActive) throw new WriteConflictException("Inactive employees cannot be edited or reactivated through profile editing.");
            if (confirmEmploymentFacts)
            {
                var number = await ValidateEmploymentAsync(employee.Id, employeeNumber, employmentStartDate, token);
                employee.ConfirmEmployment(number, employmentStartDate!.Value);
            }
            else if (employeeNumber is not null || employmentStartDate.HasValue)
                throw new DomainException("Employment fields require explicit ConfirmEmploymentFacts; omit them to preserve existing or Unknown facts.");
            var effectiveManager = managerAssignment switch
            {
                ManagerAssignmentOperation.Preserve when !managerId.HasValue => employee.ManagerId,
                ManagerAssignmentOperation.Clear when !managerId.HasValue => null,
                ManagerAssignmentOperation.Assign when managerId.HasValue && managerId != Guid.Empty => managerId,
                _ => throw new DomainException("Assign requires a manager ID; Preserve and Clear must not include one.")
            };
            await ValidateManagerAssignmentAsync(employee.Id, departmentId, effectiveManager, token);
            await EnsureDirectReportsRemainValidAsync(employee, departmentId, effectiveRoles, token);

            var identity = string.IsNullOrWhiteSpace(employee.IdentityUserId)
                ? null : await userManager.FindByIdAsync(employee.IdentityUserId);
            if (identity is null)
            {
                throw new WriteConflictException("This employee has no linked account. Contact support before editing.");
            }
            await EnsureEmailAvailableAsync(email.Trim(), identity.Id);
            var currentRoles = await userManager.GetRolesAsync(identity);
            if (!identity.RequiresActivation && currentRoles.Contains(EmployeeRoles.HrAdministrator)
                && !effectiveRoles.Contains(EmployeeRoles.HrAdministrator))
            {
                var hrCount = await CountActiveHrAsync(token);
                if (hrCount <= 1)
                {
                    throw new WriteConflictException("The last active HR Administrator cannot be removed.");
                }
            }

            // UserManager saves through the same scoped DbContext. Those saves remain inside this transaction.
            // Version rotates even for a role-only edit so stale role replacements cannot overwrite it.
            var previousManager = employee.ManagerId;
            employee.Update(fullName, email, departmentId);
            employee.AssignManager(effectiveManager);
            await notifications.ReassignmentAsync(employee, previousManager, actorIdentityUserId, token);
            // Changing a pending recipient invalidates the old invitation; HR must explicitly resend.
            if (identity.RequiresActivation && identity.NormalizedEmail != userManager.NormalizeEmail(email.Trim()))
            {
                identity.ActivationTokenHash = null;
                identity.ActivationExpiresAtUtc = null;
                identity.InvitationDeliveryState = ActivationDeliveryStates.DeliveryFailed;
            }
            identity.Email = email.Trim();
            identity.UserName = email.Trim();
            EnsureIdentitySucceeded(await userManager.UpdateAsync(identity));
            var removedRoles = currentRoles.Except(effectiveRoles).ToArray();
            var addedRoles = effectiveRoles.Except(currentRoles).ToArray();
            if (removedRoles.Length > 0)
            {
                EnsureIdentitySucceeded(await userManager.RemoveFromRolesAsync(identity, removedRoles));
            }
            if (addedRoles.Length > 0)
            {
                EnsureIdentitySucceeded(await userManager.AddToRolesAsync(identity, addedRoles));
            }
            result = new EmployeeManagementResult { EmployeeId = employee.Id, Version = employee.Version };
        }, cancellationToken);
        logger.LogInformation("Employee updated. EmployeeId: {EmployeeId}; ActorIdentityUserId: {ActorIdentityUserId}",
            employeeId, actorIdentityUserId);
        return result!;
    }

    /// <inheritdoc />
    public async Task<EmployeeDeactivationResult> DeactivateEmployeeAsync(Guid actorIdentityUserId,
        Guid employeeId, Guid expectedVersion, string reason, CancellationToken cancellationToken)
    {
        EmployeeDeactivationResult? result = null;
        await writeTransaction.ExecuteAsync(async token =>
        {
            await EnsureCurrentHrAsync(actorIdentityUserId);
            var actor = await dbContext.Employees.SingleAsync(e => e.IdentityUserId == actorIdentityUserId.ToString(), token);
            var employee = await dbContext.Employees.SingleOrDefaultAsync(e => e.Id == employeeId, token)
                ?? throw new NotFoundException("Employee was not found.");
            if (employee.Version != expectedVersion)
                throw new WriteConflictException("This employee changed. Reload before deactivating.");
            if (!employee.IsActive)
                throw new WriteConflictException("This employee is already inactive. No further cancellation was made.");
            if (await dbContext.Employees.AnyAsync(e => e.ManagerId == employeeId && e.IsActive, token))
                throw new WriteConflictException("Reassign all active direct reports before deactivating their manager.");
            var identity = employee.IdentityUserId is null ? null : await userManager.FindByIdAsync(employee.IdentityUserId);
            if (identity is not null && !identity.RequiresActivation && await userManager.IsInRoleAsync(identity, EmployeeRoles.HrAdministrator)
                && await CountActiveHrAsync(token) <= 1)
                throw new WriteConflictException("The last active HR Administrator cannot be deactivated.");
            var pending = await dbContext.LeaveRequests
                .Where(r => r.EmployeeId == employeeId && r.Status == HRFlow.Domain.Enums.LeaveRequestStatus.Pending)
                .ToListAsync(token);
            employee.Deactivate(actor.Id, reason);
            foreach (var request in pending)
            {
                request.Cancel(actor.Id, AuditEntry.DeactivationReasonPrefix + reason.Trim(), correlation.CorrelationId);
                await notifications.TransitionAsync(request, token);
            }
            result = new EmployeeDeactivationResult(employee.Id, employee.Version, employee.IsActive, pending.Count);
        }, cancellationToken);
        logger.LogInformation("Employee deactivated. EmployeeId: {EmployeeId}; ActorIdentityUserId: {ActorIdentityUserId}; CancelledRequestCount: {CancelledRequestCount}",
            employeeId, actorIdentityUserId, result!.CancelledRequestCount);
        return result;
    }

    private async Task<string> ValidateEmploymentAsync(Guid? employeeId, string? number, DateOnly? date, CancellationToken token)
    {
        var normalized = EmploymentFacts.NormalizeNumber(number);
        if (!date.HasValue) throw new DomainException("A confirmed employment start date is required.");
        EmploymentFacts.ValidateDate(date.Value);
        if (await dbContext.Employees.AnyAsync(e => e.Id != employeeId && e.EmployeeNumber == normalized, token))
            throw new WriteConflictException("This employee number is already assigned, including inactive records. Choose a unique number.");
        return normalized;
    }

    private async Task<int> CountActiveHrAsync(CancellationToken token)
    {
        var hrRole = await roleManager.FindByNameAsync(EmployeeRoles.HrAdministrator);
        // Materialize GUIDs before formatting: SQLite stores GUIDs uppercase but profile links use .NET formatting.
        var userIds = await dbContext.UserRoles.Where(r => r.RoleId == hrRole!.Id && dbContext.Users.Any(u => u.Id == r.UserId && !u.RequiresActivation))
            .Select(r => r.UserId).ToListAsync(token);
        var identityIds = userIds.Select(id => id.ToString()).ToArray();
        return await dbContext.Employees.CountAsync(e => e.IsActive && e.IdentityUserId != null
            && identityIds.Contains(e.IdentityUserId.ToLower()), token);
    }

    /// <inheritdoc />
    public async Task ValidateManagerAssignmentAsync(
        Guid? employeeId, Guid departmentId, Guid? managerId, CancellationToken cancellationToken)
    {
        if (!managerId.HasValue) return;
        if (managerId == Guid.Empty) throw new DomainException("Manager assignment must reference a valid employee.");
        if (employeeId == managerId) throw new DomainException("An employee cannot be assigned as their own manager.");

        var manager = await dbContext.Employees.AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == managerId, cancellationToken)
            ?? throw new DomainException("Assigned manager was not found.");
        if (!manager.IsActive) throw new DomainException("Assigned manager must be active.");
        if (manager.DepartmentId != departmentId)
        {
            throw new DomainException("Assigned manager must belong to the same department.");
        }
        var identity = string.IsNullOrWhiteSpace(manager.IdentityUserId)
            ? null : await userManager.FindByIdAsync(manager.IdentityUserId);
        if (identity is null || identity.RequiresActivation || !await userManager.IsInRoleAsync(identity, EmployeeRoles.Manager))
        {
            throw new DomainException("Assigned manager must have the current Manager role.");
        }

        var visited = new HashSet<Guid>();
        Employee? current = manager;
        while (current is not null)
        {
            if (current.Id == employeeId || !visited.Add(current.Id))
            {
                throw new DomainException("Manager assignment would create or use a reporting cycle.");
            }
            current = current.ManagerId.HasValue
                ? await dbContext.Employees.AsNoTracking()
                    .SingleOrDefaultAsync(e => e.Id == current.ManagerId, cancellationToken)
                : null;
        }
    }

    private async Task EnsureCurrentHrAsync(Guid identityId)
    {
        var actor = await userManager.FindByIdAsync(identityId.ToString());
        if (actor is null || actor.RequiresActivation || !await dbContext.Employees.AnyAsync(e => e.IdentityUserId == identityId.ToString() && e.IsActive)
            || !await userManager.IsInRoleAsync(actor, EmployeeRoles.HrAdministrator))
        {
            throw new ForbiddenException("Current HR Administrator membership is required to manage employees.");
        }
    }

    private async Task<string[]> ValidateRolesAsync(IReadOnlyCollection<string> roles)
    {
        if (roles is null || roles.Count == 0) throw new DomainException("At least one role is required.");
        var normalized = new List<string>();
        foreach (var role in roles)
        {
            var canonical = EmployeeRoles.All.SingleOrDefault(
                allowed => string.Equals(allowed, role?.Trim(), StringComparison.OrdinalIgnoreCase));
            if (canonical is null || !await roleManager.RoleExistsAsync(canonical))
            {
                throw new DomainException("Select only available Employee, Manager, or HR Administrator roles.");
            }
            if (!normalized.Contains(canonical)) normalized.Add(canonical);
        }
        return normalized.ToArray();
    }

    private async Task ValidateDepartmentAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        if (!await dbContext.Departments.AnyAsync(d => d.Id == departmentId, cancellationToken))
        {
            throw new DomainException("Department does not exist.");
        }
    }

    private async Task EnsureEmailAvailableAsync(string email, Guid? identityId)
    {
        var byEmail = await userManager.FindByEmailAsync(email);
        var byName = await userManager.FindByNameAsync(email);
        if ((byEmail is not null && byEmail.Id != identityId) || (byName is not null && byName.Id != identityId))
        {
            throw new DuplicateEmailException(email);
        }
    }

    private async Task EnsureDirectReportsRemainValidAsync(
        Employee employee, Guid departmentId, IReadOnlyCollection<string> roles, CancellationToken cancellationToken)
    {
        if (!await dbContext.Employees.AnyAsync(e => e.ManagerId == employee.Id && e.IsActive, cancellationToken)) return;
        if (employee.DepartmentId != departmentId)
        {
            throw new DomainException("Reassign all direct reports before changing this manager's department.");
        }
        if (!roles.Contains(EmployeeRoles.Manager))
        {
            throw new DomainException("Reassign all direct reports before removing the Manager role.");
        }
    }

    private static void EnsureIdentitySucceeded(IdentityResult result)
    {
        if (result.Succeeded) return;
        if (result.Errors.Any(e => e.Code is "DuplicateEmail" or "DuplicateUserName"))
        {
            throw new WriteConflictException("Email is already in use.");
        }
        if (result.Errors.Any(e => e.Code == "ConcurrencyFailure"))
        {
            throw new WriteConflictException("This account changed. Reload the employee before saving.");
        }
        throw new EmployeeManagementValidationException(string.Join(" ", result.Errors.Select(e => e.Description)));
    }
}
