namespace HRFlow.Application.Models.Employees;

/// <summary>
/// Returns the key identifiers produced or modified by employee management commands.
/// </summary>
public sealed class EmployeeManagementResult
{
    /// <summary>
    /// Gets or sets the persisted employee aggregate identifier.
    /// </summary>
    public Guid EmployeeId { get; set; }

    /// <summary>
    /// Gets or sets the linked ASP.NET Identity user identifier.
    /// </summary>
    public string IdentityUserId { get; set; } = string.Empty;
}
