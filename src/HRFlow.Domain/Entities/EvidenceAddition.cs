namespace HRFlow.Domain.Entities;

/// <summary>Append-only supplemental binding outcome; separate from immutable leave-state transitions.</summary>
public sealed class EvidenceAddition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActorId { get; set; }
    public Guid RequestId { get; set; }
    public Guid Key { get; set; }
    public string DocumentIds { get; set; } = "";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
}
