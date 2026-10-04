using HRFlow.Domain.Entities;

namespace HRFlow.Domain.Services;

/// <summary>
/// Defines the reporting-line and departmental boundary for manager leave decisions.
/// Role membership is intentionally verified by the application authorization service because it
/// belongs to Identity rather than the framework-independent domain model.
/// </summary>
public static class ManagerApprovalScope
{
    /// <summary>
    /// Returns whether the reporting relationship permits a manager to decide the employee's request.
    /// </summary>
    public static bool Includes(Employee manager, Employee requestOwner) =>
        manager.Id != requestOwner.Id
        && requestOwner.ManagerId == manager.Id
        && requestOwner.DepartmentId == manager.DepartmentId;
}
