using FluentValidation;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Application.Features.LeaveRequests.Queries.GetDepartmentLeaveReport;
using HRFlow.Application.Features.LeaveRequests.Queries.GetPersonalDashboard;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Models.Auth;
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
    ILeaveReportingReadTransaction reads, IAccountActivationService activation, IHrImportAttentionReader imports, TimeProvider clock)
    : IRequestHandler<GetHrWorkspaceQuery, HrWorkspaceDto>
{
    private const int PreviewLimit = 5;
    /// <inheritdoc />
    public Task<HrWorkspaceDto> Handle(GetHrWorkspaceQuery request, CancellationToken cancellationToken) => reads.ExecuteAsync(async token =>
    {
        await authorization.RequireIdentityAsync(request.ActorIdentityId, [EmployeeRoles.HrAdministrator], token);
        var asOf = clock.GetUtcNow();
        var today = GetPersonalDashboardQueryHandler.BusinessDate(asOf);
        var employees = await context.Employees.AsNoTracking().ToListAsync(token);
        var schedules = await context.WeeklyScheduleRevisions.AsNoTracking().Select(s => new { s.EmployeeId, s.EffectiveFrom }).ToListAsync(token);
        var scheduleDates = schedules.ToLookup(s => s.EmployeeId, s => s.EffectiveFrom);
        var flags = employees.Select(e => EmploymentAttention.For(e, today, scheduleDates[e.Id])).ToArray();
        var pendingActivation = 0; var failed = 0; var prepared = 0;
        foreach (var employee in employees.Where(e => e.IsActive))
        {
            var state = await activation.GetStatusAsync(employee.IdentityUserId, token);
            if (!state.RequiresActivation) continue;
            pendingActivation++;
            if (state.DeliveryState is ActivationDeliveryStates.DeliveryFailed or ActivationDeliveryStates.DeliveryUnknown) failed++;
            if (state.DeliveryState == ActivationDeliveryStates.PendingDelivery) prepared++;
        }
        var departments = await context.Departments.AsNoTracking().OrderBy(d => d.Name).ThenBy(d => d.Id)
            .Select(d => new { d.Id, d.Name }).ToListAsync(token);
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
            employees.Count(e => e.IsActive), employees.Count(e => !e.IsActive), departments.Count,
            departments.Select(d => new HrDepartmentDto(d.Id, d.Name, employees.Count(e => e.DepartmentId == d.Id && e.IsActive),
                employees.Count(e => e.DepartmentId == d.Id && !e.IsActive))).ToArray(), pendingCount, PreviewLimit,
            rows.Select(r => new HrPendingDto(r.Id, r.FullName, r.Type, DateOnly.FromDateTime(r.StartDate), DateOnly.FromDateTime(r.EndDate))).ToArray(),
            new(flags.Count(f => f.NeedsEmploymentFacts), flags.Count(f => f.NeedsCurrentSchedule), flags.Count(f => f.HasFutureScheduleOnly),
                pendingActivation, failed, prepared, await imports.ReadAsync(request.ActorIdentityId, asOf.UtcDateTime, token)), report, trends);
    }, cancellationToken);
}
