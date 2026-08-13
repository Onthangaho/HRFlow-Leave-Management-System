using System.Text.RegularExpressions;
using HRFlow.Domain.Common;

namespace HRFlow.Domain.Entities;

/// <summary>
/// Represents an employee in the HR system with identity information department and manager assignments.
/// </summary>
public class Employee : BaseEntity
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public const int MaxFullNameLength = 200;
    public const int MaxEmailLength = 256;

    /// <summary>
    /// Gets or sets the employee's full name.
    /// </summary>
    public string FullName { get; private set; } = default!;

    /// <summary>
    /// Gets or sets the employee's email address which also serves as the username for identity.
    /// </summary>
    public string Email { get; private set; } = default!;

    /// <summary>
    /// Gets or sets the unique identifier for the linked ASP.NET Core Identity user.
    /// </summary>
    public string? IdentityUserId { get; private set; }

    /// <summary>
    /// Gets or sets the unique identifier for the department this employee belongs to.
    /// </summary>
    public Guid DepartmentId { get; private set; }

    /// <summary>
    /// Gets or sets the department this employee belongs to.
    /// </summary>
    public virtual Department Department { get; private set; } = default!;

    /// <summary>
    /// Gets or sets the unique identifier for the employee's manager.
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
    /// Private constructor for EF Core and the public factory.
    /// </summary>
    private Employee()
    {
    }

    /// <summary>
    /// Creates a new employee, ensuring all validation rules for identity and department assignment are met.
    /// </summary>
    public static Employee Create(string fullName, string email, Guid departmentId)
    {
        var employee = new Employee();
        employee.Update(fullName, email, departmentId);
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
        Guid departmentId)
    {
        // Validate all inputs before any mutation to ensure aggregate remains unchanged on validation failure
        ValidateIdentity(fullName, email);
        ValidateDepartment(departmentId);

        // All validation passed - now apply mutations
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

    /// <summary>
    /// Links this employee to an ASP.NET Core Identity user account.
    /// This is a controlled operation to ensure the link is only established once and with a valid ID.
    /// </summary>
    /// <param name="identityUserId">The user ID from the identity system.</param>
    public void SetIdentityUser(string identityUserId)
    {
        if (string.IsNullOrWhiteSpace(identityUserId))
        {
            throw new InvalidOperationException("Domain validation error: IdentityUserId cannot be null or whitespace.");
        }

        if (!string.IsNullOrEmpty(IdentityUserId))
        {
            throw new InvalidOperationException("Domain integrity error: IdentityUserId has already been set and cannot be changed.");
        }

        IdentityUserId = identityUserId;
    }

    private void ValidateIdentity(string fullName, string email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
        {
            throw new InvalidOperationException("Domain validation error: FullName is required.");
        }

        if (fullName.Trim().Length > MaxFullNameLength)
        {
            throw new InvalidOperationException($"Domain validation error: FullName cannot exceed {MaxFullNameLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidOperationException("Domain validation error: Email is required.");
        }

        if (email.Trim().Length > MaxEmailLength)
        {
            throw new InvalidOperationException($"Domain validation error: Email cannot exceed {MaxEmailLength} characters.");
        }

        if (!EmailRegex.IsMatch(email.Trim()))
        {
            throw new InvalidOperationException("Domain validation error: Email format is invalid.");
        }
    }

    private void ValidateDepartment(Guid departmentId)
    {
        if (departmentId == Guid.Empty)
        {
            throw new InvalidOperationException("Domain validation error: DepartmentId is required.");
        }
    }

    private void UpdateIdentity(string fullName, string email)
    {
        ValidateIdentity(fullName, email);
        FullName = fullName.Trim();
        Email = email.Trim();
    }

    private void AssignDepartment(Guid departmentId)
    {
        ValidateDepartment(departmentId);
        DepartmentId = departmentId;
    }
}