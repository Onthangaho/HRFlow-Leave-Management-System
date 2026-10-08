using HRFlow.Application.Services;
using HRFlow.Domain.Models.Employees;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Commands.CancelLeaveRequest;

/// <summary>Withdraws an owner's pending request under database protection so it cannot race a manager decision.</summary>
public sealed class CancelLeaveRequestCommandHandler : IRequestHandler<CancelLeaveRequestCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly ILeaveNotificationOutbox _notifications;
    private readonly CurrentAccountAuthorization _authorization;
    private readonly IRequestCorrelationContext _correlation;
    private readonly ILeaveDecisionTransaction _decisionTransaction;

    /// <summary>Shares the scoped persistence services with the decision transaction.</summary>
    public CancelLeaveRequestCommandHandler(
        IApplicationDbContext context,
        ILeaveNotificationOutbox notifications,
        CurrentAccountAuthorization authorization,
        IRequestCorrelationContext correlation,
        ILeaveDecisionTransaction decisionTransaction)
    {
        _context = context;
        _notifications = notifications;
        _authorization = authorization;
        _correlation = correlation;
        _decisionTransaction = decisionTransaction;
    }

    /// <summary>Acquires protection before loading the state used to authorize and validate the decision.</summary>
    public Task Handle(CancelLeaveRequestCommand request, CancellationToken cancellationToken) =>
        _decisionTransaction.ExecuteAsync(token => DecideAsync(request, token), cancellationToken);

    private async Task DecideAsync(CancelLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        await _authorization.RequireEmployeeAsync(request.EmployeeId, [EmployeeRoles.Employee, EmployeeRoles.Manager], cancellationToken);
        var leaveRequest = await _context.LeaveRequests
            .SingleOrDefaultAsync(current => current.Id == request.LeaveRequestId, cancellationToken)
            ?? throw new NotFoundException("Leave request was not found.");

        if (leaveRequest.EmployeeId != request.EmployeeId)
        {
            throw new ForbiddenException("You can only cancel your own leave requests.");
        }

        if (leaveRequest.Status != LeaveRequestStatus.Pending)
        {
            throw new LeaveDecisionConflictException("Only pending leave requests can be decided. Refresh the request.");
        }

        leaveRequest.Cancel(request.EmployeeId, correlationId: _correlation.CorrelationId);
        await _notifications.TransitionAsync(leaveRequest, cancellationToken);
    }
}
