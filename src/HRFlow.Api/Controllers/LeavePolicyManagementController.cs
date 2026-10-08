using System.Security.Claims;
using HRFlow.Application.Exceptions;
using HRFlow.Application.Features.LeaveConfiguration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

/// <summary>Maps HR policy routes to Application; live write authorization and rules remain inside protection.</summary>
[Authorize(Policy = "HrAdministratorOnly")]
[ApiController]
[Route("api/v1/management/leave-policies")]
public sealed class LeavePolicyManagementController(LeaveConfigurationService service) : ControllerBase
{
    /// <summary>Returns shared-rule snapshots with linked types and deletion consequences.</summary>
    [HttpGet]
    public async Task<IActionResult> List(CancellationToken token) => Ok(await service.GetPoliciesAsync(Actor(), null, token));

    /// <summary>Reloads one shared policy after an edit conflict.</summary>
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Detail(Guid id, CancellationToken token) => Ok((await service.GetPoliciesAsync(Actor(), id, token)).Single());

    /// <summary>Creates current rules without accepting an actor identifier from JSON.</summary>
    [HttpPost]
    public async Task<IActionResult> Create(PolicyInput input, CancellationToken token)
    {
        var result = await service.SavePolicyAsync(Actor(), null, null, input.Name, input.AllowOverlap, input.DefaultBalance, token);
        return CreatedAtAction(nameof(Detail), new { id = result.Id }, result);
    }

    /// <summary>Replaces shared current rules without modifying historical leave decisions.</summary>
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, PolicyUpdateInput input, CancellationToken token) =>
        Ok(await service.SavePolicyAsync(Actor(), id, input.ExpectedVersion, input.Name, input.AllowOverlap, input.DefaultBalance, token));

    /// <summary>Deletes only policies with no linked leave types and a matching version.</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, [FromQuery] Guid expectedVersion, CancellationToken token)
    {
        await service.DeletePolicyAsync(Actor(), id, expectedVersion, token);
        return NoContent();
    }

    private Guid Actor() => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
        ? id : throw new ForbiddenException("A valid authenticated account is required.");
}
