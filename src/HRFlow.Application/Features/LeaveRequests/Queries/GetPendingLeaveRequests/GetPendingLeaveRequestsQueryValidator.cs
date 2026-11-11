using FluentValidation;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetPendingLeaveRequests;

/// <summary>Rejects unsafe bounds; malformed dates/GUIDs/Int32 values fail API model binding.</summary>
public sealed class GetPendingLeaveRequestsQueryValidator : AbstractValidator<GetPendingLeaveRequestsQuery>
{
    /// <summary>Limits text and pages without silently truncating input or output.</summary>
    public GetPendingLeaveRequestsQueryValidator()
    {
        RuleFor(q => q.ActorIdentityId).NotEmpty();
        RuleFor(q => q.Filter.Search).MaximumLength(100);
        RuleFor(q => q.Filter.DepartmentId).Must(id => id == null || id != Guid.Empty);
        RuleFor(q => q.Filter.LeaveTypeId).Must(id => id == null || id != Guid.Empty);
        RuleFor(q => q.Filter.Page).GreaterThan(0);
        RuleFor(q => q.Filter.PageSize).InclusiveBetween(1, 100);
        RuleFor(q => q.Filter).Must(f => f.Start == null || f.End == null || f.End >= f.Start)
            .WithMessage("End date must be on or after start date.");
    }
}
