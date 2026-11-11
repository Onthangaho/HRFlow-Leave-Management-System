using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Models.Employees;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetPendingLeaveRequests;

/// <summary>
/// Retrieves HR's read-only organisation-wide Pending monitoring view. Manager reads use the bounded queue.
/// </summary>
public class GetPendingLeaveRequestsQuery : IRequest<IReadOnlyList<PendingLeaveRequestDto>>
{
    public string Status { get; set; } = string.Empty;
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
    /// Returns organization-wide HR monitoring only; this is not a Manager pagination fallback.
    /// </summary>
    public async Task<IReadOnlyList<PendingLeaveRequestDto>> Handle(GetPendingLeaveRequestsQuery request, CancellationToken cancellationToken)
    {
        return await _readTransaction.ExecuteAsync<IReadOnlyList<PendingLeaveRequestDto>>(async token =>
        {
            cancellationToken = token;
            await _authorization.RequireIdentityAsync(request.ActorIdentityId, [EmployeeRoles.HrAdministrator], cancellationToken);

            var query = _context.LeaveRequests
                .AsNoTracking()
                .Where(lr => lr.Status == LeaveRequestStatus.Pending);

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
