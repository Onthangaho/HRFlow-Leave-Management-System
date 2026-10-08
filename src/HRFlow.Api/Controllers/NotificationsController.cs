using System.Security.Claims;
using HRFlow.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace HRFlow.Api.Controllers;

/// <summary>Own-inbox transport only; current credential and recipient checks remain in protected services.</summary>
[Authorize]
[ApiController]
[Route("api/v1/notifications")]
public sealed class NotificationsController(ILeaveNotificationService notifications) : ControllerBase
{
    private Guid Actor => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>Returns a bounded recipient-scoped page with safe current-access links.</summary>
    [HttpGet]
    public async Task<ActionResult<NotificationPageDto>> List([FromQuery] string filter = "all", [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20, CancellationToken cancellationToken = default) =>
        Ok(await notifications.ListAsync(Actor, filter, page, pageSize, cancellationToken));

    /// <summary>Includes unread unavailable items until the recipient explicitly marks them read.</summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> Count(CancellationToken cancellationToken) =>
        Ok(new { unreadCount = await notifications.CountAsync(Actor, cancellationToken) });

    /// <summary>Idempotently updates only the authenticated recipient's read state.</summary>
    [HttpPatch("{id:guid}/read")]
    public async Task<IActionResult> Read(Guid id, CancellationToken cancellationToken)
    {
        await notifications.ReadAsync(Actor, id, cancellationToken);
        return NoContent();
    }
}
