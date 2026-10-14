using HRFlow.Domain.Entities;
namespace HRFlow.Application.Services;
/// <summary>Unknown is incomplete confirmed input, never a legal violation. Future-only schedule is a subset of missing current schedule.</summary>
public sealed record EmploymentAttention(bool NeedsEmploymentFacts, bool NeedsCurrentSchedule, bool HasFutureScheduleOnly)
{
    /// <summary>Only active confirmed starters on/before today are applicable to current-schedule attention.</summary>
    public static EmploymentAttention For(Employee employee, DateOnly today, IEnumerable<DateOnly> dates)
    {
        var effective = dates.ToArray();
        var missing = employee.IsActive && employee.EmploymentStartDate.HasValue && employee.EmploymentStartDate <= today
            && !effective.Any(d => d <= today);
        return new(employee.IsActive && (employee.EmployeeNumber == null || !employee.EmploymentStartDate.HasValue),
            missing, missing && effective.Any(d => d > today));
    }
}
