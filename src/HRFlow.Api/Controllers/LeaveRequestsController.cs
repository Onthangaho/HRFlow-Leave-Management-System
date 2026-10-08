using HRFlow.Application.Features.LeaveRequests.Commands.ApproveLeaveRequest;
using HRFlow.Application.Features.LeaveRequests.Commands.CancelLeaveRequest;
using HRFlow.Application.Features.LeaveRequests.Commands.RejectLeaveRequest;
using HRFlow.Application.Features.LeaveRequests.Commands.SubmitLeaveRequest;
using HRFlow.Application.Features.LeaveRequests.Queries.GetEmployeeLeaveHistory;
using HRFlow.Application.Features.LeaveRequests.Queries.GetPendingLeaveRequests;
using HRFlow.Application.Features.LeaveRequests.Queries.GetLeaveBalances;
using HRFlow.Application.Interfaces;
using MediatR;
using System.Security.Claims;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Features.LeaveRequests.Queries.GetLeaveRequestTimeline;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using HRFlow.Application.Features.LeaveRequests;

namespace HRFlow.Api.Controllers;

[Authorize]
[ApiController]
[Route("api/v1/leave-requests")]
public class LeaveRequestsController : ControllerBase
{
    /// <summary>Returns read-only facts under live owner/manager/HR authorization in one snapshot.</summary>
    [HttpGet("{id:guid}/timeline")]
    public async Task<ActionResult<LeaveRequestTimelineDto>> GetTimeline(Guid id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            throw new ForbiddenException("A valid authenticated account is required.");
        return Ok(await _mediator.Send(new GetLeaveRequestTimelineQuery(actor, id), cancellationToken));
    }

    private const string HrAdministratorOnlyPolicy = "HrAdministratorOnly";
    private const string ManagerRoleName = "Manager";
    private const string PersonalLeaveRoles = "Employee,Manager";
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
    [Authorize(Roles = ManagerRoleName)]
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
            ActorIdentityId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
        };

        var leaveRequests = await _mediator.Send(query, cancellationToken);
        _logger.LogInformation(
            "Manager pending leave request queue returned {RequestCount} request(s) for EmployeeId: {EmployeeId}",
            leaveRequests.Count,
            employee.Id);
        return Ok(leaveRequests);
    }

    /// <summary>
    /// Returns a read-only organisation-wide view of pending requests for HR monitoring.
    /// </summary>
    [HttpGet("monitoring/pending")]
    [Authorize(Policy = HrAdministratorOnlyPolicy)]
    public async Task<IActionResult> GetOrganisationPendingLeaveRequests(CancellationToken cancellationToken)
    {
        var leaveRequests = await _mediator.Send(
            new GetPendingLeaveRequestsQuery
            {
                Status = PendingStatus,
                IsOrganisationMonitoring = true,
                ActorIdentityId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!)
            },
            cancellationToken);

        _logger.LogInformation(
            "HR pending leave monitoring returned {RequestCount} request(s).",
            leaveRequests.Count);
        return Ok(leaveRequests);
    }

    [HttpPost]
    [Authorize(Roles = PersonalLeaveRoles)]
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
    [Authorize(Roles = PersonalLeaveRoles)]
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

    /// <summary>
    /// Returns the authenticated employee's leave-request timeline and recorded lifecycle decisions.
    /// </summary>
    [HttpGet("history")]
    [Authorize(Roles = PersonalLeaveRoles)]
    public async Task<IActionResult> GetLeaveRequestHistory(CancellationToken cancellationToken)
    {
        var employee = await _currentEmployeeProvider.GetCurrentEmployeeAsync(cancellationToken);
        if (employee == null)
        {
            _logger.LogWarning("Leave request history access was denied because the caller has no employee record.");
            return StatusCode(StatusCodes.Status403Forbidden, new ProblemDetails
            {
                Title = "Forbidden",
                Detail = "Authenticated user is not a registered employee.",
                Status = StatusCodes.Status403Forbidden,
                Type = "https://www.rfc-editor.org/rfc/rfc7807"
            });
        }

        var leaveRequests = await _mediator.Send(
            new GetEmployeeLeaveHistoryQuery { EmployeeId = employee.Id },
            cancellationToken);
        _logger.LogInformation(
            "Leave request history returned for EmployeeId: {EmployeeId}; RequestCount: {RequestCount}",
            employee.Id,
            leaveRequests.Count);
        return Ok(leaveRequests);
    }

    [HttpPost("{id}/approve")]
    [Authorize(Roles = ManagerRoleName)]
    public async Task<IActionResult> ApproveLeaveRequest(Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LeaveDecisionDto? decision, CancellationToken cancellationToken)
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

        var command = new ApproveLeaveRequestCommand
        {
            LeaveRequestId = id,
            ApproverId = employee.Id,
            DecisionNote = decision?.DecisionNote
        };
        await _mediator.Send(command, cancellationToken);
        _logger.LogInformation(
            "Leave request approved. LeaveRequestId: {LeaveRequestId}; ApproverEmployeeId: {ApproverEmployeeId}; CorrelationId: {CorrelationId}",
            id,
            employee.Id,
            HttpContext.TraceIdentifier);
        return NoContent();
    }

    [HttpPost("{id}/reject")]
    [Authorize(Roles = ManagerRoleName)]
    public async Task<IActionResult> RejectLeaveRequest(Guid id, [FromBody(EmptyBodyBehavior = EmptyBodyBehavior.Allow)] LeaveDecisionDto? decision, CancellationToken cancellationToken)
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

        var command = new RejectLeaveRequestCommand
        {
            LeaveRequestId = id,
            RejectorId = employee.Id,
            DecisionNote = decision?.DecisionNote
        };
        await _mediator.Send(command, cancellationToken);
        _logger.LogInformation(
            "Leave request rejected. LeaveRequestId: {LeaveRequestId}; RejectorEmployeeId: {RejectorEmployeeId}; CorrelationId: {CorrelationId}",
            id,
            employee.Id,
            HttpContext.TraceIdentifier);
        return NoContent();
    }

    [HttpPost("{id}/cancel")]
    [Authorize(Roles = PersonalLeaveRoles)]
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