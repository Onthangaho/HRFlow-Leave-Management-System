using HRFlow.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/v1/leave-requests")]
public class LeaveRequestsController : ControllerBase
{
    private readonly IMediator _mediator;

    public LeaveRequestsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitLeaveRequest(SubmitLeaveRequestCommand command)
    {
        var leaveRequestId = await _mediator.Send(command);
        return CreatedAtAction(nameof(SubmitLeaveRequest), new { id = leaveRequestId }, leaveRequestId);
    }
}