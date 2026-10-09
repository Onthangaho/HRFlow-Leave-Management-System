namespace HRFlow.Domain.Entities;

/// <summary>Preserves evidence provenance independently of immutable leave transitions, including unavailable blobs.</summary>
public sealed class SupportingDocument
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public Guid UploadKey { get; set; }
    public Guid? RequestId { get; set; }
    public string Class { get; set; } = SupportingDocumentClass.Medical;
    public string Status { get; set; } = SupportingDocumentStatus.Receiving;
    public string MediaType { get; set; } = "";
    public long Size { get; set; }
    public string Checksum { get; set; } = "";
    public Guid Version { get; set; } = Guid.NewGuid();
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime? ScannedUtc { get; set; }
    public DateTime? BoundUtc { get; set; }
    public DateTime? RemovedUtc { get; set; }
}

/// <summary>Stable persisted lifecycle values shared by storage, scanning and protected binding.</summary>
public static class SupportingDocumentStatus
{
    public const string Receiving = "Receiving";
    public const string Quarantined = "Quarantined";
    public const string Scanning = "Scanning";
    public const string Clean = "Clean";
    public const string Rejected = "Rejected";
    public const string ScanUnavailable = "ScanUnavailable";
    public const string Removed = "Removed";
    public const string Missing = "Missing";
}

/// <summary>Explicit content classification; leave-type names never determine medical privacy.</summary>
public static class SupportingDocumentClass
{
    public const string Medical = "Medical";
    public const string Ordinary = "Ordinary";
}

/// <summary>Admission bounds prevent request binding and upload handling from applying divergent limits.</summary>
public static class SupportingDocumentLimits
{
    public const int MaxFileBytes = 10 * 1024 * 1024;
    public const int MaxRequestDocuments = 5;
    public const int MaxOwnerDrafts = 20;
    public const int MaxPdfPages = 500;
    public const int MaxImagePixels = 20_000_000;
}

/// <summary>Documents content-access/removal outcomes without inventing leave-state transitions or storing filenames.</summary>
public sealed class DocumentAccessEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid DocumentId { get; set; }
    public Guid? ActorId { get; set; }
    public string Action { get; set; } = "";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
