using HRFlow.Domain.Common;
namespace HRFlow.Domain.Entities;

/// <summary>Immutable confirmed weekly pattern; does not determine leave charges or statutory entitlement.</summary>
public sealed class WeeklyScheduleRevision
{
    public const int WeekdayCount = 7;
    public const int MaxDailyMinutes = 720;
    public const int MaxNameLength = 100;
    public Guid Id { get; private set; }
    public Guid EmployeeId { get; private set; }
    public string Name { get; private set; } = "";
    public DateOnly EffectiveFrom { get; private set; }
    /// <summary>Monday through Sunday in minutes; zero means not scheduled.</summary>
    public int MondayMinutes { get; private set; }
    public int TuesdayMinutes { get; private set; }
    public int WednesdayMinutes { get; private set; }
    public int ThursdayMinutes { get; private set; }
    public int FridayMinutes { get; private set; }
    public int SaturdayMinutes { get; private set; }
    public int SundayMinutes { get; private set; }
    public DateTime RecordedAtUtc { get; private set; }
    public Guid RecordedById { get; private set; }
    private WeeklyScheduleRevision() { }
    /// <summary>Validates bounded fixed daytime patterns before creating an append-only revision.</summary>
    public static WeeklyScheduleRevision Create(Guid employeeId, Guid actorId, string name, DateOnly date, int[] minutes)
    {
        EmploymentFacts.ValidateDate(date);
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength || name.Any(char.IsControl))
            throw new DomainException("Schedule name must contain 1-100 characters without control characters.");
        if (minutes.Length != WeekdayCount || minutes.Any(m => m < 0 || m > MaxDailyMinutes) || minutes.All(m => m == 0))
            throw new DomainException("Choose at least one weekday, with 1-720 working minutes (up to 12 hours) per selected day.");
        return new()
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            RecordedById = actorId,
            Name = name.Trim(),
            EffectiveFrom = date,
            MondayMinutes = minutes[0],
            TuesdayMinutes = minutes[1],
            WednesdayMinutes = minutes[2],
            ThursdayMinutes = minutes[3],
            FridayMinutes = minutes[4],
            SaturdayMinutes = minutes[5],
            SundayMinutes = minutes[6],
            RecordedAtUtc = DateTime.UtcNow
        };
    }
}

/// <summary>ASCII normalization avoids culture-dependent employee-number collisions; dates are confirmed facts only.</summary>
public static class EmploymentFacts
{
    public const int MaxNumberLength = 32;
    /// <summary>Trims and uppercases a restricted identifier without deleting internal characters.</summary>
    public static string NormalizeNumber(string? value)
    {
        var number = value?.Trim().ToUpperInvariant() ?? "";
        if (number.Length is < 1 or > MaxNumberLength || !number.All(c => c is >= 'A' and <= 'Z' or >= '0' and <= '9' or '-' or '_')
            || !char.IsAsciiLetterOrDigit(number[0]))
            throw new DomainException("Employee number must contain 1-32 ASCII letters, digits, hyphens or underscores and start with a letter or digit.");
        return number;
    }
    /// <summary>Bounds operator-entered dates while supporting leap dates and future starters without changing access rules.</summary>
    public static void ValidateDate(DateOnly date)
    {
        if (date < new DateOnly(1900, 1, 1) || date > new DateOnly(2100, 12, 31))
            throw new DomainException("Confirmed dates must be between 1900-01-01 and 2100-12-31.");
    }
}
