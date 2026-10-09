using HRFlow.Domain.Entities;
namespace HRFlow.Application.Features.LeaveConfiguration;

/// <summary>Explicit rules for creation; zero entitlement blocks positive-day submissions and approvals.</summary>
public sealed record PolicyInput
{
    public required string Name { get; init; }
    public required bool AllowOverlap { get; init; }
    public required int DefaultBalance { get; init; }
}

/// <summary>Replaces current policy rules only when the caller's edit snapshot is still current.</summary>
public sealed record PolicyUpdateInput
{
    public required string Name { get; init; }
    public required bool AllowOverlap { get; init; }
    public required int DefaultBalance { get; init; }
    public required Guid ExpectedVersion { get; init; }
}

/// <summary>Creates a category with an explicit existing policy, allowing deliberate policy sharing.</summary>
public sealed record LeaveTypeInput
{
    public required string Name { get; init; }
    public required Guid LeavePolicyId { get; init; }
    public string DescriptionMode { get; init; } = RequirementModes.NotRequested;
    public string EvidenceMode { get; init; } = RequirementModes.Optional;
    public string EvidenceClass { get; init; } = SupportingDocumentClass.Medical;
    public string? RequirementInstructions { get; init; }
}

/// <summary>Replaces name and policy assignment using the original type version, not a policy version.</summary>
public sealed record LeaveTypeUpdateInput
{
    public required string Name { get; init; }
    public required Guid LeavePolicyId { get; init; }
    public required string DescriptionMode { get; init; }
    public required string EvidenceMode { get; init; }
    public required string EvidenceClass { get; init; }
    public string? RequirementInstructions { get; init; }
    public required Guid ExpectedVersion { get; init; }
}

/// <summary>Identifies linked categories without exposing employee or leave-request payloads.</summary>
public sealed record LinkedLeaveTypeDto(Guid Id, string Name, int RequestCount);

/// <summary>Explains shared-policy consequences; reference counts are advisory snapshots, rechecked on deletion.</summary>
public sealed record PolicyManagementDto(
    Guid Id, string Name, Guid Version, bool AllowOverlap, int DefaultBalance,
    IReadOnlyList<LinkedLeaveTypeDto> LinkedLeaveTypes, bool CanDelete);

/// <summary>Supplies a complete type edit snapshot and the current shared-policy version and reference count.</summary>
public sealed record LeaveTypeManagementDto(
    Guid Id, string Name, Guid Version, Guid LeavePolicyId, string PolicyName,
    Guid PolicyVersion, bool AllowOverlap, int DefaultBalance, int RequestCount, bool CanDelete, string DescriptionMode, string EvidenceMode, string EvidenceClass, string? RequirementInstructions);
