using HRFlow.Application.Features.Employees.Commands.CreateEmployee;
using HRFlow.Application.Features.Employees.Commands.UpdateEmployee;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

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

    [HttpPost]
    public async Task<IActionResult> CreateEmployee([FromBody] CreateEmployeeCommand command, CancellationToken cancellationToken)
    {
        var result = await _sender.Send(command, cancellationToken);
        return Created($"/api/v1/employees/{result.EmployeeId}", result);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateEmployee(Guid id, [FromBody] UpdateEmployeeCommand command, CancellationToken cancellationToken)
    {
        command.EmployeeId = id;
        var result = await _sender.Send(command, cancellationToken);
        return Ok(result);
    }
}