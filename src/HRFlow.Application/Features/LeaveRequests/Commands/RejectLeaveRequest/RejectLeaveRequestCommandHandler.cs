using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Commands.RejectLeaveRequest
{
    public class RejectLeaveRequestCommandHandler : IRequestHandler<RejectLeaveRequestCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly ILeaveApprovalAuthorizationService _leaveApprovalAuthorizationService;

        public RejectLeaveRequestCommandHandler(
            IApplicationDbContext context,
            ILeaveApprovalAuthorizationService leaveApprovalAuthorizationService)
        {
            _context = context;
            _leaveApprovalAuthorizationService = leaveApprovalAuthorizationService;
        }

        public async Task Handle(RejectLeaveRequestCommand request, CancellationToken cancellationToken)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Employee)
                .FirstOrDefaultAsync(lr => lr.Id == request.LeaveRequestId, cancellationToken);

            if (leaveRequest == null)
            {
                throw new NotFoundException("Leave request was not found.");
            }

            var rejector = await _context.Employees.FindAsync(request.RejectorId);
            if (rejector == null)
            {
                throw new NotFoundException("Rejector was not found.");
            }

            await _leaveApprovalAuthorizationService.EnsureManagerCanDecideAsync(
                rejector,
                leaveRequest.Employee,
                cancellationToken);

            leaveRequest.Reject(request.RejectorId);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}