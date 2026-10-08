namespace HRFlow.Infrastructure.Services.Auth;
/// <summary>Private delivery boundary; normal API responses and logs never contain invitations.</summary>
public interface IActivationDelivery
{
    /// <summary>Fails closed when an approved delivery mechanism is unavailable.</summary>
    void EnsureConfigured();
    /// <summary>Writes the sensitive invitation only to the private delivery channel.</summary>
    Task DeliverAsync(Guid accountId, string email, Uri link, CancellationToken token);
}
