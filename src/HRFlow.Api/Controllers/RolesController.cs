using System.Security.Claims;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

/// <summary>Delegates protected reference-data reads and current permission checks to Application.</summary>
[ApiController]
[Authorize(Policy = "HrAdministratorOnly")]
[Route("api/v1/[controller]")]
public sealed class RolesController(ReferenceDataService service) : ControllerBase
{
    /// <summary>Preserves the client selector contract without querying persistence in the controller.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAllRoles(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor))
            throw new ForbiddenException("A valid authenticated account is required.");
        return Ok(await service.GetRolesAsync(actor, cancellationToken));
    }
}
