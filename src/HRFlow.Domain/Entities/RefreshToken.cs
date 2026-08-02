namespace HRFlow.Domain.Entities;

/// <summary>
/// Represents a server-side refresh token record so token rotation can invalidate old tokens reliably.
/// </summary>
public class RefreshToken
{
    /// <summary>
    /// Gets or sets the unique identifier of this refresh token record.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the Identity user id that owns this refresh token.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the one-way hash of the refresh token value using server-side pepper.
    /// The raw token is only held by the client and submitted for redemption; the server stores only the hash.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets when the token was created in UTC.
    /// </summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the token expires in UTC.
    /// </summary>
    public DateTime ExpiresAtUtc { get; set; }

    /// <summary>
    /// Gets or sets when the token was revoked in UTC, if rotation or invalidation has occurred.
    /// </summary>
    public DateTime? RevokedAtUtc { get; set; }
}