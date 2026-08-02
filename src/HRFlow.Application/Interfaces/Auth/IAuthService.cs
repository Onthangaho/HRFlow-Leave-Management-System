using HRFlow.Application.DTOs.Auth;
using HRFlow.Application.Models.Auth;

namespace HRFlow.Application.Interfaces.Auth;

/// <summary>
/// Encapsulates authentication and refresh-token rotation so token issuance logic stays outside API endpoint handlers.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Validates credentials and issues a new access token plus refresh token pair when successful.
    /// </summary>
    Task<AuthResult> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    /// <summary>
    /// Validates an existing refresh token, rotates it, and returns a replacement token pair.
    /// </summary>
    Task<AuthResult> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
}