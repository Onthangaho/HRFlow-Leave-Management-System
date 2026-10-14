using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Models.Employees;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetManagerAttention;

/// <summary>Scopes queue attention to the authenticated Manager, never a supplied employee.</summary>
public sealed record GetManagerAttentionQuery(Guid ActorIdentityId) : IRequest<ManagerAttentionDto>;
/// <summary>Minimal decision attention excludes emails, notes, evidence and personnel context.</summary>
public sealed record ManagerPendingDto(Guid RequestId, string EmployeeName, string LeaveTypeName, DateOnly StartDate, DateOnly EndDate);
/// <summary>All-date Pending count and bounded preview belong to the same authorized snapshot.</summary>
public sealed record ManagerAttentionDto(DateTimeOffset AsOfUtc, int PendingCount, int PreviewLimit, IReadOnlyList<ManagerPendingDto> Pending);
/// <summary>Composes queue attention without duplicating any decision or policy validation.</summary>
public sealed class GetManagerAttentionQueryHandler(IApplicationDbContext context, CurrentAccountAuthorization authorization,
    ILeaveReportingReadTransaction reads, TimeProvider clock) : IRequestHandler<GetManagerAttentionQuery, ManagerAttentionDto>
{
    public const int PreviewLimit = 5;
    /// <inheritdoc />
    public Task<ManagerAttentionDto> Handle(GetManagerAttentionQuery request, CancellationToken cancellationToken) =>
        reads.ExecuteAsync(async token =>
        {
            var manager = await authorization.RequireIdentityAsync(request.ActorIdentityId, [EmployeeRoles.Manager], token);
            var asOf = clock.GetUtcNow();
            var pending = context.LeaveRequests.AsNoTracking().Where(r => r.Status == LeaveRequestStatus.Pending
                && r.EmployeeId != manager.Id && r.Employee.ManagerId == manager.Id
                && r.Employee.DepartmentId == manager.DepartmentId);
            var count = await pending.CountAsync(token);
            var rows = await pending.OrderBy(r => r.StartDate).ThenBy(r => r.Id).Take(PreviewLimit)
                .Select(r => new { r.Id, r.Employee.FullName, Type = r.LeaveType.Name, r.StartDate, r.EndDate }).ToListAsync(token);
            return new ManagerAttentionDto(asOf, count, PreviewLimit, rows.Select(r => new ManagerPendingDto(r.Id,
                r.FullName, r.Type, DateOnly.FromDateTime(r.StartDate), DateOnly.FromDateTime(r.EndDate))).ToArray());
        }, cancellationToken);
}
