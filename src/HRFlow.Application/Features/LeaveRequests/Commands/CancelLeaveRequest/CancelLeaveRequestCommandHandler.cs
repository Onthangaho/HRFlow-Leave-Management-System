using HRFlow.Application.Exceptions;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Commands.CancelLeaveRequest;

/// <summary>
/// Withdraws an employee-owned pending leave request while ensuring decided requests remain
/// immutable and every cancellation has an audit record.
/// </summary>
public sealed class CancelLeaveRequestCommandHandler : IRequestHandler<CancelLeaveRequestCommand>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Creates the handler with persistence access for the leave request and its audit event.
    /// </summary>
    public CancelLeaveRequestCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Cancels a pending request only when the authenticated employee owns it.
    /// </summary>
    public async Task Handle(CancelLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        var leaveRequest = await _context.LeaveRequests
            .FirstOrDefaultAsync(
                currentRequest => currentRequest.Id == request.LeaveRequestId,
                cancellationToken);

        if (leaveRequest is null)
        {
            throw new NotFoundException("Leave request was not found.");
        }

        if (leaveRequest.EmployeeId != request.EmployeeId)
        {
            throw new ForbiddenException("You can only cancel your own leave requests.");
        }

        var oldStatus = leaveRequest.Status;
        leaveRequest.Cancel(request.EmployeeId);
        var auditEntry = AuditEntry.Create(
            leaveRequest.Id,
            request.EmployeeId,
            "Cancel",
            oldStatus,
            leaveRequest.Status);

        _context.AuditEntries.Add(auditEntry);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
