using HRFlow.Domain.Interfaces.Services;
using HRFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Services;

/// <summary>Reads complete capabilities from the Guid-keyed Identity store without exposing account data.</summary>
public sealed class EmployeeRoleLookupService(HRFlowDbContext context) : IEmployeeRoleLookupService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> GetRolesByIdentityUserIdAsync(string? identityUserId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(identityUserId, out var userId)) return [];
        return await (from membership in context.UserRoles
                      join role in context.Roles on membership.RoleId equals role.Id
                      where membership.UserId == userId && role.Name != null
                      orderby role.Name
                      select role.Name!).ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<string> GetRoleNameByIdentityUserIdAsync(string identityUserId, CancellationToken cancellationToken) =>
        (await GetRolesByIdentityUserIdAsync(identityUserId, cancellationToken)).FirstOrDefault() ?? string.Empty;
}
