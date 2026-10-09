using HRFlow.Domain.Common;

namespace HRFlow.Domain.Entities;

/// <summary>Company submission settings only; these are not statutory eligibility or payment rules.</summary>
public static class RequirementModes
{
    public const string NotRequested = "NotRequested";
    public const string Optional = "Optional";
    public const string Required = "Required";
    public const int MaxTextLength = 1000;

    /// <summary>Rejects unsupported values and medical upload gates until absence/payment workflows are reviewed.</summary>
    public static void Validate(string descriptionMode, string evidenceMode, string evidenceClass, string? instructions)
    {
        string[] modes = [NotRequested, Optional, Required];
        if (!modes.Contains(descriptionMode) || !modes.Contains(evidenceMode))
            throw new DomainException("Choose NotRequested, Optional or Required for each submission requirement.");
        if (evidenceClass != SupportingDocumentClass.Medical && evidenceClass != SupportingDocumentClass.Ordinary)
            throw new DomainException("Choose Medical or Ordinary evidence.");
        if (evidenceClass == SupportingDocumentClass.Medical && evidenceMode == Required)
            throw new DomainException("Medical evidence cannot be required before reporting absence. Use Optional; later proof/payment requirements are not supported yet.");
        if (instructions?.Trim().Length > MaxTextLength)
            throw new DomainException("Instructions must contain at most 1000 trimmed characters. Do not request diagnoses.");
    }
}

/// <summary>Immutable submission facts; null on legacy requests rather than guessed historical requirements.</summary>
public sealed record RequestRequirementsSnapshot(Guid TypeId, Guid TypeVersion, Guid PolicyId, Guid PolicyVersion,
    string DescriptionMode, string EvidenceMode, string EvidenceClass, string? Instructions,
    string RuleId = "CompanySubmission", int RuleVersion = 1);
