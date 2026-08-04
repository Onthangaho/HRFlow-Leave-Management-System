using HRFlow.Application.Models.Employees;
using MediatR;

namespace HRFlow.Application.Features.Employees.Commands.UpdateEmployee;

/// <summary>
/// Updates an employee profile and synchronizes the linked ASP.NET Identity role assignment.
/// </summary>
public sealed class UpdateEmployeeCommand : IRequest<EmployeeManagementResult>
{
    /// <summary>
    /// Gets or sets the employee identifier to update.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Gets or sets the employee display name used across HR workflows.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the employee email used for both domain profile and identity login.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the department assignment for the employee profile.
    /// </summary>
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// Gets or sets the optional direct manager assignment.
    /// </summary>
    public Guid? ManagerId { get; set; }

    /// <summary>
    /// Gets or sets the identity role to assign (for example HR Administrator, Manager, or Employee).
    /// </summary>
    public string RoleName { get; set; } = string.Empty;
}
