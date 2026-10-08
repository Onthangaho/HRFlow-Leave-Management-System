using HRFlow.Domain.Common;
using HRFlow.Domain.Enums;

namespace HRFlow.Domain.Entities;

/// <summary>Preserves one immutable leave transition with ordinary manager notes or separate HR cancellation context.</summary>
public class AuditEntry : BaseEntity
{
    /// <summary>Ordinary manager notes are bounded separately from sensitive cancellation reasons.</summary>
    public const int MaxDecisionNoteLength = 500;
    /// <summary>Immutable optional manager context; legacy and non-decision events retain null.</summary>
    public string? DecisionNote { get; private set; }
    public const int MaxCorrelationIdLength = 128;
    /// <summary>Optional diagnostic metadata; null accurately represents legacy events.</summary>
    public string? CorrelationId { get; private set; }
    public const string DeactivationReasonPrefix = "Employee deactivation: ";
    public static readonly int MaxReasonLength = Employee.MaxDeactivationReasonLength + DeactivationReasonPrefix.Length;
    public Guid LeaveRequestId { get; private set; }
    public Guid ActorId { get; private set; }
    public string Action { get; private set; } = string.Empty;
    /// <summary>Optional cancellation context; historical decisions retain a null reason.</summary>
    public string? Reason { get; private set; }
    public DateTime Timestamp { get; private set; }
    public LeaveRequestStatus? OldStatus { get; private set; }
    public LeaveRequestStatus NewStatus { get; private set; }

    // Navigation properties
    public LeaveRequest LeaveRequest { get; private set; } = null!;
    public Employee Actor { get; private set; } = null!;

    private AuditEntry(Guid leaveRequestId, Guid actorId, string action, LeaveRequestStatus? oldStatus, LeaveRequestStatus newStatus)
    {
        LeaveRequestId = leaveRequestId;
        ActorId = actorId;
        Action = action;
        Timestamp = DateTime.UtcNow;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }

    /// <summary>Captures actor and time at the transition without rewriting previous decisions.</summary>
    public static AuditEntry Create(Guid leaveRequestId, Guid actorId, string action, LeaveRequestStatus? oldStatus, LeaveRequestStatus newStatus, string? reason = null, string? correlationId = null, string? decisionNote = null)
    {
        if (leaveRequestId == Guid.Empty)
        {
            throw new DomainException("LeaveRequestId cannot be empty.");
        }

        if (actorId == Guid.Empty)
        {
            throw new DomainException("ActorId cannot be empty.");
        }

        if (string.IsNullOrWhiteSpace(action))
        {
            throw new DomainException("Action cannot be empty.");
        }

        if (reason is not null && (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > MaxReasonLength))
            throw new DomainException($"Cancellation reason must contain 1–{MaxReasonLength} characters.");
        if (correlationId is not null && (string.IsNullOrWhiteSpace(correlationId) || correlationId.Length > MaxCorrelationIdLength))
            throw new DomainException("Invalid audit correlation identifier.");
        var normalizedNote = string.IsNullOrWhiteSpace(decisionNote) ? null : decisionNote.Trim();
        if (normalizedNote?.Length > MaxDecisionNoteLength)
            throw new DomainException($"Decision note must contain no more than {MaxDecisionNoteLength} trimmed characters.");
        if (normalizedNote is not null && action is not ("Approve" or "Reject"))
            throw new DomainException("Decision notes belong only to approval or rejection events.");
        return new AuditEntry(leaveRequestId, actorId, action, oldStatus, newStatus) { Reason = reason?.Trim(), CorrelationId = correlationId, DecisionNote = normalizedNote };
    }
}
