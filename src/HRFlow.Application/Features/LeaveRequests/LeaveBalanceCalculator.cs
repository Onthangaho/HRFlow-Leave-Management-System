using HRFlow.Domain.Entities;

namespace HRFlow.Application.Features.LeaveRequests;

/// <summary>
/// Calculates leave use from approved history so submission validation and read models derive the
/// same balance instead of storing a value that can drift when approvals are backdated.
/// </summary>
internal static class LeaveBalanceCalculator
{
    /// <summary>
    /// Returns the policy entitlement, approved days already used, and remaining balance for one
    /// leave type. Callers must pass only approved requests for that leave type.
    /// </summary>
    public static LeaveBalance Calculate(
        int policyBalance,
        IEnumerable<LeaveRequest> approvedRequests)
    {
        var usedDays = approvedRequests.Sum(request => request.GetRequestedDays());
        return new LeaveBalance(policyBalance, usedDays);
    }
}

/// <summary>
/// Captures a calculated balance snapshot so callers share unambiguous entitlement, use, and
/// remaining-day values.
/// </summary>
internal sealed record LeaveBalance(int EntitledDays, int UsedDays)
{
    /// <summary>
    /// Gets the currently available days after subtracting approved leave from the entitlement.
    /// </summary>
    public int RemainingDays => EntitledDays - UsedDays;
}
