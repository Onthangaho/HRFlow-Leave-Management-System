namespace HRFlow.Domain.Models.Auth;
/// <summary>Stable safe contract values; delivery never implies employment or role eligibility.</summary>
public static class ActivationDeliveryStates
{
    /// <summary>Existing account eligibility is preserved without creating an invitation.</summary>
    public const string NotRequired = "NotRequired";
    /// <summary>Account commit succeeded; delivery or its final acknowledgement is outstanding.</summary>
    public const string PendingDelivery = "PendingDelivery";
    /// <summary>Private Development pickup succeeded; this is not an email-delivery claim.</summary>
    public const string PickupReady = "PickupReady";
    /// <summary>Explicit resend is required after delivery failure or recipient change.</summary>
    public const string DeliveryFailed = "DeliveryFailed";
    /// <summary>First password was established atomically with invitation consumption.</summary>
    public const string Activated = "Activated";
    /// <summary>Computed read state; expired invitations are never accepted or extended implicitly.</summary>
    public const string Expired = "Expired";
    /// <summary>No linked Identity exists; management must not silently manufacture one.</summary>
    public const string Unavailable = "Unavailable";
}
