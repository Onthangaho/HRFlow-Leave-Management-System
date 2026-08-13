using HRFlow.Domain.Common;

namespace HRFlow.Domain.Entities;

public class LeavePolicy : BaseEntity
{
    private LeavePolicy() { }

    private LeavePolicy(bool allowOverlap, int defaultBalance)
    {
        AllowOverlap = allowOverlap;
        DefaultBalance = defaultBalance;
    }

    public static LeavePolicy Create(bool allowOverlap, int defaultBalance)
    {
        return new LeavePolicy(allowOverlap, defaultBalance);
    }

    public bool AllowOverlap { get; private set; }
    public int DefaultBalance { get; private set; }

    // Navigation properties
    public ICollection<LeaveType> LeaveTypes { get; set; } = new List<LeaveType>();
}