namespace HRFlow.Application.Features.LeaveRequests.Queries.GetLeaveBalances;

/// <summary>
/// Represents a derived balance for one leave type so clients can distinguish policy entitlement,
/// approved use, and currently available days.
/// </summary>
public sealed class LeaveBalanceDto
{
    /// <summary>
    /// Gets or sets the leave type this calculated balance represents.
    /// </summary>
    public Guid LeaveTypeId { get; set; }

    /// <summary>
    /// Gets or sets the human-readable leave type name for display.
    /// </summary>
    public string LeaveTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy-provided days available before any approved leave is deducted.
    /// </summary>
    public int EntitledDays { get; set; }

    /// <summary>
    /// Gets or sets the days consumed by approved requests for this leave type.
    /// </summary>
    public int UsedDays { get; set; }

    /// <summary>
    /// Gets or sets the days remaining after approved requests are deducted.
    /// </summary>
    public int RemainingDays { get; set; }
}
