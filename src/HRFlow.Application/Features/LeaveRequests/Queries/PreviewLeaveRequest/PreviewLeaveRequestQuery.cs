using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using HRFlow.Domain.Entities;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Models.Employees;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Queries.PreviewLeaveRequest;

/// <summary>Date-only preview inputs exclude employee identity, descriptions and files. Actor scope is supplied separately by authentication.</summary>
public sealed record LeavePreviewInput(Guid LeaveTypeId, DateOnly StartDate, DateOnly EndDate);
/// <summary>Internal authenticated actor cannot be selected through the HTTP body.</summary>
public sealed record PreviewLeaveRequestQuery(Guid ActorIdentityId, LeavePreviewInput Input) : IRequest<LeavePreviewDto>;
/// <summary>Current requirements are informational; receiving these versions never reviews or replaces the caller's original submission versions.</summary>
public sealed record PreviewRequirementsDto(Guid TypeVersion, Guid PolicyId, Guid PolicyVersion,
    string DescriptionMode, string EvidenceMode, string EvidenceClass, string? Instructions);
/// <summary>A non-binding current-model assessment, not evidence validation, a reservation or a promise of approval.</summary>
public sealed record LeavePreviewDto(DateTimeOffset CapturedAtUtc, Guid LeaveTypeId, DateOnly StartDate, DateOnly EndDate,
    string Unit, string SubmissionDateBasis, int? RequestedDays, int EntitledDays, int ApprovedUsedDays,
    int RemainingDays, int? ProjectedRemainingDaysIfApproved, PreviewRequirementsDto Requirements,
    IReadOnlyList<LeavePolicyProblem> Findings);

/// <summary>Uses the same calculator, date rules, manager service and domain policy rules as submission, in a credential-checked deferred read snapshot.</summary>
public sealed class PreviewLeaveRequestQueryHandler(IApplicationDbContext context, CurrentAccountAuthorization authorization,
    ILeaveApprovalAuthorizationService managers, ILeaveReportingReadTransaction reads, TimeProvider clock)
    : IRequestHandler<PreviewLeaveRequestQuery, LeavePreviewDto>
{
    /// <inheritdoc />
    public Task<LeavePreviewDto> Handle(PreviewLeaveRequestQuery query, CancellationToken cancellationToken) =>
        reads.ExecuteAsync(async token =>
        {
            var employee = await authorization.RequireIdentityAsync(query.ActorIdentityId,
                [EmployeeRoles.Employee, EmployeeRoles.Manager], token);
            var captured = clock.GetUtcNow();
            var input = query.Input;
            if (input.LeaveTypeId == Guid.Empty || input.StartDate == default || input.EndDate == default)
                throw new HRFlow.Domain.Common.DomainException("Choose a leave type and valid start and end dates.");
            var type = await context.LeaveTypes.AsNoTracking().Include(t => t.LeavePolicy)
                .SingleOrDefaultAsync(t => t.Id == input.LeaveTypeId, token)
                ?? throw new WriteConflictException("This leave type is no longer available. Refresh and explicitly review requirements before submitting.");
            var start = input.StartDate.ToDateTime(TimeOnly.MinValue);
            var end = input.EndDate.ToDateTime(TimeOnly.MinValue);
            var findings = LeaveSubmissionDateRules.Problems(start, end, captured.UtcDateTime.Date).ToList();
            // Authorization failures above remain failures. This service's relationship denial is an owner-correctable submission prerequisite.
            try { await managers.EnsureEmployeeHasValidManagerAsync(employee, token); }
            catch (ForbiddenException) { findings.Add(new("ManagerAssignment", "An eligible assigned manager in your department is required. Contact HR to check your reporting assignment.")); }
            var approved = await context.LeaveRequests.AsNoTracking().Where(r => r.EmployeeId == employee.Id
                && r.LeaveTypeId == type.Id && r.Status == LeaveRequestStatus.Approved).ToListAsync(token);
            var balance = LeaveBalanceCalculator.Calculate(type.LeavePolicy.DefaultBalance, approved);
            int? days = end >= start ? LeaveRequest.CalendarDays(start, end) : null;
            if (days.HasValue)
                findings.AddRange(LeaveRequest.PolicyProblems(type.Id, start, end, balance.RemainingDays, approved, type.LeavePolicy));
            return new LeavePreviewDto(captured, type.Id, input.StartDate, input.EndDate,
                "inclusive calendar days (including weekends)", "UTC calendar date, matching current submission validation",
                days, balance.EntitledDays, balance.UsedDays, balance.RemainingDays,
                days.HasValue ? balance.RemainingDays - days.Value : null,
                new(type.Version, type.LeavePolicyId, type.LeavePolicy.Version, type.DescriptionMode, type.EvidenceMode,
                    type.EvidenceClass, type.RequirementInstructions), findings);
        }, cancellationToken);
}
