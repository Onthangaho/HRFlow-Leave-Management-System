using HRFlow.Domain.Entities;

namespace HRFlow.Application.Interfaces;

/// <summary>
/// Resolves the authenticated caller to their corresponding domain Employee record.
/// ASP.NET Core Identity user IDs (from the JWT "sub" claim / ClaimTypes.NameIdentifier)
/// are intentionally separate from Employee.Id because an employee may exist in the HR
/// domain before being granted a login account, and because role/identity concerns
/// belong to the Identity subsystem, not the domain model. Consumers of this interface
/// should always use the resolved Employee.Id (domain key) when referencing ApproverId,
/// RejectorId, ManagerId, or EmployeeId — never the raw Identity claim value.
/// </summary>
public interface ICurrentEmployeeProvider
{
    /// <summary>
    /// Returns the domain Employee for the currently authenticated identity user,
    /// or null if the caller is anonymous or no matching Employee record is linked.
    /// </summary>
    System.Threading.Tasks.Task<Employee?> GetCurrentEmployeeAsync(System.Threading.CancellationToken cancellationToken);
}
