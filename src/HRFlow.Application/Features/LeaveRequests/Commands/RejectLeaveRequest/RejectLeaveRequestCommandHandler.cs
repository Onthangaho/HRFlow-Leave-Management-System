using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace HRFlow.Application.Features.LeaveRequests.Commands.RejectLeaveRequest
{
    public class RejectLeaveRequestCommandHandler : IRequestHandler<RejectLeaveRequestCommand>
    {
        private readonly IApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RejectLeaveRequestCommandHandler(IApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task Handle(RejectLeaveRequestCommand request, CancellationToken cancellationToken)
        {
            var leaveRequest = await _context.LeaveRequests
                .Include(lr => lr.Employee)
                .FirstOrDefaultAsync(lr => lr.Id == request.LeaveRequestId, cancellationToken);

            if (leaveRequest == null)
            {
                throw new Exception("Leave request not found.");
            }

            var rejector = await _context.Employees.FindAsync(request.RejectorId);
            if (rejector == null)
            {
                throw new Exception("Rejector not found.");
            }

            if (string.IsNullOrEmpty(rejector.IdentityUserId))
            {
                throw new Exception("Rejector is not linked to a system user account.");
            }

            var identityUser = await _userManager.FindByIdAsync(rejector.IdentityUserId);
            if (identityUser == null)
            {
                throw new Exception("Rejector identity not found.");
            }

            var isManager = leaveRequest.Employee.ManagerId == rejector.Id;
            var isHrAdmin = await _userManager.IsInRoleAsync(identityUser, "HR Administrator");

            if (!isManager && !isHrAdmin)
            {
                throw new Exception("Only the assigned manager or an HR Administrator can reject this request.");
            }

            if (leaveRequest.EmployeeId == request.RejectorId)
            {
                throw new Exception("You cannot reject your own leave request.");
            }

            leaveRequest.Reject(request.RejectorId);

            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}