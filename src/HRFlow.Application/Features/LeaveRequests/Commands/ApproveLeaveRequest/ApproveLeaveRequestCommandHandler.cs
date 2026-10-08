using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Features.LeaveRequests;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;

/// <summary>Rechecks approval policy and permissions under database protection so competing requests cannot overspend leave.</summary>
public sealed class ApproveLeaveRequestCommandHandler : IRequestHandler<ApproveLeaveRequestCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IRequestCorrelationContext _correlation;
    private readonly ILeaveDecisionTransaction _decisionTransaction;
    private readonly ILeaveApprovalAuthorizationService _authorization;

    /// <summary>Shares the scoped persistence and authorization services with the decision transaction.</summary>
    public ApproveLeaveRequestCommandHandler(
        IApplicationDbContext context,
        IRequestCorrelationContext correlation,
        ILeaveDecisionTransaction decisionTransaction,
        ILeaveApprovalAuthorizationService authorization)
    {
        _context = context;
        _correlation = correlation;
        _decisionTransaction = decisionTransaction;
        _authorization = authorization;
    }

    /// <summary>Acquires protection before loading the state used to authorize and validate the decision.</summary>
    public Task Handle(ApproveLeaveRequestCommand request, CancellationToken cancellationToken) =>
        _decisionTransaction.ExecuteAsync(token => DecideAsync(request, token), cancellationToken);

    private async Task DecideAsync(ApproveLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await _context.LeaveRequests
            .Include(current => current.Employee)
            .SingleOrDefaultAsync(current => current.Id == request.LeaveRequestId, cancellationToken)
            ?? throw new NotFoundException("Leave request was not found.");

        var actor = await _context.Employees
            .SingleOrDefaultAsync(employee => employee.Id == request.ApproverId, cancellationToken)
            ?? throw new NotFoundException("Decision maker was not found.");
        await _authorization.EnsureManagerCanDecideAsync(actor, leaveRequest.Employee, cancellationToken);

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            throw new LeaveDecisionConflictException("Only pending leave requests can be decided. Refresh the request.");
        }

        var leaveType = await _context.LeaveTypes
            .Include(type => type.LeavePolicy)
            .SingleOrDefaultAsync(type => type.Id == leaveRequest.LeaveTypeId, cancellationToken)
            ?? throw new NotFoundException("Leave type was not found.");
        var approvedRequests = await _context.LeaveRequests.AsNoTracking()
            .Where(current => current.EmployeeId == leaveRequest.EmployeeId
                && current.LeaveTypeId == leaveRequest.LeaveTypeId
                && current.Status == LeaveRequestStatus.Approved)
            .ToListAsync(cancellationToken);
        var balance = LeaveBalanceCalculator.Calculate(leaveType.LeavePolicy.DefaultBalance, approvedRequests);
        leaveRequest.ValidateAgainstPolicy(balance.RemainingDays, approvedRequests, leaveType.LeavePolicy);

        leaveRequest.Approve(request.ApproverId, _correlation.CorrelationId, request.DecisionNote);
    }
}
