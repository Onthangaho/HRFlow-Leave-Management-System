using System.Security.Claims;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Features.LeaveTypes.Queries.GetLeaveTypes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

using Microsoft.AspNetCore.Authorization;

/// <summary>Provides the existing minimal selector while Application checks current capabilities in its read snapshot.</summary>
[Authorize]
[ApiController]
[Route("api/v1/leave-types")]
public class LeaveTypesController : ControllerBase
{
    private readonly IMediator _mediator;

    /// <summary>Keeps selector persistence and authorization out of the HTTP adapter.</summary>
    public LeaveTypesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>Supplies only the authenticated actor and propagates request cancellation to the protected read.</summary>
    [HttpGet]
    public async Task<IActionResult> GetLeaveTypes(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            throw new ForbiddenException("A valid authenticated account is required.");
        var leaveTypes = await _mediator.Send(new GetLeaveTypesQuery { ActorIdentityId = actor }, cancellationToken);
        return Ok(leaveTypes);
    }
}
