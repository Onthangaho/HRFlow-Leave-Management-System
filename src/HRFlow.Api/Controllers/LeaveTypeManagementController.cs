using System.Security.Claims;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Features.LeaveConfiguration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

/// <summary>Exposes HR type administration separately from the authenticated employee selector.</summary>
[Authorize(Policy = "HrAdministratorOnly")]
[ApiController]
[Route("api/v1/management/leave-types")]
public sealed class LeaveTypeManagementController(LeaveConfigurationService service) : ControllerBase
{
    /// <summary>Returns edit snapshots and historical reference counts for future HR forms.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken token) => Ok(await service.GetTypesAsync(null, token));

    /// <summary>Reloads one type after an edit conflict.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken token) => Ok((await service.GetTypesAsync(id, token)).Single());

    /// <summary>Creates a type with its explicit policy; the acting account comes only from authentication.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(LeaveTypeInput input, CancellationToken token)
    {
        var result = await service.SaveTypeAsync(Actor(), null, null, input.Name, input.LeavePolicyId, token);
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }

    /// <summary>Replaces current name and policy assignment using a caller-supplied edit version.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, LeaveTypeUpdateInput input, CancellationToken token) =>
        Ok(await service.SaveTypeAsync(Actor(), id, input.ExpectedVersion, input.Name, input.LeavePolicyId, token));

    /// <summary>Deletes an unused type only when its version still matches the caller's snapshot.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid expectedVersion, CancellationToken token)
    {
        await service.DeleteTypeAsync(Actor(), id, expectedVersion, token);
        return NoContent();
    }

    private Guid Actor() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : throw new ForbiddenException("A valid authenticated account is required.");
}
