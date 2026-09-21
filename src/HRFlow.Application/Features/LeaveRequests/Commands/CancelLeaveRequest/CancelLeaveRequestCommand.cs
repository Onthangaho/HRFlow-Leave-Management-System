using MediatR;

namespace HRFlow.Application.Features.LeaveRequests.Commands.CancelLeaveRequest;

/// <summary>
/// Cancels a leave request on behalf of its owner after the API resolves the caller to a domain
/// employee ID rather than trusting an Identity claim as a leave-domain identifier.
/// </summary>
public sealed class CancelLeaveRequestCommand : IRequest
{
    /// <summary>
    /// Gets or sets the leave request the employee wants to withdraw.
    /// </summary>
    public Guid LeaveRequestId { get; set; }

    /// <summary>
    /// Gets or sets the domain employee ID resolved for the authenticated request owner.
    /// </summary>
    public Guid EmployeeId { get; set; }
}
