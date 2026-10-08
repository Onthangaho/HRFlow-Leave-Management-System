using System.Security.Claims;
using HRFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
namespace HRFlow.Api.Controllers;
/// <summary>Maps anonymous token redemption and current-HR resend without exposing invitations.</summary>
[ApiController]
[Route("api/v1")]
public sealed class ActivationController(IAccountActivationService activation) : ControllerBase
{
    /// <summary>Establishes a first password; account scope comes solely from the verified opaque token.</summary>
    [AllowAnonymous, HttpPost("auth/activation"), EnableRateLimiting("activation"), RequestSizeLimit(4096)]
    public async Task<IActionResult> Redeem(RedeemActivation request, CancellationToken token)
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["Referrer-Policy"] = "no-referrer";
        await activation.RedeemAsync(request.Token, request.Password, token);
        return NoContent();
    }
    /// <summary>Explicitly replaces an invitation, with current-HR checks inside the reserved transaction.</summary>
    [Authorize(Policy = "HrAdministratorOnly"), HttpPost("employees/{id:guid}/activation/resend"), EnableRateLimiting("activation"), RequestSizeLimit(1024)]
    public async Task<IActionResult> Resend(Guid id, ResendActivation request, CancellationToken token)
    {
        var state = await activation.ResendAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), id, request.ExpectedVersion, token);
        return Ok(new { invitationDeliveryState = state });
    }
}
/// <summary>No client-selected employee identifier is accepted during redemption.</summary>
public sealed record RedeemActivation(string Token, string Password);
/// <summary>Original employee version prevents concurrent resends from silently replacing one another.</summary>
public sealed record ResendActivation(Guid ExpectedVersion);
