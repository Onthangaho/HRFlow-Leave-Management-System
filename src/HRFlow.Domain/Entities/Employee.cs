using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace HRFlow.Domain.Entities;

/// <summary>
/// Represents an employee aggregate and enforces core HR identity and assignment invariants.
/// </summary>
public class Employee
{
    private static readonly Regex EmailRegex = new(
        "^[^@\\s]+@[^@\\s]+\\.[^@\\s]+$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    /// <summary>
    /// Gets or sets the unique identifier for the employee.
    /// </summary>
    public Guid Id { get; private set; }

    /// <summary>
    /// Gets or sets the employee's full name.
    /// </summary>
    public string FullName { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the employee's primary email address.
    /// </summary>
    public string Email { get; private set; } = string.Empty;

    /// <summary>
    /// Gets or sets the identifier of the department that owns this employee.
    /// </summary>
    public Guid? DepartmentId { get; private set; }

    /// <summary>
    /// Gets or sets the department that owns this employee.
    /// </summary>
    public Department? Department { get; private set; }

    /// <summary>
    /// Gets or sets the identifier of the employee's manager when one exists.
    /// </summary>
    public Guid? ManagerId { get; private set; }

    /// <summary>
    /// Gets or sets the employee's manager when the employee reports to another employee.
    /// </summary>
    public Employee? Manager { get; private set; }

    /// <summary>
    /// Gets or sets the employees that report directly to this employee.
    /// </summary>
    public ICollection<Employee> DirectReports { get; private set; } = new List<Employee>();

    /// <summary>
    /// Creates a new employee with required identity and assignment information.
    /// This protects aggregate integrity before persistence or service-level workflows run.
    /// </summary>
    /// <param name="fullName">Employee's display name used across HR workflows.</param>
    /// <param name="email">Employee email used as the primary communication/account identity.</param>
    /// <param name="departmentId">Department assignment for operational ownership.</param>
    /// <returns>A validated employee aggregate instance.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required identity fields are missing, email format is invalid,
    /// or department assignment is missing.
    /// </exception>
    public static Employee Create(
        string fullName,
        string email,
        Guid? departmentId)
    {
        var employee = new Employee
        {
            Id = Guid.NewGuid()
        };

        employee.UpdateIdentity(fullName, email);
        employee.AssignDepartment(departmentId);

        return employee;
    }

    /// <summary>
    /// Updates employee identity and assignment in one operation so callers do not bypass aggregate invariants.
    /// </summary>
    /// <param name="fullName">Employee's display name used across HR workflows.</param>
    /// <param name="email">Employee email used as the primary communication/account identity.</param>
    /// <param name="departmentId">Department assignment for operational ownership.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when required identity fields are missing, email format is invalid,
    /// or department assignment is missing.
    /// </exception>
    public void Update(
        string fullName,
        string email,
        Guid? departmentId)
    {
        UpdateIdentity(fullName, email);
        AssignDepartment(departmentId);
    }

    /// <summary>
    /// Reassigns a manager while keeping explicit control in the aggregate for hierarchy updates.
    /// </summary>
    /// <param name="managerId">Manager identifier, or null when clearing manager assignment.</param>
    public void AssignManager(Guid? managerId)
    {
        ManagerId = managerId == Guid.Empty ? null : managerId;
    }

    private void UpdateIdentity(string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("Domain validation error: FullName is required.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Domain validation error: Email is required.");
        }

        if (!EmailRegex.IsMatch(email.Trim()))
        {
            throw new InvalidOperationException("Domain validation error: Email format is invalid.");
        }

        FullName = fullName.Trim();
        Email = email.Trim();
    }

    private void AssignDepartment(Guid? departmentId)
    {
        if (!departmentId.HasValue || departmentId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("Domain validation error: DepartmentId is required.");
        }

        DepartmentId = departmentId.Value;
    }

}
