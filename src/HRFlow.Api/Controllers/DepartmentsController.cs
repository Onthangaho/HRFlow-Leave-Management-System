using HRFlow.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class DepartmentsController : ControllerBase
{
    private readonly IHRFlowDbContext _context;

    public DepartmentsController(IHRFlowDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetAllDepartments(CancellationToken cancellationToken)
    {
        var departments = await _context.Departments
            .Select(d => new { d.Id, d.Name })
            .ToListAsync(cancellationToken);
        return Ok(departments);
    }
}