using System.Text.Json.Serialization;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Models.Employees;
using Microsoft.EntityFrameworkCore;
namespace HRFlow.Application.Services;

/// <summary>Explicit fixed-pattern input; integer minutes avoid fractional-hour rounding and implicit weekdays.</summary>
public sealed record ScheduleInput([property: JsonRequired] Guid ExpectedVersion, string Name, DateOnly EffectiveFrom, int[] Minutes);
/// <summary>Read-only revision identity and chronology; no retrospective leave recalculation.</summary>
public sealed record ScheduleDto(Guid Id, string Name, DateOnly EffectiveFrom, int[] Minutes, DateTime RecordedAtUtc);
/// <summary>Independent stream version prevents two open forms from silently appending conflicting intentions.</summary>
public sealed record ScheduleHistoryDto(Guid Version, IReadOnlyList<ScheduleDto> Revisions);

/// <summary>Coordinates HR schedule append and historical reads using existing database protection.</summary>
public sealed class EmploymentScheduleService(IApplicationDbContext context, CurrentAccountAuthorization auth,
    IEmployeeManagementTransaction writes, ILeaveReportingReadTransaction reads)
{
    /// <summary>Reads authorisation and all revisions from one deferred snapshot.</summary>
    public Task<ScheduleHistoryDto> GetAsync(Guid actor, Guid employeeId, CancellationToken token) => reads.ExecuteAsync(async ct =>
    {
        await auth.RequireIdentityAsync(actor, [EmployeeRoles.HrAdministrator], ct);
        var employee = await context.Employees.AsNoTracking().SingleOrDefaultAsync(e => e.Id == employeeId, ct)
            ?? throw new NotFoundException("Employee was not found.");
        return await HistoryAsync(context, employee, ct);
    }, token);
    /// <summary>Append only after current HR, target lifecycle and original stream version checks inside the writer reservation.</summary>
    public async Task<ScheduleHistoryDto> AppendAsync(Guid actor, Guid employeeId, ScheduleInput input, CancellationToken token)
    {
        ScheduleHistoryDto? result = null;
        await writes.ExecuteAsync(async ct =>
        {
            var hr = await auth.RequireIdentityAsync(actor, [EmployeeRoles.HrAdministrator], ct);
            var employee = await context.Employees.SingleOrDefaultAsync(e => e.Id == employeeId, ct) ?? throw new NotFoundException("Employee was not found.");
            if (!employee.IsActive) throw new WriteConflictException("Inactive employment cannot receive a new schedule.");
            if (employee.ScheduleVersion != input.ExpectedVersion) throw new WriteConflictException("Schedule history changed. Reload explicitly and review before saving.");
            if (input.Minutes is null) throw new HRFlow.Domain.Common.DomainException("Supply seven weekday working-minute values.");
            var revision = WeeklyScheduleRevision.Create(employee.Id, hr.Id, input.Name, input.EffectiveFrom, input.Minutes);
            if (await context.WeeklyScheduleRevisions.AnyAsync(s => s.EmployeeId == employee.Id && s.EffectiveFrom == input.EffectiveFrom, ct))
                throw new WriteConflictException("A schedule already starts on this date. Existing revisions are preserved; choose another effective date.");
            context.WeeklyScheduleRevisions.Add(revision);
            employee.ReviseSchedule();
            await context.SaveChangesAsync(ct);
            result = await HistoryAsync(context, employee, ct);
        }, token);
        return result!;
    }
    /// <summary>Shared HR/own-profile projection preserves the same revision identities and explicit UTC semantics.</summary>
    public static async Task<ScheduleHistoryDto> HistoryAsync(IApplicationDbContext context, Employee employee, CancellationToken token)
    {
        var rows = await context.WeeklyScheduleRevisions.AsNoTracking().Where(s => s.EmployeeId == employee.Id).OrderBy(s => s.EffectiveFrom).ThenBy(s => s.Id).ToListAsync(token);
        return new(employee.ScheduleVersion, rows.Select(s => new ScheduleDto(s.Id, s.Name, s.EffectiveFrom,
            [s.MondayMinutes, s.TuesdayMinutes, s.WednesdayMinutes, s.ThursdayMinutes, s.FridayMinutes, s.SaturdayMinutes, s.SundayMinutes], DateTime.SpecifyKind(s.RecordedAtUtc, DateTimeKind.Utc))).ToList());
    }
}
