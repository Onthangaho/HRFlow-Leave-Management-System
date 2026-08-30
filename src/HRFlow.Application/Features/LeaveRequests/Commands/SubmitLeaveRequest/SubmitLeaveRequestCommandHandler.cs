using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

/// <summary>
/// Handles leave request submission with leave policy validation against approved requests and available balance.
/// </summary>
public class SubmitLeaveRequestCommandHandler : IRequestHandler<SubmitLeaveRequestCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public SubmitLeaveRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(SubmitLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveType = await _context.LeaveTypes
            .Include(lt => lt.LeavePolicy)
            .FirstAsync(lt => lt.Id == request.LeaveTypeId, cancellationToken);

        var approvedRequests = await _context.LeaveRequests
            .Where(lr => lr.EmployeeId == request.EmployeeId &&
                         lr.LeaveTypeId == request.LeaveTypeId &&
                         lr.Status == Domain.Enums.LeaveRequestStatus.Approved)
            .ToListAsync(cancellationToken);

        var leaveRequest = LeaveRequest.Create(
            request.EmployeeId,
            request.LeaveTypeId,
            request.StartDate,
            request.EndDate);

        leaveRequest.ValidateAgainstPolicy(leaveType.LeavePolicy.DefaultBalance, approvedRequests, leaveType.LeavePolicy);

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync(cancellationToken);

        return leaveRequest.Id;
    }
}