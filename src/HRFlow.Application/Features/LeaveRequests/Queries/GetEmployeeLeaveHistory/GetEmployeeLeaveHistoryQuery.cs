using HRFlow.Application.Interfaces;
using HRFlow.Domain.Enums;
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
public sealed class GetEmployeeLeaveHistoryQuery : IRequest<LeaveHistoryPage>
{
    /// <summary>
    /// Receives only the authenticated Identity actor, resolved again inside the protected snapshot.
    /// </summary>
    public Guid ActorIdentityId { get; set; }
    /// <summary>Applied bounded filters; no caller-selected employee scope.</summary>
    public LeaveHistoryFilter Filter { get; set; } = new();
}

/// <summary>
/// Handles self-scoped leave history projection so the API can return display-ready leave and
/// audit information without clients querying individual aggregates.
/// </summary>
public sealed class GetEmployeeLeaveHistoryQueryHandler
    : IRequestHandler<GetEmployeeLeaveHistoryQuery, LeaveHistoryPage>
{
    private readonly IApplicationDbContext _context;
    private readonly ILeaveReportingReadTransaction _readTransaction;
    private readonly CurrentAccountAuthorization _authorization;
    private readonly TimeProvider _clock;

    /// <summary>
    /// Creates the handler with the database context used to read the employee's requests and audits.
    /// </summary>
    public GetEmployeeLeaveHistoryQueryHandler(IApplicationDbContext context, ILeaveReportingReadTransaction readTransaction, CurrentAccountAuthorization authorization, TimeProvider clock)
    {
        _context = context;
        _readTransaction = readTransaction;
        _authorization = authorization;
        _clock = clock;
    }

    /// <summary>
    /// Returns newest requests first and retains their audit entries so employees can see how final
    /// decisions were reached.
    /// </summary>
    public async Task<LeaveHistoryPage> Handle(
        GetEmployeeLeaveHistoryQuery request,
        CancellationToken cancellationToken)
    {
        return await _readTransaction.ExecuteAsync<LeaveHistoryPage>(async token =>
        {
            cancellationToken = token;
            var employee = await _authorization.RequireIdentityAsync(request.ActorIdentityId, [EmployeeRoles.Employee, EmployeeRoles.Manager], cancellationToken);
            var captured = _clock.GetUtcNow();
            var filter = request.Filter;
            var query = _context.LeaveRequests.AsNoTracking().Where(r => r.EmployeeId == employee.Id);
            if (filter.Status != null) { var status = Enum.Parse<LeaveRequestStatus>(filter.Status); query = query.Where(r => r.Status == status); }
            if (filter.LeaveTypeId.HasValue) query = query.Where(r => r.LeaveTypeId == filter.LeaveTypeId.Value);
            if (filter.Start.HasValue) { var start = filter.Start.Value.ToDateTime(TimeOnly.MinValue); query = query.Where(r => r.EndDate >= start); }
            if (filter.End.HasValue) { var end = filter.End.Value.ToDateTime(TimeOnly.MinValue); query = query.Where(r => r.StartDate <= end); }
            var total = await query.CountAsync(cancellationToken);
            // Clamp an empty last page after mutations, without overflow or an enormous Skip.
            var pages = Math.Max(1, (int)(((long)total + filter.PageSize - 1) / filter.PageSize));
            var page = Math.Min(filter.Page, pages);

            var items = await query
                .OrderByDescending(leaveRequest => leaveRequest.StartDate)
                .ThenByDescending(leaveRequest => leaveRequest.Id)
                .Skip((page - 1) * filter.PageSize).Take(filter.PageSize)
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
            return new LeaveHistoryPage(items, total, page, filter.PageSize, pages, captured);
        }, cancellationToken);
    }
}
