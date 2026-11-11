namespace HRFlow.Application.Features.LeaveRequests.Queries.GetPendingLeaveRequests;

/// <summary>Filters narrow HR's authorized Pending view; identifiers never establish authorization.</summary>
public sealed class HrMonitoringFilter
{
    /// <summary>Literal employee name/email substring; bounded before trimming.</summary>
    public string? Search { get; set; }
    /// <summary>Current department attribution, not reconstructed historical membership.</summary>
    public Guid? DepartmentId { get; set; }
    /// <summary>Exact type; unknown identifiers match no rows.</summary>
    public Guid? LeaveTypeId { get; set; }
    /// <summary>Optional inclusive request-date intersection lower bound.</summary>
    public DateOnly? Start { get; set; }
    /// <summary>Optional inclusive request-date intersection upper bound.</summary>
    public DateOnly? End { get; set; }
    /// <summary>Unavailable positive pages are clamped after the filtered count.</summary>
    public int Page { get; set; } = 1;
    /// <summary>Default 25 and maximum 100; no unbounded compatibility mode.</summary>
    public int PageSize { get; set; } = 25;
}

/// <summary>Count, rows and UTC capture time describe the same authorized deferred snapshot.</summary>
public sealed record HrPendingMonitoringPage(IReadOnlyList<PendingLeaveRequestDto> Items, int TotalCount,
    int Page, int PageSize, int TotalPages, DateTimeOffset CapturedAtUtc);
