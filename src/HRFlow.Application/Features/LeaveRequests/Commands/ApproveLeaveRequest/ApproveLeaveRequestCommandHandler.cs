using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HRFlow.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest
{
    public class ApproveLeaveRequestCommandHandler : IRequestHandler<ApproveLeaveRequestCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public ApproveLeaveRequestCommandHandler(IApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task Handle(ApproveLeaveRequestCommand request, CancellationToken cancellationToken)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Employee)
                .FirstOrDefaultAsync(lr => lr.Id == request.LeaveRequestId, cancellationToken);

            if (leaveRequest == null)
            {
                throw new Exception("Leave request not found.");
            }

            var approver = await _context.Employees.FindAsync(request.ApproverId);
            if (approver == null)
            {
                throw new Exception("Approver not found.");
            }

            if (string.IsNullOrEmpty(approver.IdentityUserId))
            {
                throw new Exception("Approver is not linked to a system user account.");
            }

            var identityUser = await _userManager.FindByIdAsync(approver.IdentityUserId);
            if (identityUser == null)
            {
                throw new Exception("Approver identity not found.");
            }

            var isManager = leaveRequest.Employee.ManagerId == approver.Id;
            var isHrAdmin = await _userManager.IsInRoleAsync(identityUser, "HR Administrator");

            if (!isManager && !isHrAdmin)
            {
                throw new Exception("Only the assigned manager or an HR Administrator can approve this request.");
            }

            if (leaveRequest.EmployeeId == request.ApproverId)
            {
                throw new Exception("You cannot approve your own leave request.");
            }

            if (leaveRequest.Status.ToString() != "Pending")
            {
                throw new InvalidOperationException("Only pending leave requests can be approved.");
            }

            leaveRequest.Approve(request.ApproverId);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}