using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Models.Employees;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetPendingLeaveRequests;

/// <summary>
/// Retrieves pending requests for either a manager's actionable direct-report queue or HR's read-only
/// organisation-wide monitoring view.
/// </summary>
public class GetPendingLeaveRequestsQuery : IRequest<IReadOnlyList<PendingLeaveRequestDto>>
{
    public string Status { get; set; } = string.Empty;
    public Guid CurrentEmployeeId { get; set; }
    public bool IsOrganisationMonitoring { get; set; }
    /// <summary>Supplied by authentication for HR monitoring, never a client-selected scope.</summary>
    public Guid ActorIdentityId { get; set; }
}

/// <summary>
/// Keeps current permissions, reporting scope and queue projection in one read snapshot.
/// </summary>
public class GetPendingLeaveRequestsQueryHandler : IRequestHandler<GetPendingLeaveRequestsQuery, IReadOnlyList<PendingLeaveRequestDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ILeaveReportingReadTransaction _readTransaction;
    private readonly CurrentAccountAuthorization _authorization;

    /// <summary>
    /// Creates the handler with database access needed to filter pending requests by reporting lines.
    /// </summary>
    public GetPendingLeaveRequestsQueryHandler(IApplicationDbContext context, ILeaveReportingReadTransaction readTransaction, CurrentAccountAuthorization authorization)
    {
        _context = context;
        _readTransaction = readTransaction;
        _authorization = authorization;
    }

    /// <summary>
    /// Returns all pending requests for HR monitoring, or only same-department direct-report requests
    /// for a manager queue. Decision commands recheck the shared reporting rule before mutating state.
    /// </summary>
    public async Task<IReadOnlyList<PendingLeaveRequestDto>> Handle(GetPendingLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        return await _readTransaction.ExecuteAsync<IReadOnlyList<PendingLeaveRequestDto>>(async token =>
        {
            cancellationToken = token;
            var actor = request.IsOrganisationMonitoring
                ? await _authorization.RequireIdentityAsync(request.ActorIdentityId, [EmployeeRoles.HrAdministrator], cancellationToken)
                : await _authorization.RequireEmployeeAsync(request.CurrentEmployeeId, [EmployeeRoles.Manager], cancellationToken);

            var query = _context.LeaveRequests
                .AsNoTracking()
                .Where(lr => lr.Status == LeaveRequestStatus.Pending);

            if (!request.IsOrganisationMonitoring)
            {
                query = query.Where(lr =>
                    lr.Employee.ManagerId == actor.Id
                    && lr.EmployeeId != actor.Id
                    && lr.Employee.DepartmentId == actor.DepartmentId);
            }

            return await query
                .OrderBy(lr => lr.StartDate)
                .ThenBy(lr => lr.Id)
                .Select(lr => new PendingLeaveRequestDto
                {
                    Id = lr.Id,
                    EmployeeId = lr.EmployeeId,
                    EmployeeFullName = lr.Employee.FullName,
                    EmployeeEmail = lr.Employee.Email,
                    LeaveTypeId = lr.LeaveTypeId,
                    LeaveTypeName = lr.LeaveType.Name,
                    StartDate = lr.StartDate,
                    EndDate = lr.EndDate,
                    Status = lr.Status.ToString()
                })
                .ToListAsync(cancellationToken);
        }, cancellationToken);
    }
}
