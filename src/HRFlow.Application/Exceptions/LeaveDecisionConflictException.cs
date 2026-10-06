namespace HRFlow.Application.Exceptions;

/// <summary>Reports a stale decision or exhausted database contention without exposing database details.</summary>
public sealed class LeaveDecisionConflictException : Exception
{
    /// <summary>Provides a safe explanation that the API can return as a conflict.</summary>
    public LeaveDecisionConflictException(string message) : base(message)
    {
    }
}
