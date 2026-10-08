using System.Security.Claims;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Features.LeaveRequests.Queries.GetDepartmentLeaveReport;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

/// <summary>Maps HR's authenticated reporting selection to Application without accepting an actor from client input.</summary>
[ApiController]
[Authorize(Policy = "HrAdministratorOnly")]
[Route("api/v1/reports/department-leave")]
public sealed class LeaveReportsController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns calendar-period totals by current department, preserving inactive Approved history.</summary>
    [HttpGet]
    public async Task<ActionResult<DepartmentLeaveReportDto>> Get([FromQuery] DateOnly start, [FromQuery] DateOnly end,
        [FromQuery] Guid? departmentId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            throw new ForbiddenException("A valid authenticated account is required.");
        return Ok(await mediator.Send(new GetDepartmentLeaveReportQuery(actor, start, end, departmentId), cancellationToken));
    }
}
