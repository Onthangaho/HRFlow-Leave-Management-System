using HRFlow.Domain.Models.Auth;
using Microsoft.AspNetCore.Identity;

namespace HRFlow.Domain.Entities
{
    /// <summary>Preserves Identity credentials while separating first-password activation from employment lifecycle.</summary>
    public class ApplicationUser : IdentityUser<Guid>
    {
        /// <summary>Only newly provisioned accounts require activation; legacy eligibility is preserved.</summary>
        public bool RequiresActivation { get; set; }
        /// <summary>Persisted bearer generation; password changes revoke every previously issued generation.</summary>
        public Guid CredentialVersion { get; set; } = Guid.NewGuid();
        /// <summary>Purpose-bound opaque invitation hash; the raw secret exists only in private delivery.</summary>
        public string? ActivationTokenHash { get; set; }
        /// <summary>Exclusive UTC expiry; no invitation is accepted at or after this instant.</summary>
        public DateTime? ActivationExpiresAtUtc { get; set; }
        /// <summary>Identity-normalized address captured for this hash generation; legacy invitations remain null.</summary>
        public string? ActivationRecipientNormalizedEmail { get; set; }
        /// <summary>Durable single-dispatch claim; restart never reconstructs or automatically resends a secret.</summary>
        public DateTime? ActivationDispatchStartedAtUtc { get; set; }
        /// <summary>Legacy dates remain null instead of inventing an activation event.</summary>
        public DateTime? ActivatedAtUtc { get; set; }
        /// <summary>Safe delivery feedback independent from active employment and role membership.</summary>
        public string InvitationDeliveryState { get; set; } = ActivationDeliveryStates.NotRequired;
    }
}
