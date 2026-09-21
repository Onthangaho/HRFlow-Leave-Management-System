using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetPendingLeaveRequests;

/// <summary>
/// Retrieves the manager approval queue while ensuring managers only see pending requests from their
/// direct reports, and HR administrators can review the full pending queue for operational oversight.
/// </summary>
public class GetPendingLeaveRequestsQuery : IRequest<IReadOnlyList<PendingLeaveRequestDto>>
{
    public string Status { get; set; } = string.Empty;
    public Guid CurrentEmployeeId { get; set; }
    public bool IsHrAdministrator { get; set; }
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
    /// Returns all pending requests for HR administrators, or only direct-report pending requests for managers.
    /// </summary>
    public async Task<IReadOnlyList<PendingLeaveRequestDto>> Handle(GetPendingLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.LeaveRequests
            .AsNoTracking()
            .Where(lr => lr.Status == LeaveRequestStatus.Pending);

        if (!request.IsHrAdministrator)
        {
            query = query.Where(lr => lr.Employee.ManagerId == request.CurrentEmployeeId);
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