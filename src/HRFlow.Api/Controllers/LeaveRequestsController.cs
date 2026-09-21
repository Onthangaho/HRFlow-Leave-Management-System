using HRFlow.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;
using HRFlow.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;
using HRFlow.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;
using HRFlow.Application.Features.LeaveRequests.Queries.GetPendingLeaveRequests;
using HRFlow.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/leave-requests")]
public class LeaveRequestsController : ControllerBase
{
    private const string HrAdministratorRoleName = "HR Administrator";
    private const string ManagerAndHrAdministratorRoles = "HR Administrator,Manager";
    private const string PendingStatus = "Pending";

    private readonly IMediator _mediator;
    private readonly ICurrentEmployeeProvider _currentEmployeeProvider;

    public LeaveRequestsController(IMediator mediator, ICurrentEmployeeProvider currentEmployeeProvider)
    {
        _mediator = mediator;
        _currentEmployeeProvider = currentEmployeeProvider;
    }

    [HttpGet]
    [Authorize(Roles = ManagerAndHrAdministratorRoles)]
    public async Task<IActionResult> GetLeaveRequests([FromQuery] string status, CancellationToken cancellationToken)
    {
        var employee = await _currentEmployeeProvider.GetCurrentEmployeeAsync(cancellationToken);
        if (employee == null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Authenticated user is not a registered employee.",
                Status = StatusCodes.Status403Forbidden,
                Type = "https://www.rfc-editor.org/rfc/rfc7807"
            });
        }

        var query = new GetPendingLeaveRequestsQuery
        {
            Status = status,
            CurrentEmployeeId = employee.Id,
            IsHrAdministrator = User.IsInRole(HrAdministratorRoleName)
        };

        var leaveRequests = await _mediator.Send(query, cancellationToken);
        return Ok(leaveRequests);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitLeaveRequest(SubmitLeaveRequestCommand command, CancellationToken cancellationToken)
    {
        var employee = await _currentEmployeeProvider.GetCurrentEmployeeAsync(cancellationToken);
        if (employee == null)
        {
            return Unauthorized();
        }

        command.EmployeeId = employee.Id;
        var leaveRequestId = await _mediator.Send(command, cancellationToken);
        return Ok(leaveRequestId);
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = ManagerAndHrAdministratorRoles)]
    public async Task<IActionResult> ApproveLeaveRequest(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _currentEmployeeProvider.GetCurrentEmployeeAsync(cancellationToken);
        if (employee == null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Authenticated user is not a registered employee.",
                Status = StatusCodes.Status403Forbidden,
                Type = "https://www.rfc-editor.org/rfc/rfc7807"
            });
        }

        var command = new ApproveLeaveRequestCommand { LeaveRequestId = id, ApproverId = employee.Id };
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = ManagerAndHrAdministratorRoles)]
    public async Task<IActionResult> RejectLeaveRequest(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _currentEmployeeProvider.GetCurrentEmployeeAsync(cancellationToken);
        if (employee == null)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Authenticated user is not a registered employee.",
                Status = StatusCodes.Status403Forbidden,
                Type = "https://www.rfc-editor.org/rfc/rfc7807"
            });
        }

        var command = new RejectLeaveRequestCommand { LeaveRequestId = id, RejectorId = employee.Id };
        await _mediator.Send(command, cancellationToken);
        return NoContent();
    }
}