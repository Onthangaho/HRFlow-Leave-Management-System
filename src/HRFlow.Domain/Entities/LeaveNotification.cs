namespace HRFlow.Domain.Entities;

/// <summary>Minimal delivery metadata, deliberately separate from immutable decision auditing.</summary>
public sealed class LeaveNotificationEvent
{
    /// <summary>Stable delivery identity generated before persistence.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>Audit identity or reporting-version/request key; never sensitive event content.</summary>
    public string EventKey { get; set; } = string.Empty;
    /// <summary>Employee recipient resolved at event time, not supplied by the client.</summary>
    public Guid RecipientId { get; set; }
    /// <summary>Internal reference projected only after current timeline-scope checks.</summary>
    public Guid RequestId { get; set; }
    /// <summary>Minimal category used to produce generic text instead of copying an audit payload.</summary>
    public string Kind { get; set; } = string.Empty;
    /// <summary>Original transition time in UTC, retained across retries.</summary>
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>Persisted retry delay prevents a failed event monopolising every worker batch.</summary>
    public DateTime? RetryAfterUtc { get; set; }
    /// <summary>Suppressed deliveries are acknowledged once and are never backfilled on re-enabling.</summary>
    public DateTime? SuppressedAtUtc { get; set; }
    /// <summary>Processing acknowledgment; SuppressedAtUtc distinguishes suppression from actual creation.</summary>
    public DateTime? DeliveredAtUtc { get; set; }
}

/// <summary>Recipient read state; sensitive request facts are never copied into the payload.</summary>
public sealed class LeaveNotification
{
    /// <summary>Opaque own-inbox resource identifier.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    /// <summary>References the durable source instead of duplicating its metadata.</summary>
    public Guid EventId { get; set; }
    /// <summary>Delivery recipient, with uniqueness enforced alongside the event.</summary>
    public Guid RecipientId { get; set; }
    /// <summary>Source event time, not worker processing time.</summary>
    public DateTime CreatedAtUtc { get; set; }
    /// <summary>First successful mark-read time; repeated writes preserve it.</summary>
    public DateTime? ReadAtUtc { get; set; }
    /// <summary>Source used internally for live-scope projection, never returned as an EF entity.</summary>
    public LeaveNotificationEvent Event { get; set; } = null!;
}
