namespace HRFlow.Application.Features.LeaveRequests;

/// <summary>Allows optional manager context only; request and actor identifiers are always server-derived.</summary>
public sealed record LeaveDecisionDto(string? DecisionNote);
