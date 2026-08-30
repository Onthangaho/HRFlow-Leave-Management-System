using HRFlow.Domain.DTOs.Auth;
using HRFlow.Domain.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

/// <summary>
/// Handles authentication operations including login and refresh token workflows.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    /// <summary>
    /// Authenticates a user with credentials and returns JWT access and refresh tokens.
    /// Returns 401 Unauthorized with RFC 7807 ProblemDetails on invalid credentials.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken cancellationToken)
    {
        var authResult = await _authService.LoginAsync(request, cancellationToken);
        if (!authResult.Succeeded)
        {
            return Unauthorized(new ProblemDetails
            {
                Title = "Invalid credentials",
                Detail = authResult.FailureReason.ToString(),
                Status = StatusCodes.Status401Unauthorized,
                Type = "https://www.rfc-editor.org/rfc/rfc7807"
            });
        }
        return Ok(authResult.TokenResponse);
    }

    /// <summary>
    /// Exchanges a valid refresh token for a new access token and refresh token pair.
    /// Implements refresh token rotation by invalidating the old token and issuing a new one.
    /// Returns 401 Unauthorized if the refresh token is invalid, expired, or revoked.
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var authResult = await _authService.RefreshAsync(request, cancellationToken);
        if (!authResult.Succeeded)
        {
            return Unauthorized("Invalid refresh token.");
        }
        return Ok(authResult.TokenResponse);
    }
}