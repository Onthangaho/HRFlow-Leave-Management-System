using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class RolesController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager;

    public RolesController(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    [HttpGet]
    [Authorize(Roles = "HR Administrator")]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _roleManager.Roles.Select(r => new { r.Name }).ToListAsync();
        return Ok(roles);
    }
}