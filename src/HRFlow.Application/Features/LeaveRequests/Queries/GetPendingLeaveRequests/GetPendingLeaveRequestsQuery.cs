using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Models.Employees;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetPendingLeaveRequests;

/// <summary>Reads bounded HR monitoring, never a Manager decision scope or caller-selected actor.</summary>
public sealed class GetPendingLeaveRequestsQuery : IRequest<HrPendingMonitoringPage>
{
    /// <summary>Supplied exclusively by authentication and rechecked inside the snapshot.</summary>
    public Guid ActorIdentityId { get; set; }
    /// <summary>Validated applied filters, with no unbounded default.</summary>
    public HrMonitoringFilter Filter { get; set; } = new();
}

/// <summary>Authorizes, counts and pages before materialization in one deferred transaction without reserving a writer.</summary>
public sealed class GetPendingLeaveRequestsQueryHandler(IApplicationDbContext context,
    ILeaveReportingReadTransaction reads, CurrentAccountAuthorization authorization, TimeProvider clock)
    : IRequestHandler<GetPendingLeaveRequestsQuery, HrPendingMonitoringPage>
{
    /// <inheritdoc />
    public Task<HrPendingMonitoringPage> Handle(GetPendingLeaveRequestsQuery request, CancellationToken cancellationToken) =>
        reads.ExecuteAsync(async token =>
        {
            await authorization.RequireIdentityAsync(request.ActorIdentityId, [EmployeeRoles.HrAdministrator], token);
            var captured = clock.GetUtcNow();
            var filter = request.Filter;
            var query = context.LeaveRequests.AsNoTracking().Where(r => r.Status == LeaveRequestStatus.Pending);
            var search = filter.Search?.Trim() ?? "";
            if (search.Length > 0)
            {
                // LIKE metacharacters are escaped so supplied text remains a literal substring.
                var pattern = "%" + search.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_") + "%";
                query = query.Where(r => EF.Functions.Like(r.Employee.FullName, pattern, "\\")
                    || EF.Functions.Like(r.Employee.Email, pattern, "\\"));
            }
            if (filter.DepartmentId.HasValue) query = query.Where(r => r.Employee.DepartmentId == filter.DepartmentId.Value);
            if (filter.LeaveTypeId.HasValue) query = query.Where(r => r.LeaveTypeId == filter.LeaveTypeId.Value);
            if (filter.Start.HasValue) { var start = filter.Start.Value.ToDateTime(TimeOnly.MinValue); query = query.Where(r => r.EndDate >= start); }
            if (filter.End.HasValue) { var end = filter.End.Value.ToDateTime(TimeOnly.MinValue); query = query.Where(r => r.StartDate <= end); }
            var total = await query.CountAsync(token);
            var pages = Math.Max(1, (int)(((long)total + filter.PageSize - 1) / filter.PageSize));
            var page = Math.Min(filter.Page, pages);
            var items = await query.OrderBy(r => r.StartDate).ThenBy(r => r.Id)
                .Skip((page - 1) * filter.PageSize).Take(filter.PageSize)
                .Select(r => new PendingLeaveRequestDto
                {
                    Id = r.Id, EmployeeId = r.EmployeeId, EmployeeFullName = r.Employee.FullName,
                    EmployeeEmail = r.Employee.Email, LeaveTypeId = r.LeaveTypeId, LeaveTypeName = r.LeaveType.Name,
                    StartDate = r.StartDate, EndDate = r.EndDate, Status = r.Status.ToString()
                }).ToListAsync(token);
            return new HrPendingMonitoringPage(items, total, page, filter.PageSize, pages, captured);
        }, cancellationToken);
}
