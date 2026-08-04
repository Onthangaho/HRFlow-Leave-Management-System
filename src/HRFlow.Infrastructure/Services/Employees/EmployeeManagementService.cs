using HRFlow.Application.Interfaces.Employees;
using HRFlow.Application.Models.Employees;
using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Services.Employees;

/// <summary>
/// Implements employee management by coordinating ASP.NET Identity and employee aggregate writes in one unit of work.
/// </summary>
public sealed class EmployeeManagementService : IEmployeeManagementService
{
    private readonly HRFlowDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;

    /// <summary>
    /// Creates a service with identity and persistence dependencies needed for atomic employee management flows.
    /// </summary>
    public EmployeeManagementService(
        HRFlowDbContext dbContext,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        _dbContext = dbContext;
        _userManager = userManager;
        _roleManager = roleManager;
    }

    /// <inheritdoc />
    public async Task<EmployeeManagementResult> CreateEmployeeAsync(
        string fullName,
        string email,
        string password,
        Guid departmentId,
        string roleName,
        Guid? managerId,
        bool simulateFailureAfterIdentityCreation,
        CancellationToken cancellationToken)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        IdentityUser? createdIdentityUser = null;

        try
        {
            var identityUser = new IdentityUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true
            };

            var createUserResult = await _userManager.CreateAsync(identityUser, password);
            EnsureIdentitySucceeded(createUserResult, "create the identity user account");
            createdIdentityUser = identityUser;

            var addRoleResult = await _userManager.AddToRoleAsync(identityUser, roleName);
            EnsureIdentitySucceeded(addRoleResult, $"assign the '{roleName}' identity role");

            if (simulateFailureAfterIdentityCreation)
            {
                throw new InvalidOperationException(
                    "Simulated employee persistence failure after identity user creation.");
            }

            var employee = Employee.Create(fullName, email, departmentId);
            employee.AssignManager(managerId);

            _dbContext.Set<Employee>().Add(employee);
            await _dbContext.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new EmployeeManagementResult
            {
                EmployeeId = employee.Id,
                IdentityUserId = identityUser.Id
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);

            if (createdIdentityUser is not null)
            {
                await DeleteUserIfPresentAsync(createdIdentityUser.Id);
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

        var employee = await _dbContext.Set<Employee>()
            .SingleOrDefaultAsync(entity => entity.Id == employeeId, cancellationToken)
            ?? throw new InvalidOperationException($"Employee '{employeeId}' was not found.");

        var identityUser = await _userManager.FindByEmailAsync(employee.Email)
            ?? throw new InvalidOperationException(
                $"Linked identity user for employee '{employeeId}' with email '{employee.Email}' was not found.");

        employee.Update(fullName, email, departmentId);
        employee.AssignManager(managerId);

        if (!string.Equals(identityUser.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            identityUser.Email = email;
            identityUser.UserName = email;

            var updateUserResult = await _userManager.UpdateAsync(identityUser);
            EnsureIdentitySucceeded(updateUserResult, "update the identity user email");
        }

        await EnsureSingleAssignedRoleAsync(identityUser, roleName);

        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        return new EmployeeManagementResult
        {
            EmployeeId = employee.Id,
            IdentityUserId = identityUser.Id
        };
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

    private async Task EnsureSingleAssignedRoleAsync(IdentityUser identityUser, string roleName)
    {
        var currentRoles = await _userManager.GetRolesAsync(identityUser);

        if (currentRoles.Count == 1 && string.Equals(currentRoles[0], roleName, StringComparison.Ordinal))
        {
            return;
        }

        if (currentRoles.Count > 0)
        {
            var removeRolesResult = await _userManager.RemoveFromRolesAsync(identityUser, currentRoles);
            EnsureIdentitySucceeded(removeRolesResult, "remove existing identity roles");
        }

        var addRoleResult = await _userManager.AddToRoleAsync(identityUser, roleName);
        EnsureIdentitySucceeded(addRoleResult, $"assign the '{roleName}' identity role");
    }

    private async Task DeleteUserIfPresentAsync(string userId)
    {
        var trackedEntry = _dbContext.ChangeTracker.Entries<IdentityUser>()
            .SingleOrDefault(entry => entry.Entity.Id == userId);

        if (trackedEntry is not null)
        {
            trackedEntry.State = EntityState.Detached;
        }

        var userExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId);

        if (!userExists)
        {
            return;
        }

        var existingUser = await _userManager.FindByIdAsync(userId);
        if (existingUser is null)
        {
            return;
        }

        var deleteResult = await _userManager.DeleteAsync(existingUser);
        if (deleteResult.Succeeded)
        {
            return;
        }

        var orphanStillExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId);

        if (orphanStillExists)
        {
            EnsureIdentitySucceeded(deleteResult, "delete the identity user during rollback compensation");
        }
    }

    private static void EnsureIdentitySucceeded(IdentityResult identityResult, string actionDescription)
    {
        if (identityResult.Succeeded)
        {
            return;
        }

        var errors = string.Join(", ", identityResult.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"Unable to {actionDescription}: {errors}");
    }
}
