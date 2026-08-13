using HRFlow.Domain.Common;
using System.Collections.Generic;

namespace HRFlow.Domain.Entities;

/// <summary>
/// Represents a department within the organization and its employees.
/// </summary>
public class Department : BaseEntity
{
    /// <summary>
    /// Gets or sets the department name.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the employees belonging to this department.
    /// </summary>
    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}