namespace HRFlow.Application.DTOs.Auth;

/// <summary>
/// Carries a refresh token so the API can rotate it and issue a replacement access token.
/// </summary>
public class RefreshTokenRequest
{
    /// <summary>
    /// Gets or sets the opaque refresh token value previously returned during login or refresh.
    /// </summary>
    public string RefreshToken { get; set; } = string.Empty;
}