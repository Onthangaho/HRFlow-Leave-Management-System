using HRFlow.Application.Features.LeaveRequests.Queries.GetLeaveBalances;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Models.Employees;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetPersonalDashboard;

/// <summary>Identity scope is supplied by authentication, never by a client-selected employee.</summary>
public sealed record GetPersonalDashboardQuery(Guid ActorIdentityId) : IRequest<PersonalDashboardDto>;
/// <summary>Minimal owner request facts; sensitive descriptions, notes, evidence and diagnostic metadata stay in authorized detail views.</summary>
public sealed record DashboardRequestDto(Guid Id, string LeaveTypeName, DateOnly StartDate, DateOnly EndDate,
    string Status, DateTime? LastActivityUtc);
/// <summary>Explicit current-model units/date basis prevent a lifetime calendar balance being mistaken for statutory accrual or unique absence.</summary>
public sealed record DashboardBasisDto(string TimeZone, string Unit, string BalanceBasis, DateOnly Today,
    DateOnly UpcomingStart, DateOnly UpcomingEnd, bool Inclusive, int PendingPreviewLimit, int UpcomingLimit, int RecentLimit);
/// <summary>One consistent owner snapshot; counts cover all Pending records even when lists are bounded.</summary>
public sealed record PersonalDashboardDto(DateTimeOffset AsOfUtc, DashboardBasisDto Basis,
    IReadOnlyList<LeaveBalanceDto> Balances, int PendingCount, IReadOnlyList<DashboardRequestDto> Pending,
    IReadOnlyList<DashboardRequestDto> Upcoming, IReadOnlyList<DashboardRequestDto> Recent);

/// <summary>Composes current approved-history balances and bounded personal lists in one deferred read snapshot, without reserving a writer.</summary>
public sealed class GetPersonalDashboardQueryHandler(IApplicationDbContext context, CurrentAccountAuthorization authorization,
    ILeaveReportingReadTransaction reads, TimeProvider clock) : IRequestHandler<GetPersonalDashboardQuery, PersonalDashboardDto>
{
    public const int PendingLimit = 5;
    public const int UpcomingLimit = 10;
    public const int RecentLimit = 10;
    public const int UpcomingDays = 90;
    private static readonly TimeSpan SouthAfricaOffset = TimeSpan.FromHours(2);

    /// <summary>Current South African business dates use SAST (UTC+02:00), independent of server/browser timezone; audit times remain UTC.</summary>
    public static DateOnly BusinessDate(DateTimeOffset utc) => DateOnly.FromDateTime(utc.ToOffset(SouthAfricaOffset).DateTime);

    /// <inheritdoc />
    public Task<PersonalDashboardDto> Handle(GetPersonalDashboardQuery query, CancellationToken cancellationToken) =>
        reads.ExecuteAsync(async token =>
        {
            var employee = await authorization.RequireIdentityAsync(query.ActorIdentityId,
                [EmployeeRoles.Employee, EmployeeRoles.Manager], token);
            var asOf = clock.GetUtcNow();
            var today = BusinessDate(asOf);
            var lastDay = today.AddDays(UpcomingDays);
            var start = today.ToDateTime(TimeOnly.MinValue);
            var end = lastDay.ToDateTime(TimeOnly.MinValue);
            var types = await context.LeaveTypes.AsNoTracking().Include(t => t.LeavePolicy)
                .OrderBy(t => t.Name).ThenBy(t => t.Id).ToListAsync(token);
            var approved = await context.LeaveRequests.AsNoTracking()
                .Where(r => r.EmployeeId == employee.Id && r.Status == LeaveRequestStatus.Approved).ToListAsync(token);
            var balances = types.Select(type =>
            {
                var balance = LeaveBalanceCalculator.Calculate(type.LeavePolicy.DefaultBalance,
                    approved.Where(r => r.LeaveTypeId == type.Id));
                return new LeaveBalanceDto { LeaveTypeId = type.Id, LeaveTypeName = type.Name,
                    EntitledDays = balance.EntitledDays, UsedDays = balance.UsedDays, RemainingDays = balance.RemainingDays };
            }).ToList();
            var owned = context.LeaveRequests.AsNoTracking().Where(r => r.EmployeeId == employee.Id);
            var pending = owned.Where(r => r.Status == LeaveRequestStatus.Pending);
            var count = await pending.CountAsync(token);
            var pendingRows = await Project(pending.OrderBy(r => r.StartDate).ThenBy(r => r.Id).Take(PendingLimit)).ToListAsync(token);
            var upcomingRows = await Project(owned.Where(r => r.Status == LeaveRequestStatus.Approved
                && r.EndDate >= start && r.StartDate <= end).OrderBy(r => r.StartDate).ThenBy(r => r.Id).Take(UpcomingLimit)).ToListAsync(token);
            var recentRows = await Project(owned.OrderByDescending(r => r.AuditEntries.Select(a => (DateTime?)a.Timestamp).Max() ?? r.ProcessedOn)
                .ThenByDescending(r => r.Id).Take(RecentLimit)).ToListAsync(token);
            return new PersonalDashboardDto(asOf, new("Africa/Johannesburg (SAST, UTC+02:00)", "inclusive calendar days",
                "current configured entitlement minus all approved calendar days; Pending reserves nothing", today,
                today, lastDay, true, PendingLimit, UpcomingLimit, RecentLimit), balances, count,
                Display(pendingRows), Display(upcomingRows), Display(recentRows));
        }, cancellationToken);

    private static IQueryable<RequestRow> Project(IQueryable<HRFlow.Domain.Entities.LeaveRequest> requests) =>
        requests.Select(r => new RequestRow(r.Id, r.LeaveType.Name, r.StartDate, r.EndDate, r.Status,
            r.AuditEntries.Select(a => (DateTime?)a.Timestamp).Max() ?? r.ProcessedOn));
    private static DashboardRequestDto[] Display(IEnumerable<RequestRow> rows) => rows.Select(r => new DashboardRequestDto(
        r.Id,r.Type,DateOnly.FromDateTime(r.Start),DateOnly.FromDateTime(r.End),r.Status.ToString(),
        r.Activity.HasValue ? DateTime.SpecifyKind(r.Activity.Value,DateTimeKind.Utc) : null)).ToArray();
    private sealed record RequestRow(Guid Id, string Type, DateTime Start, DateTime End, LeaveRequestStatus Status, DateTime? Activity);
}
