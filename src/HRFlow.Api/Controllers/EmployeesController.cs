using HRFlow.Application.Features.Employees.Queries.GetEmployees;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

using Microsoft.AspNetCore.Authorization;

[Authorize]
[ApiController]
[Route("api/v1/employees")]
public class EmployeesController : ControllerBase
{
    private readonly IMediator _mediator;

    public EmployeesController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet]
    public async Task<IActionResult> GetEmployees()
    {
        var employees = await _mediator.Send(new GetEmployeesQuery());
        return Ok(employees);
    }
}