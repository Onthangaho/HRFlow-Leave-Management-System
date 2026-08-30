using MediatR;
using System;

namespace HRFlow.Application.Features.LeaveRequests.Commands.RejectLeaveRequest
{
    public class RejectLeaveRequestCommand : IRequest
    {
        public Guid LeaveRequestId { get; set; }
        public Guid RejectorId { get; set; }
    }
}