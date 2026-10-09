using System.Security.Claims;
using HRFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

/// <summary>Own-account transport accepts no target identifier and delegates all protected decisions.</summary>
[Authorize]
[ApiController]
[Route("api/v1/me")]
public sealed class MeController(IOwnAccountService account) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Minimal read-only canonical facts plus private allowlisted self fields.</summary>
    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken token) => Ok(await account.GetProfileAsync(Actor, token));

    /// <summary>Reads implemented preferences without side effects or client target scope.</summary>
    [HttpGet("preferences")]
    public async Task<IActionResult> Preferences(CancellationToken token) => Ok(await account.GetPreferencesAsync(Actor, token));

    /// <summary>Bounded own-account metadata write; unknown privilege fields fail model binding.</summary>
    [HttpPut("profile"), RequestSizeLimit(4096)]
    public async Task<IActionResult> SaveProfile(ProfileUpdateDto update, CancellationToken token) =>
        Ok(await account.SaveProfileAsync(Actor, update, token));

    /// <summary>Versioned full replacement of implemented preferences only.</summary>
    [HttpPut("preferences"), RequestSizeLimit(4096)]
    public async Task<IActionResult> SavePreferences(PreferencesUpdateDto update, CancellationToken token) =>
        Ok(await account.SavePreferencesAsync(Actor, update, token));
}
