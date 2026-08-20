using HRFlow.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

using System.Security.Claims;
using HRFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

[Authorize]
[ApiController]
[Route("api/v1/leave-requests")]
public class LeaveRequestsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IApplicationDbContext _context;

    public LeaveRequestsController(IMediator mediator, IApplicationDbContext context)
    {
        _mediator = mediator;
        _context = context;
    }

    [HttpPost]
    public async Task<IActionResult> SubmitLeaveRequest(SubmitLeaveRequestCommand command)
    {
        var identityUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(identityUserId))
        {
            return Unauthorized();
        }

        var employee = await _context.Employees
            .FirstOrDefaultAsync(e => e.IdentityUserId == identityUserId);
        if (employee == null)
        {
            return Unauthorized();
        }

        command.EmployeeId = employee.Id;
        var leaveRequestId = await _mediator.Send(command);
        return Ok(leaveRequestId);
    }
}