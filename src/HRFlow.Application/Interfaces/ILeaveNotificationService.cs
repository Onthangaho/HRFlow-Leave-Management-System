using HRFlow.Domain.Entities;

namespace HRFlow.Application.Interfaces;

/// <summary>Captures recipients within the originating protected write, without delivering or creating audits.</summary>
public interface ILeaveNotificationOutbox
{
    /// <summary>Records the successful transition's minimal event before the transaction commits.</summary>
    Task TransitionAsync(LeaveRequest request, CancellationToken token);
    /// <summary>Alerts the new manager about existing pending work, keyed by the reporting edit version.</summary>
    Task ReassignmentAsync(Employee employee, Guid? previousManagerId, Guid actorIdentityId, CancellationToken token);
}

/// <summary>Own-inbox operations recheck credentials and live scope inside database protection.</summary>
public interface ILeaveNotificationService
{
    /// <summary>Projects a stable filtered page without leaking unavailable request identifiers.</summary>
    Task<NotificationPageDto> ListAsync(Guid actor, string filter, int page, int pageSize, CancellationToken token);
    /// <summary>Counts the recipient's unread rows, including generic unavailable items.</summary>
    Task<int> CountAsync(Guid actor, CancellationToken token);
    /// <summary>Sets read time once under the writer reservation; other recipients' IDs are indistinguishable from missing IDs.</summary>
    Task ReadAsync(Guid actor, Guid notificationId, CancellationToken token);
}

/// <summary>Only current authorised request links are emitted; unavailable items contain no request identifier.</summary>
public sealed record NotificationDto(Guid Id, string Message, Guid? RequestId, DateTime CreatedAtUtc, DateTime? ReadAtUtc);
/// <summary>Bounded stable pagination with a count for the selected read filter.</summary>
public sealed record NotificationPageDto(IReadOnlyList<NotificationDto> Items, int Total, int Page, int PageSize);
