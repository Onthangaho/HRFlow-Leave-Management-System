using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces.Employees;
using HRFlow.Application.Models.Employees;
using HRFlow.Domain.Entities;
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
    private readonly HRFlowDbContext _dbContext;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly ILogger<EmployeeManagementService> _logger;

    public EmployeeManagementService(
        HRFlowDbContext dbContext,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
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

        var identityUser = new IdentityUser { UserName = email, Email = email };
        string? identityUserId = null;

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
            employee.SetIdentityUser(identityUser.Id);
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
        catch (Exception ex)
        {
            _logger.LogError(ex, "Employee creation failed; rolling back transaction.");
            await transaction.RollbackAsync(cancellationToken);

            if (!string.IsNullOrEmpty(identityUserId))
            {
                await DeleteUserIfPresentAsync(identityUserId, cancellationToken);
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
            .SingleOrDefaultAsync(e => e.Id == employeeId, cancellationToken);

        if (employee is null)
        {
            throw new EmployeeNotFoundException(employeeId);
        }

        var identityUser = await _userManager.FindByIdAsync(employee.IdentityUserId!);
        if (identityUser is null)
        {
            throw new InvalidOperationException($"Consistency error: Identity user not found for employee {employeeId}.");
        }

        employee.Update(fullName, email, departmentId);
        employee.AssignManager(managerId);

        if (!string.Equals(identityUser.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            var token = await _userManager.GenerateChangeEmailTokenAsync(identityUser, email);
            var updateUserResult = await _userManager.ChangeEmailAsync(identityUser, email, token);

            if (!updateUserResult.Succeeded)
            {
                if (updateUserResult.Errors.Any(error => error.Code == "DuplicateUserName" || error.Code == "DuplicateEmail"))
                {
                    throw new DuplicateEmailException(email);
                }
                EnsureIdentitySucceeded(updateUserResult, "update the identity user email");
            }

            var setUsernameResult = await _userManager.SetUserNameAsync(identityUser, email);
            EnsureIdentitySucceeded(setUsernameResult, "update the identity username");
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

    private async Task DeleteUserIfPresentAsync(string userId, CancellationToken cancellationToken)
    {
        var trackedEntry = _dbContext.ChangeTracker.Entries<IdentityUser>()
            .SingleOrDefault(entry => entry.Entity.Id == userId);

        if (trackedEntry is not null)
        {
            trackedEntry.State = EntityState.Detached;
        }

        var userExists = await _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Id == userId, cancellationToken);

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
            .AnyAsync(user => user.Id == userId, cancellationToken);

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

        var errors = string.Join(" ", identityResult.Errors.Select(error => error.Description));
        throw new IdentityException($"Unable to {actionDescription}: {errors}");
    }
}