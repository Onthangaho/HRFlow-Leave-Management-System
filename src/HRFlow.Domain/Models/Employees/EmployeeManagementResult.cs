namespace HRFlow.Domain.Models.Employees;

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
    /// Returns the version of the accepted edit without exposing account credentials or Identity internals.
    /// </summary>
    public Guid Version { get; set; }
}
