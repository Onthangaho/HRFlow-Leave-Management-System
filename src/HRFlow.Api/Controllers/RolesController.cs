using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HRFlow.Api.Controllers;

/// <summary>
/// Provides endpoints for managing and retrieving ASP.NET Core Identity roles.
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class RolesController : ControllerBase
{
    private readonly RoleManager<IdentityRole> _roleManager;

    /// <summary>
    /// Initializes a new instance of the <see cref="RolesController"/> class.
    /// </summary>
    /// <param name="roleManager">The role manager for querying Identity roles.</param>
    public RolesController(RoleManager<IdentityRole> roleManager)
    {
        _roleManager = roleManager;
    }

    /// <summary>
    /// Retrieves all available roles in the system.
    /// </summary>
    /// <returns>A list of all role names.</returns>
    /// <remarks>
    /// This endpoint is restricted to users with the HR Administrator role.
    /// </remarks>
    [HttpGet]
    [Authorize(Roles = "HR Administrator")]
    public async Task<IActionResult> GetAllRoles()
    {
        var roles = await _roleManager.Roles.Select(r => new { r.Name }).ToListAsync();
        return Ok(roles);
    }
}