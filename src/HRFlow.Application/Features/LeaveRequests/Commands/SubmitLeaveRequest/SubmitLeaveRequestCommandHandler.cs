using HRFlow.Application.Services;
using HRFlow.Domain.Models.Employees;
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
    private readonly ILeaveNotificationOutbox _notifications;
    private readonly CurrentAccountAuthorization _authorization;
    private readonly IRequestCorrelationContext _correlation;
    private readonly ILeaveApprovalAuthorizationService _leaveApprovalAuthorizationService;
    private readonly ILeaveConfigurationTransaction _transaction;
    private readonly SupportingDocumentService _documents;

    /// <summary>Shares the context and database reservation used by policy management and approvals.</summary>
    public SubmitLeaveRequestCommandHandler(
        IApplicationDbContext context,
        ILeaveNotificationOutbox notifications,
        CurrentAccountAuthorization authorization,
        IRequestCorrelationContext correlation,
        ILeaveApprovalAuthorizationService leaveApprovalAuthorizationService,
        ILeaveConfigurationTransaction transaction, SupportingDocumentService documents)
    {
        _context = context;
        _notifications = notifications;
        _authorization = authorization;
        _correlation = correlation;
        _leaveApprovalAuthorizationService = leaveApprovalAuthorizationService;
        _transaction = transaction;
        _documents = documents;
    }

    /// <summary>Acquires writer protection before authoritative policy and relationship reads.</summary>
    public async Task<Guid> Handle(SubmitLeaveRequestCommand request, CancellationToken cancellationToken)
    {
        Guid id = Guid.Empty;
        var verified = await _documents.VerifyBindingAsync(request.EmployeeId, request.DocumentIds, cancellationToken);
        await _transaction.ExecuteAsync(async token => id = await SubmitAsync(request, verified, token), cancellationToken);
        return id;
    }

    private async Task<Guid> SubmitAsync(SubmitLeaveRequestCommand request, IReadOnlyDictionary<Guid, Guid> verified, CancellationToken cancellationToken)
    {
        var employee = await _context.Employees
            .SingleOrDefaultAsync(employee => employee.Id == request.EmployeeId, cancellationToken)
            ?? throw new NotFoundException("Employee was not found.");

        await _authorization.RequireEmployeeAsync(request.EmployeeId, [EmployeeRoles.Employee, EmployeeRoles.Manager], cancellationToken);
        await _leaveApprovalAuthorizationService.EnsureEmployeeHasValidManagerAsync(employee, cancellationToken);

        var leaveType = await _context.LeaveTypes
            .Include(lt => lt.LeavePolicy)
            .SingleOrDefaultAsync(lt => lt.Id == request.LeaveTypeId, cancellationToken)
            ?? throw new WriteConflictException("This leave type is no longer available. Reload the leave types before submitting.");

        var approvedRequests = await _context.LeaveRequests
            .Where(lr => lr.EmployeeId == request.EmployeeId &&
                         lr.LeaveTypeId == request.LeaveTypeId &&
                         lr.Status == Domain.Enums.LeaveRequestStatus.Approved)
            .ToListAsync(cancellationToken);

        var leaveRequest = LeaveRequest.Create(
            request.EmployeeId,
            request.LeaveTypeId,
            request.StartDate,
            request.EndDate, _correlation.CorrelationId);

        var balance = LeaveBalanceCalculator.Calculate(
            leaveType.LeavePolicy.DefaultBalance,
            approvedRequests);

        leaveRequest.ValidateAgainstPolicy(
            balance.RemainingDays,
            approvedRequests,
            leaveType.LeavePolicy);

        _context.LeaveRequests.Add(leaveRequest);
        await _documents.BindAsync(employee.Id, leaveRequest.Id, request.DocumentIds, verified, cancellationToken);
        await _notifications.TransitionAsync(leaveRequest, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        return leaveRequest.Id;
    }
}
