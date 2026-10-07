using HRFlow.Application.Interfaces;
using HRFlow.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Infrastructure.Services;

/// <summary>Reads lifecycle state for bearer validation and protected token issuance.</summary>
public sealed class AccountAccessService(HRFlowDbContext context) : IAccountAccessService
{
    /// <inheritdoc />
    public Task<bool> IsActiveAsync(string? identityUserId, CancellationToken cancellationToken) =>
        Guid.TryParse(identityUserId, out var id)
            ? context.Employees.AsNoTracking().AnyAsync(e => e.IdentityUserId == identityUserId && e.IsActive
                && context.Users.Any(user => user.Id == id), cancellationToken)
            : Task.FromResult(false);
}
