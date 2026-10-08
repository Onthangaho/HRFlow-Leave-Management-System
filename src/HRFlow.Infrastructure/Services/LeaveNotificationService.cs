using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces.Services;
using HRFlow.Domain.Models.Employees;
using HRFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Services;

/// <summary>Captures event-time recipients and projects only presently authorised links from one snapshot.</summary>
public sealed class LeaveNotificationService(
    HRFlowDbContext context, CurrentAccountAuthorization authorization,
    IEmployeeRoleLookupService roles, IAccountAccessService access,
    ILeaveReportingReadTransaction reads, IEmployeeManagementTransaction writes)
    : ILeaveNotificationOutbox, ILeaveNotificationService
{
    private static readonly string[] Capabilities = [EmployeeRoles.Employee, EmployeeRoles.Manager, EmployeeRoles.HrAdministrator];
    private const int MaximumPage = 10000;
    private const int MaximumPageSize = 50;

    /// <inheritdoc />
    public async Task TransitionAsync(LeaveRequest request, CancellationToken token)
    {
        var audit = request.AuditEntries.Last();
        // Register the existing domain-created event as Added before using its EF-generated key.
        // Assigning a key on an untracked aggregate child can otherwise make EF infer an update.
        context.AuditEntries.Add(audit);
        var owner = await context.Employees.SingleAsync(e => e.Id == request.EmployeeId, token);
        Employee? recipient = audit.Action is "Approve" or "Reject" ? owner : await ManagerAsync(owner, token);
        if (recipient is null || recipient.Id == audit.ActorId || !await EligibleAsync(recipient, token)) return;
        if (audit.Action is "Approve" or "Reject"
            && !(await roles.GetRolesByIdentityUserIdAsync(recipient.IdentityUserId, token))
                .Any(r => r is EmployeeRoles.Employee or EmployeeRoles.Manager)) return;
        Add(audit.Id.ToString(), recipient.Id, request.Id, audit.Action, audit.Timestamp);
    }

    /// <inheritdoc />
    public async Task ReassignmentAsync(Employee employee, Guid? previousManagerId, Guid actorIdentityId, CancellationToken token)
    {
        if (employee.ManagerId == previousManagerId) return;
        var manager = await ManagerAsync(employee, token);
        if (manager is null || manager.IdentityUserId == actorIdentityId.ToString()) return;
        var pending = await context.LeaveRequests.Where(r => r.EmployeeId == employee.Id && r.Status == LeaveRequestStatus.Pending)
            .Select(r => r.Id).ToListAsync(token);
        foreach (var requestId in pending)
            Add($"reassign:{employee.Version}:{requestId}", manager.Id, requestId, "Reassigned", DateTime.UtcNow);
    }

    private void Add(string key, Guid recipient, Guid request, string kind, DateTime utc) =>
        context.LeaveNotificationEvents.Add(new LeaveNotificationEvent
        { EventKey = key, RecipientId = recipient, RequestId = request, Kind = kind, CreatedAtUtc = utc });

    private async Task<bool> EligibleAsync(Employee employee, CancellationToken token) =>
        employee.IsActive && await access.IsActiveAsync(employee.IdentityUserId, token);

    private async Task<Employee?> ManagerAsync(Employee owner, CancellationToken token)
    {
        var manager = await context.Employees.SingleOrDefaultAsync(e => e.Id == owner.ManagerId
            && e.Id != owner.Id && e.DepartmentId == owner.DepartmentId, token);
        return manager is not null && await EligibleAsync(manager, token)
            && (await roles.GetRolesByIdentityUserIdAsync(manager.IdentityUserId, token)).Contains(EmployeeRoles.Manager)
            ? manager : null;
    }

    /// <inheritdoc />
    public Task<NotificationPageDto> ListAsync(Guid actor, string filter, int page, int pageSize, CancellationToken token)
    {
        if (filter is not ("all" or "read" or "unread") || page < 1 || page > MaximumPage || pageSize < 1 || pageSize > MaximumPageSize)
            throw new HRFlow.Domain.Common.DomainException("Use all/read/unread, page 1–10000 and pageSize 1–50.");
        return reads.ExecuteAsync(async ct =>
        {
            var employee = await authorization.RequireIdentityAsync(actor, Capabilities, ct);
            var currentRoles = await roles.GetRolesByIdentityUserIdAsync(employee.IdentityUserId, ct);
            var query = context.LeaveNotifications.AsNoTracking().Where(n => n.RecipientId == employee.Id);
            if (filter == "read") query = query.Where(n => n.ReadAtUtc != null);
            if (filter == "unread") query = query.Where(n => n.ReadAtUtc == null);
            var total = await query.CountAsync(ct);
            var rows = await query.Include(n => n.Event).OrderByDescending(n => n.CreatedAtUtc).ThenByDescending(n => n.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            var items = new List<NotificationDto>();
            foreach (var row in rows)
            {
                var allowed = await context.LeaveRequests.AnyAsync(r => r.Id == row.Event.RequestId && (
                    currentRoles.Contains(EmployeeRoles.HrAdministrator)
                    || (currentRoles.Contains(EmployeeRoles.Employee) || currentRoles.Contains(EmployeeRoles.Manager)) && r.EmployeeId == employee.Id
                    || currentRoles.Contains(EmployeeRoles.Manager) && r.EmployeeId != employee.Id
                        && r.Employee.ManagerId == employee.Id && r.Employee.DepartmentId == employee.DepartmentId), ct);
                var message = allowed ? row.Event.Kind switch
                {
                    "Submit" => "A leave request was submitted.",
                    "Approve" => "Your leave request was approved.",
                    "Reject" => "Your leave request was rejected.",
                    "Cancel" => "A leave request was cancelled.",
                    _ => "Pending leave work was assigned to you."
                } : "This notification is no longer available under your current access.";
                items.Add(new(row.Id, message, allowed ? row.Event.RequestId : null,
                    DateTime.SpecifyKind(row.CreatedAtUtc, DateTimeKind.Utc), row.ReadAtUtc is DateTime read ? DateTime.SpecifyKind(read, DateTimeKind.Utc) : null));
            }
            return new NotificationPageDto(items, total, page, pageSize);
        }, token);
    }

    /// <inheritdoc />
    public Task<int> CountAsync(Guid actor, CancellationToken token) => reads.ExecuteAsync(async ct =>
    {
        var employee = await authorization.RequireIdentityAsync(actor, Capabilities, ct);
        return await context.LeaveNotifications.CountAsync(n => n.RecipientId == employee.Id && n.ReadAtUtc == null, ct);
    }, token);

    /// <inheritdoc />
    public Task ReadAsync(Guid actor, Guid notificationId, CancellationToken token) => writes.ExecuteAsync(async ct =>
    {
        var employee = await authorization.RequireIdentityAsync(actor, Capabilities, ct);
        var row = await context.LeaveNotifications.SingleOrDefaultAsync(n => n.Id == notificationId && n.RecipientId == employee.Id, ct)
            ?? throw new NotFoundException("Notification was not found.");
        row.ReadAtUtc ??= DateTime.UtcNow;
    }, token);
}
