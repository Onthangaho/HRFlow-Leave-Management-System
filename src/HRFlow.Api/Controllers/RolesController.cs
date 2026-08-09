using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class RolesController : ControllerBase
{
    [HttpGet]
    public IActionResult GetAllRoles()
    {
        var roles = new[]
        {
            new { Name = "Employee" },
            new { Name = "HR Administrator" }
        };
        return Ok(roles);
    }
}