namespace HRFlow.Domain.Entities;
/// <summary>Actor-owned, short-lived review; committed outcomes survive expiry and recovery without reauthorizing imports.</summary>
public sealed class OnboardingBatch
{
 public Guid Id { get; set; } = Guid.NewGuid();
 public Guid ActorId { get; set; }
 public Guid Version { get; set; } = Guid.NewGuid();
 public string SourceHash { get; set; } = "";
 public string MappingJson { get; set; } = "";
 public string RowsJson { get; set; } = "[]";
 public DateTime ExpiresAtUtc { get; set; }
 public bool Cancelled { get; set; }
 public string SelectedRowsJson { get; set; } = "[]";
 public string? ConfirmationKey { get; set; }
 public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
/// <summary>Versioned fixed-pattern lookup; employee revisions copy values rather than referencing mutable templates.</summary>
public sealed class OnboardingScheduleTemplate
{
 public Guid Id { get; set; } = Guid.NewGuid();
 public string Code { get; set; } = "";
 public string Name { get; set; } = "";
 public string MinutesJson { get; set; } = "[]";
 public Guid Version { get; set; } = Guid.NewGuid();
}
