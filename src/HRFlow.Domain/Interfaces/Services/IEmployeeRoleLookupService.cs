namespace HRFlow.Domain.Interfaces.Services;

/// <summary>
/// Provides role lookup operations for employees based on their ASP.NET Core Identity user ID.
/// </summary>
public interface IEmployeeRoleLookupService
{
    /// <summary>
    /// Retrieves the role name assigned to the specified Identity user.
    /// </summary>
    /// <param name="identityUserId">The ASP.NET Core Identity user ID to look up.</param>
    /// <param name="cancellationToken">Cancellation token for the async operation.</param>
    /// <returns>
    /// The name of the first role assigned to the user, or an empty string if the user has no roles
    /// or if the provided <paramref name="identityUserId"/> is null or empty.
    /// </returns>
    System.Threading.Tasks.Task<string> GetRoleNameByIdentityUserIdAsync(string identityUserId, System.Threading.CancellationToken cancellationToken);
}