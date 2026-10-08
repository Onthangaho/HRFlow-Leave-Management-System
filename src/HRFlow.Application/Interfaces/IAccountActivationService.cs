namespace HRFlow.Application.Interfaces;
/// <summary>Establishes the first password only; never changes employment status or roles.</summary>
public interface IAccountActivationService
{
    /// <summary>Safe HR-only state projection, read inside the caller's existing snapshot.</summary>
    Task<ActivationStatus> GetStatusAsync(string? identityId, CancellationToken token);
    /// <summary>Rejects unsupported delivery before creating an undeliverable account.</summary>
    void EnsureDeliveryConfigured();
    /// <summary>Prepares an account-bound invitation inside the caller's reserved transaction.</summary>
    string Prepare(HRFlow.Domain.Entities.ApplicationUser user);
    /// <summary>Delivers after commit, then records a safe delivery state without returning the token.</summary>
    Task<string> DeliverAsync(Guid userId, string invitation, CancellationToken token);
    /// <summary>Explicitly replaces the invitation after rechecking HR and target state under protection.</summary>
    Task<string> ResendAsync(Guid actorId, Guid employeeId, Guid expectedVersion, CancellationToken token);
    /// <summary>Consumes a verified invitation and establishes the first password atomically.</summary>
    Task RedeemAsync(string invitation, string password, CancellationToken token);
}

/// <summary>Contains no token, hash, password or private delivery path.</summary>
public sealed record ActivationStatus(bool RequiresActivation, string DeliveryState, DateTime? ActivatedAtUtc);
