using HRFlow.Domain.Models.Employees;

namespace HRFlow.Domain.Interfaces.Services.Employees;

/// <summary>Coordinates account/profile changes under database protection, including live actor permissions.</summary>
public interface IEmployeeManagementService
{
    /// <summary>Creates a linked account and profile atomically, with all selected capabilities.</summary>
    Task<EmployeeManagementResult> CreateEmployeeAsync(
        Guid actorIdentityUserId, string fullName, string email,
        Guid departmentId, IReadOnlyCollection<string> roles, Guid? managerId, string? employeeNumber, DateOnly? employmentStartDate,
        CancellationToken cancellationToken);

    /// <summary>Replaces roles explicitly and rejects stale versions; omitted manager changes preserve reporting.</summary>
    Task<EmployeeManagementResult> UpdateEmployeeAsync(
        Guid actorIdentityUserId, Guid employeeId, Guid expectedVersion, string fullName, string email,
        Guid departmentId, IReadOnlyCollection<string> roles, ManagerAssignmentOperation managerAssignment,
        Guid? managerId, bool confirmEmploymentFacts, string? employeeNumber, DateOnly? employmentStartDate, CancellationToken cancellationToken);

    /// <summary>Deactivates a versioned profile and cancels pending requests atomically, preserving history.</summary>
    Task<EmployeeDeactivationResult> DeactivateEmployeeAsync(Guid actorIdentityUserId, Guid employeeId,
        Guid expectedVersion, string reason, CancellationToken cancellationToken);

    /// <summary>Rejects managers outside the effective department or in a self/cyclic reporting relationship.</summary>
    Task ValidateManagerAssignmentAsync(
        Guid? employeeId, Guid departmentId, Guid? managerId, CancellationToken cancellationToken);
}
