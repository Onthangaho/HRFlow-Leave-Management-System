namespace HRFlow.Application.Models.Auth;

/// <summary>
/// Indicates why an authentication flow failed so endpoints can map failures to RFC 7807 Problem Details responses.
/// </summary>
public enum AuthFailureReason
{
    /// <summary>
    /// Credentials were invalid for the specified user identity.
    /// </summary>
    InvalidCredentials,

    /// <summary>
    /// Refresh token was missing, expired, revoked, or not recognized.
    /// </summary>
    InvalidRefreshToken
}