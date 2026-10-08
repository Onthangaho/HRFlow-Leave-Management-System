using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Models.Employees;
using HRFlow.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetEmployeeLeaveHistory;

/// <summary>
/// Retrieves an employee's own leave-request timeline, including recorded decisions, without
/// exposing leave records that belong to other employees.
/// </summary>
public sealed class GetEmployeeLeaveHistoryQuery : IRequest<IReadOnlyList<LeaveRequestHistoryDto>>
{
    /// <summary>
    /// Gets or sets the employee whose personal leave history should be returned.
    /// </summary>
    public Guid EmployeeId { get; set; }
}

/// <summary>
/// Handles self-scoped leave history projection so the API can return display-ready leave and
/// audit information without clients querying individual aggregates.
/// </summary>
public sealed class GetEmployeeLeaveHistoryQueryHandler
    : IRequestHandler<GetEmployeeLeaveHistoryQuery, IReadOnlyList<LeaveRequestHistoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILeaveReportingReadTransaction _readTransaction;
    private readonly CurrentAccountAuthorization _authorization;

    /// <summary>
    /// Creates the handler with the database context used to read the employee's requests and audits.
    /// </summary>
    public GetEmployeeLeaveHistoryQueryHandler(IApplicationDbContext context, ILeaveReportingReadTransaction readTransaction, CurrentAccountAuthorization authorization)
    {
        _context = context;
        _readTransaction = readTransaction;
        _authorization = authorization;
    }

    /// <summary>
    /// Returns newest requests first and retains their audit entries so employees can see how final
    /// decisions were reached.
    /// </summary>
    public async Task<IReadOnlyList<LeaveRequestHistoryDto>> Handle(
        GetEmployeeLeaveHistoryQuery request,
        CancellationToken cancellationToken)
    {
        return await _readTransaction.ExecuteAsync<IReadOnlyList<LeaveRequestHistoryDto>>(async token =>
        {
            cancellationToken = token;
            await _authorization.RequireEmployeeAsync(request.EmployeeId, [EmployeeRoles.Employee, EmployeeRoles.Manager], cancellationToken);

            return await _context.LeaveRequests
                .AsNoTracking()
                .Where(leaveRequest => leaveRequest.EmployeeId == request.EmployeeId)
                .OrderByDescending(leaveRequest => leaveRequest.StartDate)
                .ThenByDescending(leaveRequest => leaveRequest.Id)
                .Select(leaveRequest => new LeaveRequestHistoryDto
                {
                    Id = leaveRequest.Id,
                    LeaveTypeName = leaveRequest.LeaveType.Name,
                    StartDate = leaveRequest.StartDate,
                    EndDate = leaveRequest.EndDate,
                    Status = leaveRequest.Status.ToString(),
                    ProcessedOn = leaveRequest.ProcessedOn,
                    DecisionHistory = leaveRequest.AuditEntries
                        .OrderBy(auditEntry => auditEntry.Timestamp)
                        .ThenBy(auditEntry => auditEntry.Id)
                        .Select(auditEntry => new LeaveRequestDecisionDto
                        {
                            Action = auditEntry.Action,
                            DecisionNote = auditEntry.DecisionNote,
                            ActorFullName = auditEntry.Actor.FullName,
                            Timestamp = DateTime.SpecifyKind(auditEntry.Timestamp, DateTimeKind.Utc),
                            OldStatus = auditEntry.OldStatus.HasValue
                                ? auditEntry.OldStatus.Value.ToString()
                                : null,
                            NewStatus = auditEntry.NewStatus.ToString()
                        })
                        .ToList()
                })
                .ToListAsync(cancellationToken);
        }, cancellationToken);
    }
}
