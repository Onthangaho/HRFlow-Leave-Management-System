using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;

/// <summary>Rejects a pending request under the same database protection used by approval and cancellation.</summary>
public sealed class RejectLeaveRequestCommandHandler : IRequestHandler<RejectLeaveRequestCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ILeaveDecisionTransaction _decisionTransaction;
    private readonly ILeaveApprovalAuthorizationService _authorization;

    /// <summary>Shares the scoped persistence and authorization services with the decision transaction.</summary>
    public RejectLeaveRequestCommandHandler(
        IApplicationDbContext context,
        ILeaveDecisionTransaction decisionTransaction,
        ILeaveApprovalAuthorizationService authorization)
    {
        _context = context;
        _decisionTransaction = decisionTransaction;
        _authorization = authorization;
    }

    /// <summary>Acquires protection before loading the state used to authorize and validate the decision.</summary>
    public Task Handle(RejectLeaveRequestCommand request, CancellationToken cancellationToken) =>
        _decisionTransaction.ExecuteAsync(token => DecideAsync(request, token), cancellationToken);

    private async Task DecideAsync(RejectLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await _context.LeaveRequests
            .Include(current => current.Employee)
            .SingleOrDefaultAsync(current => current.Id == request.LeaveRequestId, cancellationToken)
            ?? throw new NotFoundException("Leave request was not found.");

        var actor = await _context.Employees
            .SingleOrDefaultAsync(employee => employee.Id == request.RejectorId, cancellationToken)
            ?? throw new NotFoundException("Decision maker was not found.");
        await _authorization.EnsureManagerCanDecideAsync(actor, leaveRequest.Employee, cancellationToken);

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            throw new LeaveDecisionConflictException("Only pending leave requests can be decided. Refresh the request.");
        }

        leaveRequest.Reject(request.RejectorId);
    }
}
