namespace HRFlow.Application.DTOs.Auth;

/// <summary>
/// Carries user credentials for authentication so the API can issue an access token and refresh token pair.
/// </summary>
public class LoginRequest
{
    /// <summary>
    /// Gets or sets the email used to identify the user in ASP.NET Identity.
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the plaintext password that will be verified against the Identity password hash.
    /// </summary>
    public string Password { get; set; } = string.Empty;
}