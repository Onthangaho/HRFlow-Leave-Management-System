
using HRFlow.Application.Exceptions;
using HRFlow.Domain.Exceptions;
using HRFlow.Domain.Interfaces.Services.Employees;
using HRFlow.Domain.Models.Employees;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Common;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace HRFlow.Infrastructure.Services.Employees;

/// <summary>
/// Provides employee management services including creation and updates with transactional integrity
/// between the application database and the ASP.NET Core Identity store.
/// </summary>
public sealed class EmployeeManagementService : IEmployeeManagementService
{
    private const string ManagerRoleName = "Manager";

    private readonly HRFlowDbContext _dbContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole<Guid>> _roleManager;
    private readonly ILogger<EmployeeManagementService> _logger;

    public EmployeeManagementService(
        HRFlowDbContext dbContext,
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole<Guid>> roleManager,
        ILogger<EmployeeManagementService> logger)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<EmployeeManagementResult> CreateEmployeeAsync(
        string fullName,
        string email,
        string password,
        Guid departmentId,
        string roleName,
        Guid? managerId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        await ValidateManagerAssignmentAsync(null, departmentId, managerId, cancellationToken);
        _logger.LogInformation(
            "Creating employee. DepartmentId: {DepartmentId}; RoleName: {RoleName}; ManagerId: {ManagerId}",
            departmentId,
            roleName,
            managerId);

        var identityUser = new ApplicationUser { UserName = email, Email = email };
        Guid? identityUserId = null;

        try
        {
            var identityResult = await _userManager.CreateAsync(identityUser, password);
            if (!identityResult.Succeeded)
            {
                if (identityResult.Errors.Any(error => error.Code == "DuplicateUserName" || error.Code == "DuplicateEmail"))
                {
                    throw new DuplicateEmailException(email);
                }
                EnsureIdentitySucceeded(identityResult, "create the identity user");
            }
            identityUserId = identityUser.Id;

            var addRoleResult = await _userManager.AddToRoleAsync(identityUser, roleName);
            EnsureIdentitySucceeded(addRoleResult, $"assign the '{roleName}' identity role");

            var employee = Employee.Create(fullName, email, departmentId);
            employee.SetIdentityUser(identityUser.Id.ToString());
            employee.AssignManager(managerId);

            _dbContext.Set<Employee>().Add(employee);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation(
                "Employee created. EmployeeId: {EmployeeId}; IdentityUserId: {IdentityUserId}; DepartmentId: {DepartmentId}; RoleName: {RoleName}",
                employee.Id,
                identityUser.Id,
                departmentId,
                roleName);

            return new EmployeeManagementResult
            {
                EmployeeId = employee.Id,
                IdentityUserId = identityUser.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Employee creation failed; rolling back transaction. DepartmentId: {DepartmentId}; RoleName: {RoleName}; ManagerId: {ManagerId}",
                departmentId,
                roleName,
                managerId);
            await transaction.RollbackAsync(cancellationToken);

            if (identityUserId.HasValue)
            {
                await DeleteUserIfPresentAsync(identityUserId.Value, cancellationToken);
            }
            
            throw;
        }
    }

    /// <inheritdoc />
    public async Task<EmployeeManagementResult> UpdateEmployeeAsync(
        Guid employeeId,
        string fullName,
        string email,
        Guid departmentId,
        string roleName,
        Guid? managerId,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
        _logger.LogInformation(
            "Updating employee. EmployeeId: {EmployeeId}; DepartmentId: {DepartmentId}; RoleName: {RoleName}; ManagerId: {ManagerId}",
            employeeId,
            departmentId,
            roleName,
            managerId);

        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            throw new NotFoundException($"Employee with ID {employeeId} not found.");
        }

        await ValidateManagerAssignmentAsync(employeeId, departmentId, managerId, cancellationToken);

        if (string.IsNullOrEmpty(employee.IdentityUserId))
        {
            throw new InvalidOperationException(
                $"Employee {employeeId} does not have an associated identity user.");
        }
        
        var identityUser = await _userManager.FindByIdAsync(employee.IdentityUserId);
        if (identityUser is null)
        {
            throw new InvalidOperationException(
                $"Could not find the identity user for employee {employeeId} with identity ID {employee.IdentityUserId}.");
        }

        await EnsureDirectReportsRemainValidAsync(
            employee,
            departmentId,
            roleName,
            cancellationToken);

        try
        {
            employee.Update(fullName, email, departmentId);
            employee.AssignManager(managerId);

            identityUser.UserName = email;
            identityUser.Email = email;
            var identityResult = await _userManager.UpdateAsync(identityUser);
            EnsureIdentitySucceeded(identityResult, "update the identity user");

            var currentRoles = await _userManager.GetRolesAsync(identityUser);
            if (!currentRoles.Contains(roleName))
            {
                var removeRolesResult = await _userManager.RemoveFromRolesAsync(identityUser, currentRoles);
                EnsureIdentitySucceeded(removeRolesResult, "remove the old identity roles");

                var addRoleResult = await _userManager.AddToRoleAsync(identityUser, roleName);
                EnsureIdentitySucceeded(addRoleResult, $"assign the new '{roleName}' identity role");
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            _logger.LogInformation(
                "Employee updated. EmployeeId: {EmployeeId}; IdentityUserId: {IdentityUserId}; DepartmentId: {DepartmentId}; RoleName: {RoleName}",
                employee.Id,
                identityUser.Id,
                departmentId,
                roleName);

            return new EmployeeManagementResult
            {
                EmployeeId = employee.Id,
                IdentityUserId = identityUser.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Employee update failed; rolling back transaction. EmployeeId: {EmployeeId}; DepartmentId: {DepartmentId}; RoleName: {RoleName}; ManagerId: {ManagerId}",
                employeeId,
                departmentId,
                roleName,
                managerId);
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    /// <inheritdoc />
    public Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Department>().AnyAsync(
            department => department.Id == departmentId,
            cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(roleName);
        return await _roleManager.RoleExistsAsync(roleName);
    }

    /// <inheritdoc />
    public Task<bool> EmployeeExistsAsync(Guid employeeId, CancellationToken cancellationToken)
    {
        return _dbContext.Set<Employee>().AnyAsync(employee => employee.Id == employeeId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<bool> IsEmailAvailableAsync(
        string email,
        Guid? currentEmployeeId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(email);

        var existingUser = await _userManager.FindByEmailAsync(email);
        if (existingUser is null)
        {
            return true;
        }

        if (!currentEmployeeId.HasValue)
        {
            return false;
        }

        var currentEmployee = await _dbContext.Set<Employee>()
            .AsNoTracking()
            .SingleOrDefaultAsync(employee => employee.Id == currentEmployeeId.Value, cancellationToken);

        return currentEmployee is not null
            && string.Equals(currentEmployee.Email, email, StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public async Task ValidateManagerAssignmentAsync(
        Guid? employeeId,
        Guid departmentId,
        Guid? managerId,
        CancellationToken cancellationToken)
    {
        if (!managerId.HasValue)
        {
            return;
        }

        if (managerId.Value == Guid.Empty)
        {
            throw new DomainException("Manager assignment must reference a valid employee.");
        }

        if (employeeId == managerId)
        {
            throw new DomainException("An employee cannot be assigned as their own manager.");
        }

        var manager = await _dbContext.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(employee => employee.Id == managerId.Value, cancellationToken)
            ?? throw new DomainException("Assigned manager was not found.");

        if (manager.DepartmentId != departmentId)
        {
            throw new DomainException("Assigned manager must belong to the same department.");
        }

        if (string.IsNullOrWhiteSpace(manager.IdentityUserId))
        {
            throw new DomainException("Assigned manager must be linked to an account with the Manager role.");
        }

        var managerIdentity = await _userManager.FindByIdAsync(manager.IdentityUserId);
        if (managerIdentity is null || !await _userManager.IsInRoleAsync(managerIdentity, ManagerRoleName))
        {
            throw new DomainException("Assigned manager must have the Manager role.");
        }

        if (employeeId.HasValue)
        {
            await EnsureAssignmentDoesNotCreateCycleAsync(employeeId.Value, manager, cancellationToken);
        }
    }

    private async Task EnsureAssignmentDoesNotCreateCycleAsync(
        Guid employeeId,
        Employee assignedManager,
        CancellationToken cancellationToken)
    {
        var visitedEmployeeIds = new HashSet<Guid>();
        Employee? current = assignedManager;

        while (current is not null)
        {
            if (current.Id == employeeId)
            {
                throw new DomainException("Manager assignment would create a reporting cycle.");
            }

            if (!visitedEmployeeIds.Add(current.Id))
            {
                throw new DomainException("Manager assignment cannot use an existing reporting cycle.");
            }

            current = current.ManagerId.HasValue
                ? await _dbContext.Employees
                    .AsNoTracking()
                    .SingleOrDefaultAsync(employee => employee.Id == current.ManagerId.Value, cancellationToken)
                : null;
        }
    }

    private async Task EnsureDirectReportsRemainValidAsync(
        Employee employee,
        Guid proposedDepartmentId,
        string proposedRoleName,
        CancellationToken cancellationToken)
    {
        var hasDirectReports = await _dbContext.Employees.AnyAsync(
            candidate => candidate.ManagerId == employee.Id,
            cancellationToken);

        if (!hasDirectReports)
        {
            return;
        }

        if (employee.DepartmentId != proposedDepartmentId)
        {
            throw new DomainException(
                "Cannot change an employee's department while they have direct reports. Reassign the direct reports first.");
        }

        if (!string.Equals(proposedRoleName, ManagerRoleName, StringComparison.Ordinal))
        {
            throw new DomainException(
                "Cannot remove the Manager role while the employee has direct reports. Reassign the direct reports first.");
        }
    }
    
    private async Task DeleteUserIfPresentAsync(Guid identityUserId, CancellationToken cancellationToken)
    {
        var user = await _userManager.FindByIdAsync(identityUserId.ToString());
        if (user is not null)
        {
            await _userManager.DeleteAsync(user);
        }
    }

    private static void EnsureIdentitySucceeded(IdentityResult result, string operationDescription)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to {operationDescription}: {errors}");
        }
    }
}