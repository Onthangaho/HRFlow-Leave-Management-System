using FluentValidation;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Models.Employees;
using HRFlow.Domain.Enums;
using HRFlow.Domain.Interfaces;
using HRFlow.Domain.Interfaces.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Application.Features.LeaveRequests.Queries.GetDepartmentLeaveReport;

/// <summary>Requests an inclusive, bounded report; the controller alone supplies the authenticated Identity actor.</summary>
public sealed record GetDepartmentLeaveReportQuery(Guid ActorIdentityId, DateOnly Start, DateOnly End, Guid? DepartmentId)
    : IRequest<DepartmentLeaveReportDto>
{
    public const int MaximumRangeDays = 366;
}

/// <summary>Separates summed request durations from distinct employees and identifies inactive historical contributions.</summary>
public sealed record LeaveReportMetricsDto(int PendingRequests, int ApprovedRequests, int EmployeesWithApprovedLeave,
    long ApprovedRequestDays, int InactiveApprovedRequests, int InactiveEmployeesWithApprovedLeave, long InactiveApprovedRequestDays);

/// <summary>Includes zero-result departments without exposing employee-level personal information.</summary>
public sealed record DepartmentLeaveBreakdownDto(Guid DepartmentId, string DepartmentName, LeaveReportMetricsDto Metrics);

/// <summary>Returns the applied period/scope alongside totals so draft UI filters cannot relabel an old result.</summary>
public sealed record DepartmentLeaveReportDto(DateOnly Start, DateOnly End, Guid? DepartmentId,
    LeaveReportMetricsDto Totals, IReadOnlyList<DepartmentLeaveBreakdownDto> Departments);

/// <summary>Bounds aggregate work and distinguishes malformed selection from a valid empty result.</summary>
public sealed class GetDepartmentLeaveReportQueryValidator : AbstractValidator<GetDepartmentLeaveReportQuery>
{
    /// <summary>Uses inclusive calendar days rather than introducing annual or working-day rules.</summary>
    public GetDepartmentLeaveReportQueryValidator()
    {
        RuleFor(query => query.Start).NotEmpty().WithMessage("A start date is required (YYYY-MM-DD).");
        RuleFor(query => query.End).NotEmpty().WithMessage("An end date is required (YYYY-MM-DD).");
        RuleFor(query => query.End).GreaterThanOrEqualTo(query => query.Start).WithMessage("End date must be on or after start date.");
        RuleFor(query => query).Must(query => query.End.DayNumber - query.Start.DayNumber + 1 <= GetDepartmentLeaveReportQuery.MaximumRangeDays)
            .WithMessage($"Select no more than {GetDepartmentLeaveReportQuery.MaximumRangeDays} inclusive calendar days.");
        RuleFor(query => query.DepartmentId).NotEqual(Guid.Empty).When(query => query.DepartmentId.HasValue)
            .WithMessage("Choose an existing department or omit the department filter.");
    }
}

/// <summary>Attributes preserved leave to current departments and clips each Approved duration before summing.</summary>
public sealed class GetDepartmentLeaveReportQueryHandler(IApplicationDbContext context,
    IEmployeeRoleLookupService roles, ILeaveReportingReadTransaction transaction)
    : IRequestHandler<GetDepartmentLeaveReportQuery, DepartmentLeaveReportDto>
{
    /// <summary>Reads authorization and all aggregate inputs from the same snapshot; no decisions or audit writes occur.</summary>
    public Task<DepartmentLeaveReportDto> Handle(GetDepartmentLeaveReportQuery request, CancellationToken cancellationToken) =>
        transaction.ExecuteAsync(async token =>
        {
            var identityId = request.ActorIdentityId.ToString();
            if (!await context.Employees.AsNoTracking().AnyAsync(employee => employee.IdentityUserId == identityId && employee.IsActive, token)
                || !(await roles.GetRolesByIdentityUserIdAsync(identityId, token)).Contains(EmployeeRoles.HrAdministrator))
                throw new ForbiddenException("An active account with current HR Administrator membership is required to view leave reports.");

            var departments = await context.Departments.AsNoTracking()
                .Where(department => !request.DepartmentId.HasValue || department.Id == request.DepartmentId.Value)
                .OrderBy(department => department.Name).ThenBy(department => department.Id)
                .Select(department => new { department.Id, department.Name }).ToListAsync(token);
            if (request.DepartmentId.HasValue && departments.Count == 0)
                throw new NotFoundException("The selected department was not found. Choose an existing department.");

            var start = request.Start.ToDateTime(TimeOnly.MinValue);
            var end = request.End.ToDateTime(TimeOnly.MaxValue);
            var leaves = await context.LeaveRequests.AsNoTracking()
                .Where(leave => (leave.Status == LeaveRequestStatus.Pending || leave.Status == LeaveRequestStatus.Approved)
                    && leave.StartDate <= end && leave.EndDate >= start
                    && (!request.DepartmentId.HasValue || leave.Employee.DepartmentId == request.DepartmentId.Value))
                .Select(leave => new ReportRow(leave.EmployeeId, leave.Employee.DepartmentId, leave.Employee.IsActive,
                    leave.Status, leave.StartDate, leave.EndDate)).ToListAsync(token);

            var byDepartment = leaves.ToLookup(leave => leave.DepartmentId);
            var breakdown = departments.Select(department => new DepartmentLeaveBreakdownDto(department.Id, department.Name,
                Calculate(byDepartment[department.Id], request.Start, request.End))).ToList();
            return new DepartmentLeaveReportDto(request.Start, request.End, request.DepartmentId,
                Calculate(leaves, request.Start, request.End), breakdown);
        }, cancellationToken);

    private static LeaveReportMetricsDto Calculate(IEnumerable<ReportRow> rows, DateOnly start, DateOnly end)
    {
        var leaves = rows.ToList();
        var approved = leaves.Where(leave => leave.Status == LeaveRequestStatus.Approved).ToList();
        var inactive = approved.Where(leave => !leave.IsActive).ToList();
        long Days(ReportRow leave) => Math.Min(DateOnly.FromDateTime(leave.EndDate).DayNumber, end.DayNumber)
            - Math.Max(DateOnly.FromDateTime(leave.StartDate).DayNumber, start.DayNumber) + 1L;
        // Overlap is deliberately summed per request, not misrepresented as unique employee absence.
        return new LeaveReportMetricsDto(leaves.Count(leave => leave.Status == LeaveRequestStatus.Pending), approved.Count,
            approved.Select(leave => leave.EmployeeId).Distinct().Count(), approved.Sum(Days), inactive.Count,
            inactive.Select(leave => leave.EmployeeId).Distinct().Count(), inactive.Sum(Days));
    }

    private sealed record ReportRow(Guid EmployeeId, Guid DepartmentId, bool IsActive, LeaveRequestStatus Status, DateTime StartDate, DateTime EndDate);
}
