namespace HRFlow.Domain.Entities;

/// <summary>Durable owner-scoped execution identity. Retained with its request so lost responses never permit duplicate execution.</summary>
public sealed class LeaveSubmissionOutcome
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid OwnerId { get; set; }
    public Guid Key { get; set; }
    public Guid RequestId { get; set; }
    public string PayloadHash { get; set; } = "";
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
}
