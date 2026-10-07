namespace HRFlow.Domain.Models.Employees;

/// <summary>Returns the new edit version and cancellation count without disclosing account credentials.</summary>
public sealed record EmployeeDeactivationResult(Guid EmployeeId, Guid Version, bool IsActive, int CancelledRequestCount);
