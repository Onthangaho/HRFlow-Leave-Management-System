using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Services;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Services;

/// <summary>
/// Applies Identity Manager-role checks to the reporting relationships used by the leave workflow.
/// </summary>
public sealed class LeaveApprovalAuthorizationService : ILeaveApprovalAuthorizationService
{
    private const string ManagerRoleName = "Manager";

    private readonly HRFlowDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    /// <summary>
    /// Creates the service with the Identity and domain stores required to validate a reporting line.
    /// </summary>
    public LeaveApprovalAuthorizationService(
        HRFlowDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    /// <inheritdoc />
    public async Task EnsureEmployeeHasValidManagerAsync(Employee employee, CancellationToken cancellationToken)
    {
        if (!employee.IsActive) throw new ForbiddenException("Inactive employees cannot submit leave requests.");
        if (!employee.ManagerId.HasValue)
        {
            throw new ForbiddenException(
                "Leave requests require an assigned manager. Contact HR to update your reporting assignment.");
        }

        var manager = await _context.Employees
            .AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == employee.ManagerId.Value, cancellationToken);

        if (manager is null
            || manager.DepartmentId != employee.DepartmentId
            || !ManagerApprovalScope.Includes(manager, employee)
            || !await HasManagerRoleAsync(manager, cancellationToken))
        {
            throw new ForbiddenException(
                "Leave requests require a valid assigned manager in your department. Contact HR to correct your reporting assignment.");
        }
    }

    /// <inheritdoc />
    public async Task EnsureManagerCanDecideAsync(
        Employee manager,
        Employee requestOwner,
        CancellationToken cancellationToken)
    {
        if (!await HasManagerRoleAsync(manager, cancellationToken))
        {
            throw new ForbiddenException("Only employees with the Manager role can decide leave requests.");
        }

        if (!ManagerApprovalScope.Includes(manager, requestOwner))
        {
            throw new ForbiddenException(
                "You can only decide pending leave requests from your assigned direct reports in your department.");
        }
    }

    private async Task<bool> HasManagerRoleAsync(Employee employee, CancellationToken cancellationToken)
    {
        if (!employee.IsActive || string.IsNullOrWhiteSpace(employee.IdentityUserId))
        {
            return false;
        }

        var identityUser = await _userManager.FindByIdAsync(employee.IdentityUserId);
        return identityUser is not null && !identityUser.RequiresActivation && await _userManager.IsInRoleAsync(identityUser, ManagerRoleName);
    }
}
