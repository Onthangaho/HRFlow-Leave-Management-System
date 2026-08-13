using HRFlow.Domain.Common;

namespace HRFlow.Domain.Entities;

/// <summary>
/// Defines leave policy rules including overlap restrictions and default balance for leave types.
/// </summary>
public class LeavePolicy : BaseEntity
{
    private LeavePolicy() { }

    private LeavePolicy(bool allowOverlap, int defaultBalance)
    {
        AllowOverlap = allowOverlap;
        DefaultBalance = defaultBalance;
    }

    /// <summary>
    /// Factory method to create a new leave policy with specified rules.
    /// </summary>
    public static LeavePolicy Create(bool allowOverlap, int defaultBalance)
    {
        return new LeavePolicy(allowOverlap, defaultBalance);
    }

    public bool AllowOverlap { get; private set; }
    public int DefaultBalance { get; private set; }

    public ICollection<LeaveType> LeaveTypes { get; set; } = new List<LeaveType>();
}