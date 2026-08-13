using HRFlow.Domain.Common;

namespace HRFlow.Domain.Entities;

public class LeaveType : BaseEntity
{
    private LeaveType() { }

    private LeaveType(string name, LeavePolicy leavePolicy)
    {
        Name = name;
        LeavePolicy = leavePolicy;
        LeavePolicyId = leavePolicy.Id;
    }

    public static LeaveType Create(string name, LeavePolicy leavePolicy)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("LeaveType name cannot be null or empty.");
        }

        if (leavePolicy is null)
        {
            throw new DomainException("LeavePolicy cannot be null.");
        }

        var leaveType = new LeaveType(name, leavePolicy);
        leaveType.LeavePolicyId = leavePolicy.Id;
        return leaveType;
    }

    public string Name { get; private set; } = string.Empty;
    public Guid LeavePolicyId { get; private set; }
    public LeavePolicy LeavePolicy { get; private set; } = null!;
}