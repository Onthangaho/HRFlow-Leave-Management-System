namespace HRFlow.Application.Features.LeaveRequests.Queries.GetEmployeeLeaveHistory;

/// <summary>
/// Represents a leave request in an employee's personal timeline, including its current lifecycle
/// state and the decisions recorded against it.
/// </summary>
public sealed class LeaveRequestHistoryDto
{
    /// <summary>
    /// Gets or sets the leave request identifier used for employee cancellation actions.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the human-readable leave category shown to the employee.
    /// </summary>
    public string LeaveTypeName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the first day included in the request.
    /// </summary>
    public DateTime StartDate { get; set; }

    /// <summary>
    /// Gets or sets the final day included in the request.
    /// </summary>
    public DateTime EndDate { get; set; }

    /// <summary>
    /// Gets or sets the request's current lifecycle status.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when a final action was processed, if the request is no longer pending.
    /// </summary>
    public DateTime? ProcessedOn { get; set; }

    /// <summary>
    /// Gets or sets the immutable audit records that explain request decisions.
    /// </summary>
    public IReadOnlyList<LeaveRequestDecisionDto> DecisionHistory { get; set; } = [];
}

/// <summary>
/// Represents one auditable lifecycle transition so employees can understand who acted on a request
/// and when without receiving unrelated audit data.
/// </summary>
public sealed class LeaveRequestDecisionDto
{
    /// <summary>Ordinary manager context, separate from sensitive HR cancellation reasons.</summary>
    public string? DecisionNote { get; set; }
    /// <summary>
    /// Gets or sets the action that caused the request status transition.
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the display name of the employee who performed the action.
    /// </summary>
    public string ActorFullName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the lifecycle action was recorded.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Gets or sets the status before the lifecycle action, when applicable.
    /// </summary>
    public string? OldStatus { get; set; }

    /// <summary>
    /// Gets or sets the status after the lifecycle action.
    /// </summary>
    public string NewStatus { get; set; } = string.Empty;
}
