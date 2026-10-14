using System.Security.Claims;
using HRFlow.Application.Interfaces;
using HRFlow.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace HRFlow.Api.Controllers;

/// <summary>Own-account image transport delegates current authorization and versioned lifecycle to Application.</summary>
[Authorize, ApiController, Route("api/v1/me/image")]
public sealed class ProfileImagesController(ProfileImageService images) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    /// <summary>Returns an independent version even when initials are displayed.</summary>
    [HttpGet]
    public async Task<IActionResult> Metadata(CancellationToken token) => Ok(await images.GetAsync(Actor, token));
    /// <summary>Only processed, scanned bytes become the current image; no target identity is accepted.</summary>
    [HttpPut, EnableRateLimiting("document-upload"), RequestSizeLimit(2 * 1024 * 1024 + 16384),
        RequestFormLimits(MultipartBodyLengthLimit = 2 * 1024 * 1024 + 16384, MemoryBufferThreshold = 2 * 1024 * 1024 + 16384)]
    public async Task<IActionResult> Replace(IFormFile file, [FromForm, BindRequired] Guid expectedVersion,
        [FromForm] string x, [FromForm] string y, [FromForm] string size, CancellationToken token)
    {
        await using var input = file.OpenReadStream();
        try { return Ok(await images.ReplaceAsync(Actor, expectedVersion, input, Path.GetExtension(file.FileName), file.ContentType, new ImageCrop(Coordinate(x), Coordinate(y), Coordinate(size)), token)); }
        catch (ArgumentException error) { return Problem(statusCode: 400, title: "Invalid profile image", detail: error.Message); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException) { return Problem(statusCode: 409, title: "Image unavailable", detail: "Image storage could not complete the operation. Reload and retry explicitly; the previous image is retained."); }
    }
    // Form numeric binding uses server culture; the wire contract deliberately uses invariant decimal fractions.
    private static double Coordinate(string value) => value.Length <= 32 && double.TryParse(value,
        System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var result)
        ? result : throw new ArgumentException("Crop coordinates must be invariant decimal fractions.");
    /// <summary>Explicit removal never touches leave evidence or another account's image.</summary>
    [HttpDelete]
    public async Task<IActionResult> Remove([FromQuery, BindRequired] Guid expectedVersion, CancellationToken token)
    { await images.RemoveAsync(Actor, expectedVersion, token); return NoContent(); }
    /// <summary>Authenticated no-store content has no public reusable URL.</summary>
    [HttpGet("content")]
    [HttpGet("/api/v1/employees/{employeeId:guid}/image")]
    public async Task<IActionResult> Content([FromRoute] Guid? employeeId, CancellationToken token)
    {
        var content = await images.DownloadAsync(Actor, employeeId, token);
        Response.Headers.CacheControl = "no-store"; Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(content, "image/png");
    }
}
