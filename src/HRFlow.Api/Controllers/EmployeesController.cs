using HRFlow.Application.Features.Employees.Commands.CreateEmployee;
using HRFlow.Application.Features.Employees.Commands.UpdateEmployee;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using HRFlow.Application.Features.Employees.Queries.GetAllEmployees;

namespace HRFlow.Api.Controllers;

/// <summary>
/// Manages employee resources with HR Administrator-only authorization boundary.
/// All endpoints require the authenticated user to be in the 'HR Administrator' role.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "HrAdministratorOnly")]
public class EmployeesController : ControllerBase
{
    private readonly ISender _sender;

    public EmployeesController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Gets a list of all employees.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAllEmployees(CancellationToken cancellationToken)
    {
        var employees = await _sender.Send(new GetAllEmployeesQuery(), cancellationToken);
        return Ok(employees);
    }

    /// <summary>
    /// Creates a new employee record.
    /// Returns 201 Created with the employee representation and Location header pointing to the new resource.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Created($"/api/v1/employees/{result.EmployeeId}", result);
    }

    /// <summary>
    /// Updates an existing employee's information.
    /// The route id parameter takes precedence and overwrites any id provided in the request body.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeCommand command, CancellationToken cancellationToken)
    {
        command.EmployeeId = id;
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }
}