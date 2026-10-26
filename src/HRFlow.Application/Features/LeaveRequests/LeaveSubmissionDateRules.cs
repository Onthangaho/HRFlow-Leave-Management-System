using HRFlow.Domain.Entities;

namespace HRFlow.Application.Features.LeaveRequests;

/// <summary>Shares submission date findings with preview; retains the existing UTC-date lower bound, not a new statutory/date rule.</summary>
public static class LeaveSubmissionDateRules
{
    /// <summary>Evaluates ordered, nonempty dates against a caller-captured UTC day without wall-clock drift between findings.</summary>
    public static IReadOnlyList<LeavePolicyProblem> Problems(DateTime start, DateTime end, DateTime utcToday)
    {
        var problems = new List<LeavePolicyProblem>();
        if (start == default || start < utcToday.Date)
            problems.Add(new("StartDate", "Start date must be today or later (UTC submission-date basis)."));
        if (end == default || end < start)
            problems.Add(new("EndDate", "End date must be on or after the start date."));
        return problems;
    }
}
