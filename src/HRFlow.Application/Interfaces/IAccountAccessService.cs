namespace HRFlow.Application.Interfaces;

/// <summary>Checks persisted lifecycle without exposing Identity to the API.</summary>
public interface IAccountAccessService
{
    /// <summary>Validates both lifecycle and the presented credential generation against persisted Identity state.</summary>
    Task<bool> IsCurrentAsync(string? identityUserId, string? version, CancellationToken cancellationToken);
    /// <summary>Requires a linked active profile and activated Identity account for anonymous token issuance.</summary>
    Task<bool> IsActiveAsync(string? identityUserId, CancellationToken cancellationToken);
}
