namespace HRFlow.Application.Exceptions;

/// <summary>Returns safe Identity validation explanations without leaking password or token values.</summary>
public sealed class EmployeeManagementValidationException(string message) : Exception(message);
