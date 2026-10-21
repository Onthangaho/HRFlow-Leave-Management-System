using FluentValidation;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Application.Features.LeaveRequests.Queries.GetDepartmentLeaveReport;
using HRFlow.Application.Features.LeaveRequests.Queries.GetPersonalDashboard;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Models.Employees;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetHrWorkspace;
/// <summary>HR scope comes only from authentication; period filters do not narrow all-date operational attention.</summary>
public sealed record GetHrWorkspaceQuery(Guid ActorIdentityId, DateOnly Start, DateOnly End, Guid? DepartmentId) : IRequest<HrWorkspaceDto>;
/// <summary>Reuses the existing report's 366-day/date/department validation contract.</summary>
public sealed class GetHrWorkspaceQueryValidator : AbstractValidator<GetHrWorkspaceQuery>
{
    /// <summary>Keeps report and Overview selection limits identical.</summary>
    public GetHrWorkspaceQueryValidator() => RuleFor(r => new GetDepartmentLeaveReportQuery(r.ActorIdentityId, r.Start, r.End, r.DepartmentId))
        .SetValidator(new GetDepartmentLeaveReportQueryValidator());
}
/// <summary>Request preview excludes emails, evidence, notes, descriptions and personnel context.</summary>
public sealed record HrPendingDto(Guid RequestId, string EmployeeName, string LeaveTypeName, DateOnly StartDate, DateOnly EndDate);
/// <summary>Operational department staffing counts describe lifecycle, not activation or available capacity.</summary>
public sealed record HrDepartmentDto(Guid Id, string Name, int ActiveEmployees, int InactiveEmployees);
/// <summary>Counts separate active employment from Pending activation and delivery outcomes; categories may overlap.</summary>
public sealed record HrAttentionDto(int UnknownEmploymentFacts, int MissingCurrentSchedules, int FutureScheduleOnly,
    int PendingActivation, int DeliveryFailed, int DeliveryPending, ImportAttentionDto Imports);
/// <summary>Request counts are attributed once at max(request start, period start); days are clipped/split across monthly buckets.</summary>
public sealed record HrTrendDto(DateOnly Start, DateOnly End, int PendingRequests, int ApprovedRequests, long ApprovedRequestDays, long InactiveApprovedRequestDays);
/// <summary>One HR-authorized snapshot separates organisation operational counts from the applied period report.</summary>
public sealed record HrWorkspaceDto(DateTimeOffset AsOfUtc, DateOnly BusinessToday, string DateBasis, string DayUnit,
    int ActiveEmployees, int InactiveEmployees, int DepartmentCount, IReadOnlyList<HrDepartmentDto> OrganisationDepartments,
    int PendingCount, int PendingPreviewLimit, IReadOnlyList<HrPendingDto> Pending, HrAttentionDto Attention,
    DepartmentLeaveReportDto Report, IReadOnlyList<HrTrendDto> Trends);
/// <summary>Composes missing operational metrics with existing report calculations without any writer reservation or decision authority.</summary>
public sealed class GetHrWorkspaceQueryHandler(IApplicationDbContext context, CurrentAccountAuthorization authorization,
    ILeaveReportingReadTransaction reads, IHrWorkforceAttentionReader workforce, IHrImportAttentionReader imports, TimeProvider clock)
    : IRequestHandler<GetHrWorkspaceQuery, HrWorkspaceDto>
{
    private const int PreviewLimit = 5;
    /// <inheritdoc />
    public Task<HrWorkspaceDto> Handle(GetHrWorkspaceQuery request, CancellationToken cancellationToken) => reads.ExecuteAsync(async token =>
    {
        await authorization.RequireIdentityAsync(request.ActorIdentityId, [EmployeeRoles.HrAdministrator], token);
        var asOf = clock.GetUtcNow();
        var today = GetPersonalDashboardQueryHandler.BusinessDate(asOf);
        var staff = await workforce.ReadAsync(asOf, token);
        var pending = context.LeaveRequests.AsNoTracking().Where(r => r.Status == LeaveRequestStatus.Pending);
        var pendingCount = await pending.CountAsync(token);
        var rows = await pending.OrderBy(r => r.StartDate).ThenBy(r => r.Id).Take(PreviewLimit)
            .Select(r => new { r.Id, r.Employee.FullName, Type = r.LeaveType.Name, r.StartDate, r.EndDate }).ToListAsync(token);
        var report = await GetDepartmentLeaveReportQueryHandler.ReadAsync(context, request.Start, request.End, request.DepartmentId, token);
        var start = request.Start.ToDateTime(TimeOnly.MinValue); var end = request.End.ToDateTime(TimeOnly.MaxValue);
        var trendRows = await context.LeaveRequests.AsNoTracking().Where(r => (r.Status == LeaveRequestStatus.Pending || r.Status == LeaveRequestStatus.Approved)
            && r.StartDate <= end && r.EndDate >= start && (!request.DepartmentId.HasValue || r.Employee.DepartmentId == request.DepartmentId))
            .Select(r => new GetDepartmentLeaveReportQueryHandler.ReportRow(r.EmployeeId, r.Employee.DepartmentId, r.Employee.IsActive, r.Status, r.StartDate, r.EndDate)).ToListAsync(token);
        var trends = new List<HrTrendDto>();
        for (var cursor = request.Start; cursor <= request.End;)
        {
            var monthEnd = new DateOnly(cursor.Year, cursor.Month, DateTime.DaysInMonth(cursor.Year, cursor.Month));
            var bucketEnd = monthEnd < request.End ? monthEnd : request.End;
            var matching = trendRows.Where(r => DateOnly.FromDateTime(r.StartDate) <= bucketEnd && DateOnly.FromDateTime(r.EndDate) >= cursor);
            var days = GetDepartmentLeaveReportQueryHandler.Calculate(matching, cursor, bucketEnd);
            var attributed = trendRows.Where(r => {
                var date = DateOnly.FromDateTime(r.StartDate); if (date < request.Start) date = request.Start;
                return date >= cursor && date <= bucketEnd;
            }).ToArray();
            trends.Add(new(cursor, bucketEnd, attributed.Count(r => r.Status == LeaveRequestStatus.Pending),
                attributed.Count(r => r.Status == LeaveRequestStatus.Approved), days.ApprovedRequestDays, days.InactiveApprovedRequestDays));
            if (bucketEnd == request.End) break;
            cursor = bucketEnd.AddDays(1);
        }
        return new HrWorkspaceDto(asOf, today, "Africa/Johannesburg (SAST, UTC+02:00), server business date", "summed Approved inclusive calendar request-days",
            staff.ActiveEmployees, staff.InactiveEmployees, staff.Departments.Count, staff.Departments, pendingCount, PreviewLimit,
            rows.Select(r => new HrPendingDto(r.Id, r.FullName, r.Type, DateOnly.FromDateTime(r.StartDate), DateOnly.FromDateTime(r.EndDate))).ToArray(),
            new(staff.UnknownEmploymentFacts, staff.MissingCurrentSchedules, staff.FutureScheduleOnly,
                staff.PendingActivation, staff.DeliveryFailed, staff.DeliveryPending, await imports.ReadAsync(request.ActorIdentityId, asOf.UtcDateTime, token)), report, trends);
    }, cancellationToken);
}
