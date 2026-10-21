/** Safe delivery feedback is independent from a committed account or employment eligibility. */
export function activationFeedback(state?: string | null): string {
  switch (state) {
    case 'PickupReady': return 'Private development invitation is ready for pickup. This is not email delivery.';
    case 'Accepted': return 'Transport accepted the invitation. This does not confirm mailbox delivery.';
    case 'DeliveryFailed': return 'Invitation delivery failed. Refresh status, then explicitly resend to rotate the invitation.';
    case 'Activated': return 'Account activated. No invitation needs to be resent.';
    case 'Expired': return 'Invitation expired. Refresh status, then explicitly resend.';
    case 'Unavailable': return 'Invitation is unavailable for the current account state. Refresh employee status.';
    default: return 'Delivery is unconfirmed or pending. Refresh status before explicitly resending; resend invalidates older links.';
  }
}
