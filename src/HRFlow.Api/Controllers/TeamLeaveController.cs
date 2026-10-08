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
