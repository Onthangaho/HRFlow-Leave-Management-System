using System.Security.Claims;
using HRFlow.Application.Interfaces;
namespace HRFlow.Api.Services;
/// <summary>Reads the original bearer proof without passing framework objects into Application.</summary>
public sealed class InitiatingCredential(IHttpContextAccessor accessor) : IInitiatingCredential
{
    /// <inheritdoc />
    public string? IdentityUserId => accessor.HttpContext?.User.Identity?.IsAuthenticated == true
        ? accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier) : null;
    /// <inheritdoc />
    public string? Version => accessor.HttpContext?.User.FindFirstValue(CredentialClaims.Version);
}
