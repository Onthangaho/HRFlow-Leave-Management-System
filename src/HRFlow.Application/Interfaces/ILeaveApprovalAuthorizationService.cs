using HRFlow.Domain.Entities;

namespace HRFlow.Application.Interfaces;

/// <summary>
/// Verifies the current reporting relationship and Identity role before leave workflow operations
/// can rely on a manager assignment.
/// </summary>
public interface ILeaveApprovalAuthorizationService
{
    /// <summary>
    /// Confirms that the employee has a current valid manager assignment before creating a request.
    /// </summary>
    Task EnsureEmployeeHasValidManagerAsync(Employee employee, CancellationToken cancellationToken);

    /// <summary>
    /// Confirms that an authenticated manager can decide the specified direct report's request.
    /// </summary>
    Task EnsureManagerCanDecideAsync(
        Employee manager,
        Employee requestOwner,
        CancellationToken cancellationToken);
}
