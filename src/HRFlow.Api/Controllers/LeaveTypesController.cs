using HRFlow.Application.Features.LeaveTypes.Queries.GetLeaveTypes;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/v1/leave-types")]
public class LeaveTypesController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeaveTypesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetLeaveTypes()
    {
        var leaveTypes = await _mediator.Send(new GetLeaveTypesQuery());
        return Ok(leaveTypes);
    }
}