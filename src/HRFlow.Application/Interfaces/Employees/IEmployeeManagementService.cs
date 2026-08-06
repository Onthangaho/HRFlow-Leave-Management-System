using HRFlow.Application.Models.Employees;

namespace HRFlow.Application.Interfaces.Employees;

/// <summary>
/// Coordinates employee profile persistence with ASP.NET Identity account and role updates.
/// The implementation must ensure identity and domain writes do not leave half-applied state.
/// </summary>
public interface IEmployeeManagementService
{
    /// <summary>
    /// Creates an employee profile plus identity user account and role assignment as one atomic operation.
    /// </summary>
    Task<EmployeeManagementResult> CreateEmployeeAsync(
        string fullName,
        string email,
        string password,
        Guid departmentId,
        string roleName,
        Guid? managerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Updates employee profile fields and synchronizes the linked identity account role assignment.
    /// </summary>
    Task<EmployeeManagementResult> UpdateEmployeeAsync(
        Guid employeeId,
        string fullName,
        string email,
        Guid departmentId,
        string roleName,
        Guid? managerId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether a department exists before command execution reaches persistence writes.
    /// </summary>
    Task<bool> DepartmentExistsAsync(Guid departmentId, CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether an identity role exists using the current Identity role store.
    /// </summary>
    Task<bool> RoleExistsAsync(string roleName, CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether an employee exists for update command validation.
    /// </summary>
    Task<bool> EmployeeExistsAsync(Guid employeeId, CancellationToken cancellationToken);

    /// <summary>
    /// Checks whether an email can be used by the target create/update flow without conflicting with another employee account.
    /// </summary>
    Task<bool> IsEmailAvailableAsync(string email, Guid? currentEmployeeId, CancellationToken cancellationToken);
}
