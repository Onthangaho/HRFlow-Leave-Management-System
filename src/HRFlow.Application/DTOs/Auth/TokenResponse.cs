namespace HRFlow.Application.DTOs.Auth;

/// <summary>
/// Returns the token pair required by clients to call protected APIs and renew sessions without re-entering credentials.
/// </summary>
public class TokenResponse
{
    /// <summary>
    /// Gets or sets the short-lived JWT access token used for API authorization.
    /// </summary>
    public string AccessToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the server-tracked refresh token used to obtain the next access token.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC expiration moment of the access token.
    /// </summary>
    public DateTime AccessTokenExpiresAtUtc { get; set; }
}