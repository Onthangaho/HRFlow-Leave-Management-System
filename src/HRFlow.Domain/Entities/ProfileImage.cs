namespace HRFlow.Domain.Entities;

/// <summary>Separate optional image lifecycle; originals are never retained or served.</summary>
public sealed class ProfileImage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public Guid Version { get; set; } = Guid.NewGuid();
    public string Status { get; set; } = ProfileImageStatus.Processing;
    public long Size { get; set; }
    public string Checksum { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>Images have no request binding, medical classification or leave-transition status.</summary>
public static class ProfileImageStatus
{
    public const string Processing = "Processing";
    public const string Current = "Current";
    public const string Removed = "Removed";
}
