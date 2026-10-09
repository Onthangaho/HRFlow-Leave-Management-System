using HRFlow.Domain.Common;

namespace HRFlow.Domain.Entities;

/// <summary>Names a leave category with type-specific company requirements and shared entitlement/overlap rules.</summary>
public class LeaveType : BaseEntity
{
    /// <summary>Maximum trimmed name length accepted by management and persistence.</summary>
    public const int MaxNameLength = 100;
    private LeaveType() { }

    private LeaveType(string name, LeavePolicy leavePolicy)
    {
        Name = name.Trim();
        NormalizedName = Name.ToUpperInvariant();
        LeavePolicy = leavePolicy;
        LeavePolicyId = leavePolicy.Id;
    }

    /// <summary>Creates a category with an explicit policy; policy sharing is intentional.</summary>
    public static LeaveType Create(string name, LeavePolicy leavePolicy)
    {
        ValidateName(name);
        if (leavePolicy is null)
        {
            throw new DomainException("LeavePolicy cannot be null.");
        }

        var leaveType = new LeaveType(name, leavePolicy);
        leaveType.Id = Guid.NewGuid();
        leaveType.LeavePolicyId = leavePolicy.Id;
        return leaveType;
    }

    public string DescriptionMode { get; private set; } = RequirementModes.NotRequested;
    public string EvidenceMode { get; private set; } = RequirementModes.Optional;
    public string EvidenceClass { get; private set; } = SupportingDocumentClass.Ordinary;
    public string? RequirementInstructions { get; private set; }

    /// <summary>Changes only this type's company submission requirements, rotating its edit version.</summary>
    public void ConfigureRequirements(string descriptionMode, string evidenceMode, string evidenceClass, string? instructions)
    {
        RequirementModes.Validate(descriptionMode, evidenceMode, evidenceClass, instructions);
        DescriptionMode = descriptionMode; EvidenceMode = evidenceMode; EvidenceClass = evidenceClass;
        RequirementInstructions = string.IsNullOrWhiteSpace(instructions) ? null : instructions.Trim();
        Version = Guid.NewGuid();
    }

    public string Name { get; private set; } = string.Empty;
    public string NormalizedName { get; private set; } = string.Empty;
    public Guid Version { get; private set; } = Guid.NewGuid();
    public Guid LeavePolicyId { get; private set; }
    public LeavePolicy LeavePolicy { get; private set; } = null!;

    /// <summary>Changes current classification without rewriting requests or historical audits.</summary>
    public void Update(string name, LeavePolicy policy)
    {
        ValidateName(name);
        ArgumentNullException.ThrowIfNull(policy);
        Name = name.Trim();
        NormalizedName = Name.ToUpperInvariant();
        LeavePolicyId = policy.Id;
        LeavePolicy = policy;
        Version = Guid.NewGuid();
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength || name.Trim().Any(char.IsControl))
            throw new DomainException($"Leave type name must contain 1 to {MaxNameLength} characters after trimming, without control characters.");
    }
}
