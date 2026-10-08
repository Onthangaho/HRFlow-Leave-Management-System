namespace HRFlow.Application.Interfaces;
/// <summary>Captures authenticated proof for rechecking inside database protection, independent of HTTP.</summary>
public interface IInitiatingCredential
{
    /// <summary>Null denotes an anonymous operation such as login or activation.</summary>
    string? IdentityUserId { get; }
    /// <summary>The credential generation presented by the initiating bearer token.</summary>
    string? Version { get; }
}
/// <summary>Rechecks the initiating proof after acquiring a snapshot or writer reservation.</summary>
public interface IRequestCredentialValidator
{
    /// <summary>Rejects revoked authenticated proof; anonymous token-authorised workflows retain their own checks.</summary>
    Task RequireCurrentAsync(CancellationToken token);
}
/// <summary>Stable token contract independent of ASP.NET and Identity internals.</summary>
public static class CredentialClaims
{
    /// <summary>Account generation rotated when credentials change, not on ordinary refresh.</summary>
    public const string Version = "credential_version";
}
