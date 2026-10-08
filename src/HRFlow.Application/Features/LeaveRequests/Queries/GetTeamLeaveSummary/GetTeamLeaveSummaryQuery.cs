using FluentValidation;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetTeamLeaveSummary;

/// <summary>Describes an inclusive calendar range, with the Identity actor supplied only by the controller.</summary>
public sealed record GetTeamLeaveSummaryQuery(Guid ActorIdentityId, DateOnly Start, DateOnly End)
    : IRequest<IReadOnlyList<TeamLeaveSummaryDto>>
{
    public const int MaximumRangeDays = 62;
}

/// <summary>Exposes coverage-planning information without unrelated employee or lifecycle details.</summary>
public sealed record TeamLeaveSummaryDto(
    Guid RequestId, Guid EmployeeId, string EmployeeName, bool IsActive,
    Guid LeaveTypeId, string LeaveTypeName, DateTime StartDate, DateTime EndDate);

/// <summary>Bounds read size to a month-friendly range and rejects missing or reversed dates.</summary>
public sealed class GetTeamLeaveSummaryQueryValidator : AbstractValidator<GetTeamLeaveSummaryQuery>
{
    /// <summary>Uses inclusive calendar days, matching leave duration semantics.</summary>
    public GetTeamLeaveSummaryQueryValidator()
    {
        RuleFor(query => query.Start).NotEmpty().WithMessage("A start date is required (YYYY-MM-DD).");
        RuleFor(query => query.End).NotEmpty().WithMessage("An end date is required (YYYY-MM-DD).");
        RuleFor(query => query.End).GreaterThanOrEqualTo(query => query.Start)
            .WithMessage("End date must be on or after start date.");
        RuleFor(query => query).Must(query => query.End.DayNumber - query.Start.DayNumber + 1 <= GetTeamLeaveSummaryQuery.MaximumRangeDays)
            .WithMessage($"Select no more than {GetTeamLeaveSummaryQuery.MaximumRangeDays} inclusive calendar days.");
    }
}

/// <summary>Reads current reporting scope, live capabilities and approved history from a single database snapshot.</summary>
public sealed class GetTeamLeaveSummaryQueryHandler(
    IApplicationDbContext context,
    IEmployeeRoleLookupService roles,
    ITeamLeaveReadTransaction transaction)
    : IRequestHandler<GetTeamLeaveSummaryQuery, IReadOnlyList<TeamLeaveSummaryDto>>
{
    private const string ManagerRole = "Manager";

    /// <summary>Includes inactive reports' preserved approvals, but never the manager's own leave.</summary>
    public Task<IReadOnlyList<TeamLeaveSummaryDto>> Handle(GetTeamLeaveSummaryQuery request, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync<IReadOnlyList<TeamLeaveSummaryDto>>(async token =>
        {
            var identityId = request.ActorIdentityId.ToString();
            var manager = await context.Employees.AsNoTracking()
                .SingleOrDefaultAsync(employee => employee.IdentityUserId == identityId, token);
            if (manager is null || !manager.IsActive
                || !(await roles.GetRolesByIdentityUserIdAsync(manager.IdentityUserId, token)).Contains(ManagerRole))
                throw new ForbiddenException("An active account with current Manager membership is required to view team leave.");

            var start = request.Start.ToDateTime(TimeOnly.MinValue);
            var end = request.End.ToDateTime(TimeOnly.MaxValue);
            return await context.LeaveRequests.AsNoTracking()
                .Where(leave => leave.Status == LeaveRequestStatus.Approved
                    && leave.EmployeeId != manager.Id
                    && leave.Employee.ManagerId == manager.Id
                    && leave.Employee.DepartmentId == manager.DepartmentId
                    && leave.StartDate <= end && leave.EndDate >= start)
                .OrderBy(leave => leave.StartDate).ThenBy(leave => leave.Employee.FullName).ThenBy(leave => leave.Id)
                .Select(leave => new TeamLeaveSummaryDto(leave.Id, leave.EmployeeId, leave.Employee.FullName,
                    leave.Employee.IsActive, leave.LeaveTypeId, leave.LeaveType.Name, leave.StartDate, leave.EndDate))
                .ToListAsync(token);
        }, cancellationToken);
}
