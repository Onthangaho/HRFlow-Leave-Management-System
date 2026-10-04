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
    public Guid CurrentDepartmentId { get; set; }
}

/// <summary>
/// Projects pending leave requests into a queue-friendly response shape so the API can enforce role-aware
/// scoping without leaking entity-tracking concerns to controllers.
/// </summary>
public class GetPendingLeaveRequestsQueryHandler : IRequestHandler<GetPendingLeaveRequestsQuery, IReadOnlyList<PendingLeaveRequestDto>>
{
    private readonly IApplicationDbContext _context;

    /// <summary>
    /// Creates the handler with database access needed to filter pending requests by reporting lines.
    /// </summary>
    public GetPendingLeaveRequestsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// Returns all pending requests for HR monitoring, or only same-department direct-report requests
    /// for a manager queue. Decision commands recheck the shared reporting rule before mutating state.
    /// </summary>
    public async Task<IReadOnlyList<PendingLeaveRequestDto>> Handle(GetPendingLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.LeaveRequests
            .AsNoTracking()
            .Where(lr => lr.Status == LeaveRequestStatus.Pending);

        if (!request.IsOrganisationMonitoring)
        {
            query = query.Where(lr =>
                lr.Employee.ManagerId == request.CurrentEmployeeId
                && lr.Employee.DepartmentId == request.CurrentDepartmentId);
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
    }
}