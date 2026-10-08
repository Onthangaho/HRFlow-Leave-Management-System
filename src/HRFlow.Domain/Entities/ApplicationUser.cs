using HRFlow.Domain.Models.Auth;
using Microsoft.AspNetCore.Identity;

namespace HRFlow.Domain.Entities
{
    /// <summary>Preserves Identity credentials while separating first-password activation from employment lifecycle.</summary>
    public class ApplicationUser : IdentityUser<Guid>
    {
        /// <summary>Only newly provisioned accounts require activation; legacy eligibility is preserved.</summary>
        public bool RequiresActivation { get; set; }
        /// <summary>Purpose-bound opaque invitation hash; the raw secret exists only in private delivery.</summary>
        public string? ActivationTokenHash { get; set; }
        /// <summary>Exclusive UTC expiry; no invitation is accepted at or after this instant.</summary>
        public DateTime? ActivationExpiresAtUtc { get; set; }
        /// <summary>Legacy dates remain null instead of inventing an activation event.</summary>
        public DateTime? ActivatedAtUtc { get; set; }
        /// <summary>Safe delivery feedback independent from active employment and role membership.</summary>
        public string InvitationDeliveryState { get; set; } = ActivationDeliveryStates.NotRequired;
    }
}
