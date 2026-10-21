using HRFlow.Application.Features.LeaveRequests.Queries.GetHrWorkspace;

namespace HRFlow.Application.Interfaces;

/// <summary>Only aggregated workforce facts leave persistence; no full employee, Identity or schedule records are needed.</summary>
public sealed record HrWorkforceAttention(int ActiveEmployees, int InactiveEmployees,
    int UnknownEmploymentFacts, int MissingCurrentSchedules, int FutureScheduleOnly,
    int PendingActivation, int DeliveryFailed, int DeliveryPending, IReadOnlyList<HrDepartmentDto> Departments);

/// <summary>Uses the caller's already-authorized deferred snapshot; it neither authorizes independently nor starts a transaction.</summary>
public interface IHrWorkforceAttentionReader
{
    /// <summary>One captured UTC instant also determines SAST schedule applicability and invitation expiry for every row.</summary>
    Task<HrWorkforceAttention> ReadAsync(DateTimeOffset asOfUtc, CancellationToken token);
}
