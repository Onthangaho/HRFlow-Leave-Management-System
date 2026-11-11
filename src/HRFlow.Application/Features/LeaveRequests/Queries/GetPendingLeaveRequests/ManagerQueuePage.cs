using FluentValidation;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Models.Employees;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetPendingLeaveRequests;

/// <summary>Bounded filters narrow an already authorized Manager scope; IDs never grant access.</summary>
public sealed class ManagerQueueFilter
{
    /// <summary>Retains the existing Pending-only query parameter.</summary>
    public string Status { get; set; } = "Pending";
    /// <summary>Exact current direct report, intersected with authoritative reporting scope.</summary>
    public Guid? EmployeeId { get; set; }
    /// <summary>Exact leave type; unknown IDs simply match no eligible requests.</summary>
    public Guid? LeaveTypeId { get; set; }
    /// <summary>Inclusive request-date intersection lower bound.</summary>
    public DateOnly? Start { get; set; }
    /// <summary>Inclusive request-date intersection upper bound.</summary>
    public DateOnly? End { get; set; }
    /// <summary>Exact lookup lets an open dialog recheck its original request independently of page filters.</summary>
    public Guid? RequestId { get; set; }
    /// <summary>Unavailable pages are clamped after the authorized filtered count.</summary>
    public int Page { get; set; } = 1;
    /// <summary>Default 25; maximum 100, without an unbounded fallback.</summary>
    public int PageSize { get; set; } = 25;
}

/// <summary>Count, selected rows and capture time belong to one deferred authorized snapshot.</summary>
public sealed record ManagerQueuePage(IReadOnlyList<PendingLeaveRequestDto> Items, int TotalCount, int Page,
    int PageSize, int TotalPages, DateTimeOffset CapturedAtUtc);
/// <summary>The actor comes exclusively from authentication.</summary>
public sealed record GetManagerQueueQuery(Guid ActorIdentityId, ManagerQueueFilter Filter) : IRequest<ManagerQueuePage>;

/// <summary>Rejects unsafe bounds before query execution; malformed Int32/date/GUID values fail model binding.</summary>
public sealed class GetManagerQueueQueryValidator : AbstractValidator<GetManagerQueueQuery>
{
    /// <summary>Preserves Pending-only semantics and prevents invalid offset arithmetic.</summary>
    public GetManagerQueueQueryValidator()
    {
        RuleFor(q => q.ActorIdentityId).NotEmpty();
        RuleFor(q => q.Filter.Status).Must(s => string.Equals(s, "Pending", StringComparison.OrdinalIgnoreCase));
        RuleFor(q => q.Filter.Page).GreaterThan(0);
        RuleFor(q => q.Filter.PageSize).InclusiveBetween(1, 100);
        RuleFor(q => q.Filter.EmployeeId).Must(id => id == null || id != Guid.Empty);
        RuleFor(q => q.Filter.LeaveTypeId).Must(id => id == null || id != Guid.Empty);
        RuleFor(q => q.Filter.RequestId).Must(id => id == null || id != Guid.Empty);
        RuleFor(q => q.Filter).Must(f => f.Start == null || f.End == null || f.End >= f.Start)
            .WithMessage("End date must be on or after start date.");
    }
}

internal static class ManagerQueueScope
{
    internal static IQueryable<LeaveRequest> Pending(IApplicationDbContext context, Employee actor) =>
        context.LeaveRequests.AsNoTracking().Where(r => r.Status == LeaveRequestStatus.Pending
            && r.EmployeeId != actor.Id && r.Employee.ManagerId == actor.Id
            && r.Employee.DepartmentId == actor.DepartmentId);
}

/// <summary>Filters/counts/pages before projection; no writer reservation or decision logic is used for reporting.</summary>
public sealed class GetManagerQueueQueryHandler(IApplicationDbContext context, ILeaveReportingReadTransaction reads,
    CurrentAccountAuthorization authorization, TimeProvider clock) : IRequestHandler<GetManagerQueueQuery, ManagerQueuePage>
{
    /// <inheritdoc />
    public Task<ManagerQueuePage> Handle(GetManagerQueueQuery request, CancellationToken cancellationToken) =>
        reads.ExecuteAsync(async token =>
        {
            var actor = await authorization.RequireIdentityAsync(request.ActorIdentityId, [EmployeeRoles.Manager], token);
            var captured = clock.GetUtcNow();
            var f = request.Filter;
            var query = ManagerQueueScope.Pending(context, actor);
            if (f.EmployeeId.HasValue) query = query.Where(r => r.EmployeeId == f.EmployeeId.Value);
            if (f.LeaveTypeId.HasValue) query = query.Where(r => r.LeaveTypeId == f.LeaveTypeId.Value);
            if (f.RequestId.HasValue) query = query.Where(r => r.Id == f.RequestId.Value);
            if (f.Start.HasValue) { var start = f.Start.Value.ToDateTime(TimeOnly.MinValue); query = query.Where(r => r.EndDate >= start); }
            if (f.End.HasValue) { var end = f.End.Value.ToDateTime(TimeOnly.MinValue); query = query.Where(r => r.StartDate <= end); }
            var total = await query.CountAsync(token);
            var pages = Math.Max(1, (int)(((long)total + f.PageSize - 1) / f.PageSize));
            var page = Math.Min(f.Page, pages);
            var items = await query.OrderBy(r => r.StartDate).ThenBy(r => r.Id)
                .Skip((page - 1) * f.PageSize).Take(f.PageSize)
                .Select(r => new PendingLeaveRequestDto { Id = r.Id, EmployeeId = r.EmployeeId,
                    EmployeeFullName = r.Employee.FullName, EmployeeEmail = r.Employee.Email,
                    LeaveTypeId = r.LeaveTypeId, LeaveTypeName = r.LeaveType.Name,
                    StartDate = r.StartDate, EndDate = r.EndDate, Status = r.Status.ToString() }).ToListAsync(token);
            return new ManagerQueuePage(items, total, page, f.PageSize, pages, captured);
        }, cancellationToken);
}

/// <summary>Minimal scoped selector values exclude email, evidence and personnel context.</summary>
public sealed record QueuePerson(Guid Id, string Name);
/// <summary>A bounded search over eligible Pending owners, independent of the displayed page.</summary>
public sealed record GetQueuePeopleQuery(Guid ActorIdentityId, string? Search) : IRequest<IReadOnlyList<QueuePerson>>;
/// <summary>Bounds selector text and avoids loading a complete employee collection.</summary>
public sealed class GetQueuePeopleQueryValidator : AbstractValidator<GetQueuePeopleQuery>
{
    /// <summary>Substring matching is literal and limited to 100 supplied characters.</summary>
    public GetQueuePeopleQueryValidator() { RuleFor(q => q.Search).MaximumLength(100); }
}
/// <summary>Searches all eligible Pending owners in a protected snapshot, returning at most 25 stable choices.</summary>
public sealed class GetQueuePeopleQueryHandler(IApplicationDbContext context, ILeaveReportingReadTransaction reads,
    CurrentAccountAuthorization authorization) : IRequestHandler<GetQueuePeopleQuery, IReadOnlyList<QueuePerson>>
{
    /// <inheritdoc />
    public Task<IReadOnlyList<QueuePerson>> Handle(GetQueuePeopleQuery request, CancellationToken cancellationToken) =>
        reads.ExecuteAsync<IReadOnlyList<QueuePerson>>(async token =>
        {
            var actor = await authorization.RequireIdentityAsync(request.ActorIdentityId, [EmployeeRoles.Manager], token);
            var search = request.Search?.Trim() ?? "";
            var people = ManagerQueueScope.Pending(context, actor).Select(r => r.Employee).Distinct();
            if (search.Length > 0) people = people.Where(e => e.FullName.Contains(search));
            return await people.OrderBy(e => e.FullName).ThenBy(e => e.Id).Take(25)
                .Select(e => new QueuePerson(e.Id, e.FullName)).ToListAsync(token);
        }, cancellationToken);
}
