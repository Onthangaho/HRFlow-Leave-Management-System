namespace HRFlow.Application.Exceptions;

/// <summary>Distinguishes stale edits and expected write contention from validation or unexpected database failures.</summary>
public sealed class WriteConflictException(string message) : Exception(message);
