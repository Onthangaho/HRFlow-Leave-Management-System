using System.Collections.Generic;

namespace HRFlow.Domain.Entities;

/// <summary>
/// Represents an employee in the organization and supports manager hierarchy relationships.
/// </summary>
public class Employee
{
    /// <summary>
    /// Gets or sets the unique identifier for the employee.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the employee's full name.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the employee's primary email address.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the department that owns this employee.
    /// </summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>
    /// Gets or sets the department that owns this employee.
    /// </summary>
    public Department? Department { get; set; }

    /// <summary>
    /// Gets or sets the identifier of the employee's manager when one exists.
    /// </summary>
    public Guid? ManagerId { get; set; }

    /// <summary>
    /// Gets or sets the employee's manager when the employee reports to another employee.
    /// </summary>
    public Employee? Manager { get; set; }

    /// <summary>
    /// Gets or sets the employees that report directly to this employee.
    /// </summary>
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();
}
