using System.ComponentModel.DataAnnotations;
using HRFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace HRFlow.Api.Controllers;
/// <summary>Own-account credential changes; never accepts a target identity.</summary>
[ApiController, Authorize, Route("api/v1/auth/password")]
public sealed class PasswordController(IPasswordChangeService service) : ControllerBase
{
    /// <summary>Ends every previous session, including the caller, only after a committed Identity change.</summary>
    [HttpPost, RequestSizeLimit(PasswordChangeLimits.MaximumBodyBytes), EnableRateLimiting("password-change")]
    public async Task<IActionResult> Change(ChangePasswordRequest request, CancellationToken token)
    {
        Response.Headers.CacheControl = "no-store";
        await service.ChangeAsync(request.CurrentPassword, request.NewPassword, token);
        return NoContent();
    }
}
/// <summary>Bounded secrets used only for this request; actor scope comes from authentication.</summary>
public sealed record ChangePasswordRequest(
    [Required, StringLength(PasswordChangeLimits.MaximumPasswordLength, MinimumLength = 1)] string CurrentPassword,
    [Required, StringLength(PasswordChangeLimits.MaximumPasswordLength, MinimumLength = 1)] string NewPassword);
