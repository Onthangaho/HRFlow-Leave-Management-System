using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Interfaces.Services;
using HRFlow.Domain.Models.Employees;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetLeaveRequestTimeline;

/// <summary>The authenticated Identity actor is supplied by the controller, never by client JSON.</summary>
public sealed record GetLeaveRequestTimelineQuery(Guid ActorIdentityId, Guid RequestId) : IRequest<LeaveRequestTimelineDto>;

/// <summary>Contains only transition facts, current actor display names and role-filtered context.</summary>
public sealed record LeaveTimelineEventDto(Guid Id, Guid ActorId, string ActorName, string Action,
    DateTime TimestampUtc, string? OldStatus, string NewStatus, string? Explanation, string? CorrelationId);

/// <summary>Reports missing legacy submission honestly without synthesizing events or timestamps.</summary>
public sealed record LeaveRequestTimelineDto(Guid RequestId, string EmployeeName, bool EmployeeIsActive,
    string LeaveTypeName, DateOnly StartDate, DateOnly EndDate, string Status, bool SubmissionRecorded,
    IReadOnlyList<LeaveTimelineEventDto> Events);

/// <summary>Applies live owner, reporting and HR scopes in the same deferred snapshot as the timeline.</summary>
public sealed class GetLeaveRequestTimelineQueryHandler(IApplicationDbContext context,
    IEmployeeRoleLookupService roles, ILeaveReportingReadTransaction transaction)
    : IRequestHandler<GetLeaveRequestTimelineQuery, LeaveRequestTimelineDto>
{
    /// <summary>Out-of-scope requests share the missing-resource response to prevent enumeration.</summary>
    public Task<LeaveRequestTimelineDto> Handle(GetLeaveRequestTimelineQuery request, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(async token =>
        {
            var actor = await context.Employees.AsNoTracking().SingleOrDefaultAsync(
                employee => employee.IdentityUserId == request.ActorIdentityId.ToString() && employee.IsActive, token)
                ?? throw new ForbiddenException("An active employee account is required to view request history.");
            var capabilities = await roles.GetRolesByIdentityUserIdAsync(actor.IdentityUserId, token);
            var hr = capabilities.Contains(EmployeeRoles.HrAdministrator);
            var manager = capabilities.Contains(EmployeeRoles.Manager);
            var personal = manager || capabilities.Contains(EmployeeRoles.Employee);
            if (!hr && !personal) throw new ForbiddenException("Your account no longer has permission to view request history.");
            var leave = await context.LeaveRequests.AsNoTracking()
                .Where(leave => leave.Id == request.RequestId && (hr
                    || (personal && leave.EmployeeId == actor.Id)
                    || (manager && leave.EmployeeId != actor.Id && leave.Employee.ManagerId == actor.Id
                        && leave.Employee.DepartmentId == actor.DepartmentId)))
                .Select(leave => new { leave.Id, leave.Employee.FullName, leave.Employee.IsActive,
                    TypeName = leave.LeaveType.Name, leave.StartDate, leave.EndDate, leave.Status })
                .SingleOrDefaultAsync(token) ?? throw new NotFoundException("Leave request was not found or is not available to your account.");
            var audits = await context.AuditEntries.AsNoTracking().Where(entry => entry.LeaveRequestId == leave.Id)
                .OrderBy(entry => entry.Timestamp).ThenBy(entry => entry.Id)
                .Select(entry => new { entry.Id, entry.ActorId, ActorName = entry.Actor.FullName, entry.Action,
                    entry.Timestamp, entry.OldStatus, entry.NewStatus, entry.Reason, entry.CorrelationId }).ToListAsync(token);
            var events = audits.Select(entry => new LeaveTimelineEventDto(entry.Id, entry.ActorId, entry.ActorName,
                entry.Action, DateTime.SpecifyKind(entry.Timestamp, DateTimeKind.Utc), entry.OldStatus?.ToString(),
                entry.NewStatus.ToString(), entry.Reason is null ? null : hr ? entry.Reason
                    : "Cancelled because the employee account was deactivated.", hr ? entry.CorrelationId : null)).ToList();
            return new LeaveRequestTimelineDto(leave.Id, leave.FullName, leave.IsActive, leave.TypeName,
                DateOnly.FromDateTime(leave.StartDate), DateOnly.FromDateTime(leave.EndDate), leave.Status.ToString(),
                audits.Any(entry => entry.Action == "Submit" && entry.OldStatus is null && entry.NewStatus == LeaveRequestStatus.Pending), events);
        }, cancellationToken);
}
