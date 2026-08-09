using HRFlow.Application.DTOs.Auth;
using HRFlow.Application.Interfaces.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

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