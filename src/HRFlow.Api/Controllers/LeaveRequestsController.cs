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
    /// <summary>Checks only a previously keyed own-account attempt; missing outcomes never create requests.</summary>
    [HttpPost("submission-outcome"), Authorize(Roles = PersonalLeaveRoles)]
    [RequestSizeLimit(16384)]
    public async Task<IActionResult> SubmissionOutcome(SubmitLeaveRequestCommand command, CancellationToken cancellationToken)
    {
        var employee = await _currentEmployeeProvider.GetCurrentEmployeeAsync(cancellationToken);
        if (employee == null) return Unauthorized();
        command.EmployeeId = employee.Id;
        Response.Headers.CacheControl = "no-store";
        return Ok(new { RequestId = await _mediator.Send(new CheckLeaveSubmissionOutcomeQuery(command), cancellationToken) });
    }

    /// <summary>Assesses only the authenticated owner's dates and current policy; never creates or binds anything.</summary>
    [HttpPost("preview"), Authorize(Roles = PersonalLeaveRoles)]
    [RequestSizeLimit(2048)]
    public async Task<IActionResult> Preview(
        HRFlow.Application.Features.LeaveRequests.Queries.PreviewLeaveRequest.LeavePreviewInput input,
        CancellationToken token)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            throw new ForbiddenException("A valid authenticated account is required.");
        Response.Headers.CacheControl = "no-store";
        return Ok(await _mediator.Send(new HRFlow.Application.Features.LeaveRequests.Queries.PreviewLeaveRequest.PreviewLeaveRequestQuery(actor, input), token));
    }

    /// <summary>Returns read-only facts under live owner/manager/HR authorization in one snapshot.</summary>
    [HttpGet("{id:guid}/timeline")]
    public async Task<ActionResult<LeaveRequestTimelineDto>> GetTimeline(Guid id, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            throw new ForbiddenException("A valid authenticated account is required.");
        return Ok(await _mediator.Send(new GetLeaveRequestTimelineQuery(actor, id), cancellationToken));
    }

    /// <summary>Returns only the authenticated employee's current-model dashboard from one live authorization/data snapshot.</summary>
    [HttpGet("dashboard"), Authorize(Roles = PersonalLeaveRoles)]
    public async Task<IActionResult> GetDashboard(CancellationToken token)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            throw new ForbiddenException("A valid authenticated account is required.");
        return Ok(await _mediator.Send(new HRFlow.Application.Features.LeaveRequests.Queries.GetPersonalDashboard.GetPersonalDashboardQuery(actor), token));
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

    /// <summary>Returns a bounded current Manager queue; selected IDs only narrow authenticated reporting scope.</summary>
    [HttpGet]
    [Authorize(Roles = ManagerRoleName)]
    public async Task<ActionResult<ManagerQueuePage>> GetLeaveRequests([FromQuery] ManagerQueueFilter filter, CancellationToken cancellationToken)
    {
        var actor = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        Response.Headers.CacheControl = "no-store";
        var result = await _mediator.Send(new GetManagerQueueQuery(actor, filter), cancellationToken);
        _logger.LogInformation("Manager queue returned; ActorId: {ActorId}; MatchingCount: {MatchingCount}; RowCount: {RowCount}", actor, result.TotalCount, result.Items.Count);
        return Ok(result);
    }

    /// <summary>Searches current eligible Pending owners without exposing an employee-directory endpoint.</summary>
    [HttpGet("queue-people")]
    [Authorize(Roles = ManagerRoleName)]
    public async Task<ActionResult<IReadOnlyList<QueuePerson>>> GetQueuePeople([FromQuery] string? search, CancellationToken cancellationToken)
    {
        Response.Headers.CacheControl = "no-store";
        return Ok(await _mediator.Send(new GetQueuePeopleQuery(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!), search), cancellationToken));
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
            "Leave submission outcome resolved. LeaveRequestId: {LeaveRequestId}; EmployeeId: {EmployeeId}",
            leaveRequestId,
            employee.Id);
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
    public async Task<ActionResult<LeaveHistoryPage>> GetLeaveRequestHistory([FromQuery] LeaveHistoryFilter filter, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            throw new ForbiddenException("A valid authenticated account is required.");
        Response.Headers.CacheControl = "no-store";
        var result = await _mediator.Send(new GetEmployeeLeaveHistoryQuery { ActorIdentityId = actor, Filter = filter }, cancellationToken);
        _logger.LogInformation("Personal history returned; ActorId: {ActorId}; MatchingCount: {MatchingCount}; Page: {Page}; RowCount: {RowCount}",
            actor, result.TotalCount, result.Page, result.Items.Count);
        return Ok(result);
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
