using HRFlow.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;
using HRFlow.Application.Features.LeaveRequests.Commands.CancelLeaveRequest;
using HRFlow.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;
using HRFlow.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;
using HRFlow.Application.Features.LeaveRequests.Queries.GetPendingLeaveRequests;
using HRFlow.Application.Features.LeaveRequests.Queries.GetLeaveBalances;
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
    private readonly ILogger<LeaveRequestsController> _logger;

    public LeaveRequestsController(
        IMediator mediator,
        ICurrentEmployeeProvider currentEmployeeProvider,
        ILogger<LeaveRequestsController> logger)
    {
        _mediator = mediator;
        _currentEmployeeProvider = currentEmployeeProvider;
        _logger = logger;
    }

    [HttpGet]
    [Authorize(Roles = ManagerAndHrAdministratorRoles)]
    public async Task<IActionResult> GetLeaveRequests([FromQuery] string status, CancellationToken cancellationToken)
    {
        var employee = await _currentEmployeeProvider.GetCurrentEmployeeAsync(cancellationToken);
        if (employee == null)
        {
            _logger.LogWarning("Leave request queue access was denied because the caller has no employee record.");
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
        _logger.LogInformation(
            "Pending leave request queue returned {RequestCount} request(s) for EmployeeId: {EmployeeId}; IsHrAdministrator: {IsHrAdministrator}",
            leaveRequests.Count,
            employee.Id,
            query.IsHrAdministrator);
        return Ok(leaveRequests);
    }

    [HttpPost]
    public async Task<IActionResult> SubmitLeaveRequest(SubmitLeaveRequestCommand command, CancellationToken cancellationToken)
    {
        var employee = await _currentEmployeeProvider.GetCurrentEmployeeAsync(cancellationToken);
        if (employee == null)
        {
            _logger.LogWarning("Leave request submission was denied because the caller has no employee record.");
            return Unauthorized();
        }

        command.EmployeeId = employee.Id;
        var leaveRequestId = await _mediator.Send(command, cancellationToken);
        _logger.LogInformation(
            "Leave request submitted. LeaveRequestId: {LeaveRequestId}; EmployeeId: {EmployeeId}; LeaveTypeId: {LeaveTypeId}; StartDate: {StartDate}; EndDate: {EndDate}",
            leaveRequestId,
            employee.Id,
            command.LeaveTypeId,
            command.StartDate,
            command.EndDate);
        return Ok(leaveRequestId);
    }

    /// <summary>
    /// Returns the authenticated employee's balances derived from leave policy entitlement and
    /// approved request history rather than a persisted balance field.
    /// </summary>
    [HttpGet("balances")]
    public async Task<IActionResult> GetLeaveBalances(CancellationToken cancellationToken)
    {
        var employee = await _currentEmployeeProvider.GetCurrentEmployeeAsync(cancellationToken);
        if (employee == null)
        {
            _logger.LogWarning("Leave balance access was denied because the caller has no employee record.");
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Authenticated user is not a registered employee.",
                Status = StatusCodes.Status403Forbidden,
                Type = "https://www.rfc-editor.org/rfc/rfc7807"
            });
        }

        var balances = await _mediator.Send(
            new GetLeaveBalancesQuery { EmployeeId = employee.Id },
            cancellationToken);
        _logger.LogInformation(
            "Leave balances returned for EmployeeId: {EmployeeId}; LeaveTypeCount: {LeaveTypeCount}",
            employee.Id,
            balances.Count);
        return Ok(balances);
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = ManagerAndHrAdministratorRoles)]
    public async Task<IActionResult> ApproveLeaveRequest(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _currentEmployeeProvider.GetCurrentEmployeeAsync(cancellationToken);
        if (employee == null)
        {
            _logger.LogWarning(
                "Leave request approval was denied because the caller has no employee record. LeaveRequestId: {LeaveRequestId}",
                id);
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
        _logger.LogInformation(
            "Leave request approved. LeaveRequestId: {LeaveRequestId}; ApproverEmployeeId: {ApproverEmployeeId}",
            id,
            employee.Id);
        return NoContent();
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = ManagerAndHrAdministratorRoles)]
    public async Task<IActionResult> RejectLeaveRequest(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _currentEmployeeProvider.GetCurrentEmployeeAsync(cancellationToken);
        if (employee == null)
        {
            _logger.LogWarning(
                "Leave request rejection was denied because the caller has no employee record. LeaveRequestId: {LeaveRequestId}",
                id);
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
        _logger.LogInformation(
            "Leave request rejected. LeaveRequestId: {LeaveRequestId}; RejectorEmployeeId: {RejectorEmployeeId}",
            id,
            employee.Id);
        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    public async Task<IActionResult> CancelLeaveRequest(Guid id, CancellationToken cancellationToken)
    {
        var employee = await _currentEmployeeProvider.GetCurrentEmployeeAsync(cancellationToken);
        if (employee == null)
        {
            _logger.LogWarning(
                "Leave request cancellation was denied because the caller has no employee record. LeaveRequestId: {LeaveRequestId}",
                id);
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Authenticated user is not a registered employee.",
                Status = StatusCodes.Status403Forbidden,
                Type = "https://www.rfc-editor.org/rfc/rfc7807"
            });
        }

        var command = new CancelLeaveRequestCommand
        {
            LeaveRequestId = id,
            EmployeeId = employee.Id
        };

        await _mediator.Send(command, cancellationToken);
        _logger.LogInformation(
            "Leave request cancelled. LeaveRequestId: {LeaveRequestId}; EmployeeId: {EmployeeId}",
            id,
            employee.Id);
        return NoContent();
    }
}