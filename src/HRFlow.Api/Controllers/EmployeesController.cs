using System.Security.Claims;
using HRFlow.Application.Features.Employees.Commands.DeactivateEmployee;
using FluentValidation;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Features.Employees.Commands.CreateEmployee;
using HRFlow.Application.Features.Employees.Commands.UpdateEmployee;
using HRFlow.Application.Features.Employees.Queries.GetEmployees;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

/// <summary>Maps HR management routes to application commands; live write authorization belongs to the transactional service.</summary>
[Authorize(Policy = "HrAdministratorOnly")]
[ApiController]
[Route("api/v1/employees")]
public sealed class EmployeesController(IMediator mediator) : ControllerBase
{
    /// <summary>Returns complete edit snapshots for the HR directory and reporting selectors.</summary>
    [HttpGet]
    public async Task<IActionResult> GetEmployees(CancellationToken cancellationToken) =>
        Ok(await mediator.Send(new GetEmployeesQuery(), cancellationToken));

    /// <summary>Allows clients to reload a resource after a stale-edit conflict.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetEmployee(Guid id, CancellationToken cancellationToken)
    {
        var employee = (await mediator.Send(new GetEmployeesQuery { EmployeeId = id }, cancellationToken)).SingleOrDefault();
        return employee is null ? throw new NotFoundException("Employee was not found.") : Ok(employee);
    }

    /// <summary>Creates the profile and linked login account through the authorized application command.</summary>
    [HttpPost]
    public async Task<IActionResult> CreateEmployee(CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        command.ActorIdentityUserId = GetActorIdentityId();
        var result = await mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetEmployee), new { id = result.EmployeeId }, result);
    }

    /// <summary>Uses the route as the update target and rejects any contradictory body identifier.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateEmployee(Guid id, UpdateEmployeeCommand command, CancellationToken cancellationToken)
    {
        if (command.EmployeeId.HasValue && command.EmployeeId != id)
        {
            throw new ValidationException("EmployeeId must match the route.");
        }
        command.EmployeeId = id;
        command.ActorIdentityUserId = GetActorIdentityId();
        return Ok(await mediator.Send(command, cancellationToken));
    }

    /// <summary>Ends access and cancels pending leave in one versioned operation without deleting history.</summary>
    [HttpPatch("{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateEmployee(Guid id, DeactivateEmployeeCommand command, CancellationToken cancellationToken)
    {
        command.EmployeeId = id;
        command.ActorIdentityUserId = GetActorIdentityId();
        return Ok(await mediator.Send(command, cancellationToken));
    }

    private Guid GetActorIdentityId() =>
        Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var identityId)
            ? identityId : throw new ForbiddenException("A valid authenticated account is required.");
}
