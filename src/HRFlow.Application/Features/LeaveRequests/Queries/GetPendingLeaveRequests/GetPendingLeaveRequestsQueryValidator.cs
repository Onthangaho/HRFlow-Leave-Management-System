using FluentValidation;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetPendingLeaveRequests;

/// <summary>
/// Restricts the queue endpoint to the currently supported pending-status workflow and ensures the
/// authenticated caller has been resolved to a real domain employee before any data is queried.
/// </summary>
public class GetPendingLeaveRequestsQueryValidator : AbstractValidator<GetPendingLeaveRequestsQuery>
{
    private const string PendingStatus = "Pending";

    public GetPendingLeaveRequestsQueryValidator()
    {
        RuleFor(query => query.CurrentEmployeeId)
            .NotEmpty()
            .WithMessage("Authenticated user is not linked to an employee record.");

        RuleFor(query => query.Status)
            .NotEmpty()
            .Must(status => string.Equals(status, PendingStatus, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Only status=Pending is supported for this endpoint.");
    }
}