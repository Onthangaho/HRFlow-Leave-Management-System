using HRFlow.Application.DTOs.Auth;

namespace HRFlow.Application.Models.Auth;

/// <summary>
/// Represents either a successful token issuance outcome or a typed auth failure reason.
/// </summary>
public sealed class AuthResult
{
    private AuthResult(bool succeeded, TokenResponse? tokenResponse, AuthFailureReason? failureReason)
    {
        Succeeded = succeeded;
        TokenResponse = tokenResponse;
        FailureReason = failureReason;
    }

    /// <summary>
    /// Gets a value indicating whether token issuance succeeded.
    /// </summary>
    public bool Succeeded { get; }

    /// <summary>
    /// Gets the issued token pair when authentication succeeds.
    /// </summary>
    public TokenResponse? TokenResponse { get; }

    /// <summary>
    /// Gets the typed failure reason when authentication fails.
    /// </summary>
    public AuthFailureReason? FailureReason { get; }

    /// <summary>
    /// Creates a successful auth result carrying issued tokens.
    /// </summary>
    public static AuthResult Success(TokenResponse tokenResponse) => new(true, tokenResponse, null);

    /// <summary>
    /// Creates a failed auth result carrying a machine-readable reason.
    /// </summary>
    public static AuthResult Failed(AuthFailureReason failureReason) => new(false, null, failureReason);
}