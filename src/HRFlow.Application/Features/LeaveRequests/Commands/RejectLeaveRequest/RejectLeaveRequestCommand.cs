using MediatR;
using System;

namespace HRFlow.Application.Features.LeaveRequests.Commands.RejectLeaveRequest
{
    /// <summary>Supplies server-derived actor identity and optional immutable manager context.</summary>
    public class RejectLeaveRequestCommand : IRequest
    {
        /// <summary>Optional plain text, normalized and bounded before audit persistence.</summary>
        public string? DecisionNote { get; set; }
        public Guid LeaveRequestId { get; set; }
        public Guid RejectorId { get; set; }
    }
}