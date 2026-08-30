using HRFlow.Domain.Common;
using HRFlow.Domain.Enums;

namespace HRFlow.Domain.Entities;

public class AuditEntry : BaseEntity
{
    public Guid LeaveRequestId { get; private set; }
    public Guid ActorId { get; private set; }
    public string Action { get; private set; } = string.Empty;
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

    public static AuditEntry Create(Guid leaveRequestId, Guid actorId, string action, LeaveRequestStatus? oldStatus, LeaveRequestStatus newStatus)
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

        return new AuditEntry(leaveRequestId, actorId, action, oldStatus, newStatus);
    }
}