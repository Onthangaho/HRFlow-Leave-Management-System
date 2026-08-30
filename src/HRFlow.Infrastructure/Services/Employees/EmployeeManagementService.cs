
using HRFlow.Application.Exceptions;
using HRFlow.Domain.Exceptions;
using HRFlow.Domain.Interfaces.Services.Employees;
using HRFlow.Domain.Models.Employees;
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

            return new EmployeeManagementResult
            {
                EmployeeId = employee.Id,
                IdentityUserId = identityUser.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Employee creation failed; rolling back transaction.");
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

        var employee = await _dbContext.Employees
            .FirstOrDefaultAsync(e => e.Id == employeeId, cancellationToken);
        if (employee is null)
        {
            throw new NotFoundException($"Employee with ID {employeeId} not found.");
        }

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

            return new EmployeeManagementResult
            {
                EmployeeId = employee.Id,
                IdentityUserId = identityUser.Id.ToString()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Employee update failed; rolling back transaction.");
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