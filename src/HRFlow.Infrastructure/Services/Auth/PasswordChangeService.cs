using HRFlow.Application.Exceptions;
using HRFlow.Domain.Common;
using HRFlow.Application.Interfaces;
using HRFlow.Domain.Entities;
using HRFlow.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
namespace HRFlow.Infrastructure.Services.Auth;
/// <summary>Serializes Identity password establishment and all-session revocation with existing SQLite writers.</summary>
public sealed class PasswordChangeService(HRFlowDbContext context, UserManager<ApplicationUser> users,
    IInitiatingCredential actor, IAccountAccessService access, IEmployeeManagementTransaction transaction,
    ILogger<PasswordChangeService> logger, IRequestCorrelationContext correlation) : IPasswordChangeService
{
    /// <inheritdoc />
    public async Task ChangeAsync(string currentPassword, string newPassword, CancellationToken token)
    {
        if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword)
            || currentPassword.Length > PasswordChangeLimits.MaximumPasswordLength || newPassword.Length > PasswordChangeLimits.MaximumPasswordLength)
            throw new DomainException("Current and new passwords are required and must not exceed 256 characters.");
        await transaction.ExecuteAsync(async ct =>
        {
            if (!await access.IsCurrentAsync(actor.IdentityUserId, actor.Version, ct))
                throw new InvalidCredentialsException("Your session is no longer valid. Sign in again.");
            var user = await users.FindByIdAsync(actor.IdentityUserId!);
            var result = await users.ChangePasswordAsync(user!, currentPassword, newPassword);
            if (!result.Succeeded) throw new DomainException(string.Join(" ", result.Errors.Select(e => e.Description)));
            user!.CredentialVersion = Guid.NewGuid();
            await context.RefreshTokens.Where(r => r.UserId == user.Id.ToString() && r.RevokedAtUtc == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(r => r.RevokedAtUtc, DateTime.UtcNow), ct);
        }, token);
        logger.LogInformation("Password change committed. ActorId: {ActorId}; CorrelationId: {CorrelationId}; Outcome: {Outcome}",
            actor.IdentityUserId, correlation.CorrelationId, "AllPreviousSessionsRevoked");
    }
}
