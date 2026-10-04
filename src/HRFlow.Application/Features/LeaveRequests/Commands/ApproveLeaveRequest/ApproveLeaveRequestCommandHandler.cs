using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest
{
    public class ApproveLeaveRequestCommandHandler : IRequestHandler<ApproveLeaveRequestCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILeaveApprovalAuthorizationService _leaveApprovalAuthorizationService;

        public ApproveLeaveRequestCommandHandler(
            IApplicationDbContext context,
            ILeaveApprovalAuthorizationService leaveApprovalAuthorizationService)
        {
            _context = context;
            _leaveApprovalAuthorizationService = leaveApprovalAuthorizationService;
        }

        public async Task Handle(ApproveLeaveRequestCommand request, CancellationToken cancellationToken)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Employee)
                .FirstOrDefaultAsync(lr => lr.Id == request.LeaveRequestId, cancellationToken);

            if (leaveRequest == null)
            {
                throw new NotFoundException("Leave request was not found.");
            }

            var approver = await _context.Employees.FindAsync(request.ApproverId);
            if (approver == null)
            {
                throw new NotFoundException("Approver was not found.");
            }

            await _leaveApprovalAuthorizationService.EnsureManagerCanDecideAsync(
                approver,
                leaveRequest.Employee,
                cancellationToken);

            leaveRequest.Approve(request.ApproverId);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}