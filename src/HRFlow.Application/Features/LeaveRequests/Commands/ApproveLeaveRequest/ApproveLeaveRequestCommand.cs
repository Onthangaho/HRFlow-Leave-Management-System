using MediatR;
using System;

namespace HRFlow.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest
{
    /// <summary>Supplies server-derived actor identity and optional immutable manager context.</summary>
    public class ApproveLeaveRequestCommand : IRequest
    {
        /// <summary>Optional plain text, normalized and bounded before audit persistence.</summary>
        public string? DecisionNote { get; set; }
        public Guid LeaveRequestId { get; set; }
        public Guid ApproverId { get; set; }
    }
}