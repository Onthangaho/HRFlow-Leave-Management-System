using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Application.Features.LeaveRequests;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;

/// <summary>
/// Handles leave request submission with leave policy validation against approved requests and available balance.
/// </summary>
public class SubmitLeaveRequestCommandHandler : IRequestHandler<SubmitLeaveRequestCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly ILeaveApprovalAuthorizationService _leaveApprovalAuthorizationService;

    public SubmitLeaveRequestCommandHandler(
        IApplicationDbContext context,
        ILeaveApprovalAuthorizationService leaveApprovalAuthorizationService)
    {
        _context = context;
        _leaveApprovalAuthorizationService = leaveApprovalAuthorizationService;
    }

    public async Task<Guid> Handle(SubmitLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .SingleOrDefaultAsync(employee => employee.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee was not found.");

        await _leaveApprovalAuthorizationService.EnsureEmployeeHasValidManagerAsync(employee, cancellationToken);

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

        var balance = LeaveBalanceCalculator.Calculate(
            leaveType.LeavePolicy.DefaultBalance,
            approvedRequests);

        leaveRequest.ValidateAgainstPolicy(
            balance.RemainingDays,
            approvedRequests,
            leaveType.LeavePolicy);

        _context.LeaveRequests.Add(leaveRequest);
        await _context.SaveChangesAsync(cancellationToken);

        return leaveRequest.Id;
    }
}