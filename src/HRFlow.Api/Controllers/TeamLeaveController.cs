using System.Security.Claims;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Features.LeaveRequests.Queries.GetTeamLeaveSummary;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

/// <summary>Maps the authenticated manager's coverage-planning range to an Application query.</summary>
[ApiController]
[Authorize(Roles = "Manager")]
[Route("api/v1/leave-requests/team-summary")]
public sealed class TeamLeaveController(IMediator mediator) : ControllerBase
{
    /// <summary>Exports only the authenticated Manager's fresh current reporting scope, never an arbitrary employee list.</summary>
    [HttpGet("export")]
    public async Task<IActionResult> Export([FromQuery] DateOnly start, [FromQuery] DateOnly end, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            throw new ForbiddenException("A valid authenticated account is required.");
        Response.Headers.CacheControl = "no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
        var file = await mediator.Send(new HRFlow.Application.Features.LeaveRequests.Queries.ExportLeaveReports.ExportTeamLeaveQuery(actor, start, end), cancellationToken);
        return File(file.Content, "text/csv; charset=utf-8", file.FileName);
    }

    /// <summary>Returns a bounded decision preview and total count from the authenticated Manager's live scope.</summary>
    [HttpGet("/api/v1/leave-requests/manager-attention")]
    public async Task<IActionResult> Attention(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            throw new ForbiddenException("A valid authenticated account is required.");
        return Ok(await mediator.Send(new HRFlow.Application.Features.LeaveRequests.Queries.GetManagerAttention.GetManagerAttentionQuery(actor), cancellationToken));
    }

    /// <summary>Returns intersecting approved leave for current same-department direct reports, including inactive history.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TeamLeaveSummaryDto>>> Get(
        [FromQuery] DateOnly start, [FromQuery] DateOnly end, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            throw new ForbiddenException("A valid authenticated account is required.");
        return Ok(await mediator.Send(new GetTeamLeaveSummaryQuery(actor, start, end), cancellationToken));
    }
}
