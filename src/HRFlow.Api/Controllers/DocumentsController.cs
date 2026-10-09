using System.Security.Claims;
using HRFlow.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace HRFlow.Api.Controllers;

/// <summary>Authenticated document transport; ownership and current capabilities are checked by Application.</summary>
[Authorize, ApiController, Route("api/v1/documents")]
public sealed class DocumentsController(SupportingDocumentService documents) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    /// <summary>Multipart uploads never accept employee identity or a storage path.</summary>
    [HttpPost, EnableRateLimiting("document-upload"), RequestSizeLimit(11 * 1024 * 1024), RequestFormLimits(MultipartBodyLengthLimit = 11 * 1024 * 1024)]
    public async Task<IActionResult> Upload(IFormFile file, [FromForm] Guid uploadKey, [FromForm] string classification, CancellationToken token)
    {
        await using var stream = file.OpenReadStream();
        try { return Ok(await documents.UploadAsync(Actor, uploadKey, classification, stream, Path.GetExtension(file.FileName), file.ContentType, token)); }
        catch (ArgumentException) { return Problem(statusCode: 400, title: "Invalid document", detail: "Upload a valid PDF, JPEG or PNG within the file limit and choose its evidence class. A failed draft may be removed before retrying."); }
    }
    /// <summary>Lists owned drafts or role-filtered request evidence without exposing storage identifiers.</summary>
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] Guid? requestId, CancellationToken token) => Ok(await documents.ListAsync(Actor, requestId, token));
    /// <summary>Explicit rescanning is bounded and never performed by authentication replay.</summary>
    [HttpPost("{id:guid}/scan"), EnableRateLimiting("document-upload")]
    public async Task<IActionResult> Scan(Guid id, CancellationToken token) => Ok(await documents.ScanAsync(Actor, id, token));
    /// <summary>All content is authenticated download-only, including PDFs.</summary>
    [HttpGet("{id:guid}/download")]
    public async Task<IActionResult> Download(Guid id, CancellationToken token)
    {
        var result = await documents.DownloadAsync(Actor, id, token);
        Response.Headers.CacheControl = "no-store"; Response.Headers["X-Content-Type-Options"] = "nosniff";
        return File(result.Content, result.MediaType, "supporting-document" + (result.MediaType == "application/pdf" ? ".pdf" : result.MediaType == "image/png" ? ".png" : ".jpg"));
    }
    /// <summary>Original version protects draft removal; bound history remains immutable.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Remove(Guid id, [FromQuery] Guid expectedVersion, CancellationToken token)
    { await documents.RemoveAsync(Actor, id, expectedVersion, token); return NoContent(); }
}
