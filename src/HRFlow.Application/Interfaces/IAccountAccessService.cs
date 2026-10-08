namespace HRFlow.Application.Interfaces;

/// <summary>Checks persisted lifecycle without exposing Identity to the API.</summary>
public interface IAccountAccessService
{
    /// <summary>Requires an existing account linked to an active profile and activated account; JWT claims alone cannot grant access.</summary>
    Task<bool> IsActiveAsync(string? identityUserId, CancellationToken cancellationToken);
}
