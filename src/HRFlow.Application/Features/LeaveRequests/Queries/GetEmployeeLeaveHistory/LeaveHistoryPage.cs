using FluentValidation;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetEmployeeLeaveHistory;

/// <summary>Optional inclusive intersection bounds apply before count/paging; page values cannot overflow offsets.</summary>
public sealed class LeaveHistoryFilter
{
    /// <summary>Exact lifecycle name; numeric and unknown enum values are rejected.</summary>
    public string? Status { get; set; }
    /// <summary>Exact type ID; unavailable IDs safely match no owned records.</summary>
    public Guid? LeaveTypeId { get; set; }
    /// <summary>Only requests ending on/after this date match.</summary>
    public DateOnly? Start { get; set; }
    /// <summary>Only requests starting on/before this date match.</summary>
    public DateOnly? End { get; set; }
    /// <summary>Positive requested page; out-of-range pages return the last actual page.</summary>
    public int Page { get; set; } = 1;
    /// <summary>Defaults to 25 and never exceeds 100.</summary>
    public int PageSize { get; set; } = 25;
}

/// <summary>Count and bounded rows come from the same authorized snapshot; balances remain a separate full-history calculation.</summary>
public sealed record LeaveHistoryPage(IReadOnlyList<LeaveRequestHistoryDto> Items, int TotalCount,
    int Page, int PageSize, int TotalPages, DateTimeOffset CapturedAtUtc);

/// <summary>Rejects malformed filters before any unbounded work; oversized query integers are rejected by model binding.</summary>
public sealed class GetEmployeeLeaveHistoryQueryValidator : AbstractValidator<GetEmployeeLeaveHistoryQuery>
{
    /// <summary>Only explicit supported names are accepted, never numeric enum aliases.</summary>
    public GetEmployeeLeaveHistoryQueryValidator()
    {
        RuleFor(q => q.Filter.Page).GreaterThan(0);
        RuleFor(q => q.Filter.PageSize).InclusiveBetween(1, 100);
        RuleFor(q => q.Filter.Status).Must(s => s == null || s is "Pending" or "Approved" or "Rejected" or "Cancelled")
            .WithMessage("Choose a supported request status.");
        RuleFor(q => q.Filter.LeaveTypeId).Must(id => id == null || id != Guid.Empty);
        RuleFor(q => q.Filter).Must(f => f.Start == null || f.End == null || f.End >= f.Start)
            .WithMessage("End date must be on or after start date.");
    }
}
