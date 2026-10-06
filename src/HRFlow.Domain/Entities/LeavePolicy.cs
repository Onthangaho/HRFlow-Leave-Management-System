using HRFlow.Domain.Common;

namespace HRFlow.Domain.Entities;

/// <summary>
/// Defines leave policy rules including overlap restrictions and default balance for leave types.
/// </summary>
public class LeavePolicy : BaseEntity
{
    /// <summary>Maximum trimmed policy label length.</summary>
    public const int MaxNameLength = 100;
    private LeavePolicy() { }

    /// <summary>
    /// Creates current shared rules; zero entitlement blocks positive-day requests rather than making leave unlimited.
    /// </summary>
    public static LeavePolicy Create(string name, bool allowOverlap, int defaultBalance)
    {
        var policy = new LeavePolicy { Id = Guid.NewGuid() };
        policy.Update(name, allowOverlap, defaultBalance);
        return policy;
    }

    public string Name { get; private set; } = string.Empty;
    public Guid Version { get; private set; } = Guid.NewGuid();
    public bool AllowOverlap { get; private set; }
    public int DefaultBalance { get; private set; }

    public ICollection<LeaveType> LeaveTypes { get; set; } = new List<LeaveType>();

    /// <summary>Updates shared current rules; zero entitlement permits no positive-day requests.</summary>
    public void Update(string name, bool allowOverlap, int defaultBalance)
    {
        var trimmed = name?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.Length > MaxNameLength || trimmed.Any(char.IsControl))
            throw new DomainException($"Policy name must contain 1 to {MaxNameLength} characters after trimming, without control characters.");
        if (defaultBalance < 0)
            throw new DomainException("Entitlement must be zero or greater. Zero allows no positive-day leave requests.");
        Name = trimmed;
        AllowOverlap = allowOverlap;
        DefaultBalance = defaultBalance;
        Version = Guid.NewGuid();
    }
}
