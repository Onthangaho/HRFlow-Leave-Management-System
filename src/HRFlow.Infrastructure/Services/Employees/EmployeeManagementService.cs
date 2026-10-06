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
    ILogger<EmployeeManagementService> logger) : IEmployeeManagementService
{
    /// <inheritdoc />
    public async Task<EmployeeManagementResult> CreateEmployeeAsync(
        Guid actorIdentityUserId, string fullName, string email, string password,
        Guid departmentId, IReadOnlyCollection<string> roles, Guid? managerId,
        CancellationToken cancellationToken)
    {
        EmployeeManagementResult? result = null;
        await writeTransaction.ExecuteAsync(async token =>
        {
            await EnsureCurrentHrAsync(actorIdentityUserId);
            var effectiveRoles = await ValidateRolesAsync(roles);
            await ValidateDepartmentAsync(departmentId, token);
            await EnsureEmailAvailableAsync(email.Trim(), null);
            await ValidateManagerAssignmentAsync(null, departmentId, managerId, token);

            var identity = new ApplicationUser { UserName = email.Trim(), Email = email.Trim() };
            EnsureIdentitySucceeded(await userManager.CreateAsync(identity, password));
            EnsureIdentitySucceeded(await userManager.AddToRolesAsync(identity, effectiveRoles));

            var employee = Employee.Create(fullName, email, departmentId);
            employee.SetIdentityUser(identity.Id.ToString());
            employee.AssignManager(managerId);
            dbContext.Employees.Add(employee);
            result = new EmployeeManagementResult { EmployeeId = employee.Id, Version = employee.Version };
        }, cancellationToken);
        logger.LogInformation("Employee created. EmployeeId: {EmployeeId}; ActorIdentityUserId: {ActorIdentityUserId}",
            result!.EmployeeId, actorIdentityUserId);
        return result;
    }

    /// <inheritdoc />
    public async Task<EmployeeManagementResult> UpdateEmployeeAsync(
        Guid actorIdentityUserId, Guid employeeId, Guid expectedVersion, string fullName, string email,
        Guid departmentId, IReadOnlyCollection<string> roles, ManagerAssignmentOperation managerAssignment,
        Guid? managerId, CancellationToken cancellationToken)
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
            if (currentRoles.Contains(EmployeeRoles.HrAdministrator)
                && !effectiveRoles.Contains(EmployeeRoles.HrAdministrator))
            {
                var hrRole = await roleManager.FindByNameAsync(EmployeeRoles.HrAdministrator);
                var hrCount = await dbContext.UserRoles.CountAsync(r => r.RoleId == hrRole!.Id, token);
                if (hrCount <= 1)
                {
                    throw new WriteConflictException("The last HR Administrator cannot be removed.");
                }
            }

            // UserManager saves through the same scoped DbContext. Those saves remain inside this transaction.
            // Version rotates even for a role-only edit so stale role replacements cannot overwrite it.
            employee.Update(fullName, email, departmentId);
            employee.AssignManager(effectiveManager);
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
    public async Task ValidateManagerAssignmentAsync(
        Guid? employeeId, Guid departmentId, Guid? managerId, CancellationToken cancellationToken)
    {
        if (!managerId.HasValue) return;
        if (managerId == Guid.Empty) throw new DomainException("Manager assignment must reference a valid employee.");
        if (employeeId == managerId) throw new DomainException("An employee cannot be assigned as their own manager.");

        var manager = await dbContext.Employees.AsNoTracking()
            .SingleOrDefaultAsync(e => e.Id == managerId, cancellationToken)
            ?? throw new DomainException("Assigned manager was not found.");
        if (manager.DepartmentId != departmentId)
        {
            throw new DomainException("Assigned manager must belong to the same department.");
        }
        var identity = string.IsNullOrWhiteSpace(manager.IdentityUserId)
            ? null : await userManager.FindByIdAsync(manager.IdentityUserId);
        if (identity is null || !await userManager.IsInRoleAsync(identity, EmployeeRoles.Manager))
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
        if (actor is null || !await userManager.IsInRoleAsync(actor, EmployeeRoles.HrAdministrator))
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
        if (!await dbContext.Employees.AnyAsync(e => e.ManagerId == employee.Id, cancellationToken)) return;
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
