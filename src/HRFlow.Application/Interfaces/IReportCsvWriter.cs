using HRFlow.Application.Features.LeaveRequests.Queries.GetDepartmentLeaveReport;
using HRFlow.Application.Features.LeaveRequests.Queries.GetTeamLeaveSummary;

namespace HRFlow.Application.Interfaces;

/// <summary>Keeps CSV encoding and bounded spreadsheet-safe output in Infrastructure, outside database read snapshots.</summary>
public interface IReportCsvWriter
{
    /// <summary>Writes department metrics without recalculating any leave quantities.</summary>
    byte[] Departments(DepartmentLeaveReportDto report, DateTimeOffset generatedAt, CancellationToken token);
    /// <summary>Writes only the already-authorized team projection, including complete request spans.</summary>
    byte[] Team(IReadOnlyList<TeamLeaveSummaryDto> entries, DateOnly start, DateOnly end, DateTimeOffset generatedAt, CancellationToken token);
}
