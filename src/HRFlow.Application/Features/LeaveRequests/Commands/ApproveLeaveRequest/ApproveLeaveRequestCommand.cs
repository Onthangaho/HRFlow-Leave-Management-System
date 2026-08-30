using MediatR;
using System;

namespace HRFlow.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest
{
    public class ApproveLeaveRequestCommand : IRequest
    {
        public Guid LeaveRequestId { get; set; }
        public Guid ApproverId { get; set; }
    }
}