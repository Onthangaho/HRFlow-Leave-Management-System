using HRFlow.Application.Exceptions;
using HRFlow.Application.Interfaces;
namespace HRFlow.Infrastructure.Services;
/// <summary>Uses the caller's DbContext transaction so revocation and business reads have one serial boundary.</summary>
public sealed class RequestCredentialValidator(IInitiatingCredential credential, IAccountAccessService access) : IRequestCredentialValidator
{
    /// <inheritdoc />
    public async Task RequireCurrentAsync(CancellationToken token)
    {
        if (credential.IdentityUserId is {} id && !await access.IsCurrentAsync(id, credential.Version, token))
            throw new InvalidCredentialsException("Your session is no longer valid. Sign in again.");
    }
}
