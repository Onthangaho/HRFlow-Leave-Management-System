namespace HRFlow.Infrastructure.Services.Auth;
/// <summary>Safe acknowledgement categories; transports never return payloads, addresses or links.</summary>
public enum ActivationDeliveryOutcome { PickupReady, Accepted, Failed, Unknown }
/// <summary>Private delivery boundary; implementations honour cancellation and never retry a submission.</summary>
public interface IActivationDelivery
{
    /// <summary>Fails closed when an approved delivery mechanism is unavailable; never probes a recipient.</summary>
    void EnsureConfigured();
    /// <summary>Submits once outside database protection; acceptance is not proof of mailbox delivery.</summary>
    Task<ActivationDeliveryOutcome> DeliverAsync(Guid accountId, string email, Uri link, CancellationToken token);
}
