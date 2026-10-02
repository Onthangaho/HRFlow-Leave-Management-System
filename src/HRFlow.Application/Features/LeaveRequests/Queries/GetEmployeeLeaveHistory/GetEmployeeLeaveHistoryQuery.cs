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

    /// <summary>
    /// Creates the handler with the database context used to read the employee's requests and audits.
    /// </summary>
    public GetEmployeeLeaveHistoryQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns newest requests first and retains their audit entries so employees can see how final
    /// decisions were reached.
    /// </summary>
    public async Task<IReadOnlyList<LeaveRequestHistoryDto>> Handle(
        GetEmployeeLeaveHistoryQuery request,
        CancellationToken cancellationToken)
    {
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
                    .Select(auditEntry => new LeaveRequestDecisionDto
                    {
                        Action = auditEntry.Action,
                        ActorFullName = auditEntry.Actor.FullName,
                        Timestamp = auditEntry.Timestamp,
                        OldStatus = auditEntry.OldStatus.HasValue
                            ? auditEntry.OldStatus.Value.ToString()
                            : null,
                        NewStatus = auditEntry.NewStatus.ToString()
                    })
                    .ToList()
            })
            .ToListAsync(cancellationToken);
    }
}
