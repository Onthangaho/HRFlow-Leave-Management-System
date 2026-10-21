# Activation delivery coordination (#79 partial slice)

## Operational boundary

21 October 2026: reusable coordination and private local SMTP tests only. ADR 0029 D1-D5 remain
Pending/Unassigned. No approved provider, SMTP authentication adapter, external sending or production
onboarding is implemented. Production configuration still rejects Development PrivatePickup.
Development retains its private pickup mechanism; pickup is not email.

## State and transaction contract

Manual employee creation and CSV confirmation share employee creation and commit a passwordless,
Pending activation account before delivery. Prepare records the purpose-bound SHA-256 token hash,
normalized recipient and UTC expiry under the existing SQLite writer reservation. The hash identifies
the invitation generation. Only the raw value in the current call can construct its link; no message,
link or recoverable token is stored in a database queue. Development pickup deliberately writes the
private invitation outside public/repository storage, under its existing permissions.

Delivery claims the generation once under a writer reservation, with current HR and initiating
credential checks. A deferred snapshot rechecks recipient/generation/active Pending eligibility before
network work. Network work never holds the writer reservation. Activation:DeliveryTimeoutSeconds
is bounded 1-30 seconds, default 15; one adapter attempt, no automatic retry. Existing SQLite write
contention timeout is separate. The adapter must honour cancellation; timing out cannot recall already
accepted mail. An edit after the final eligibility read can race dispatch, but cannot authorize redemption
of an invalidated link or overwrite the current invitation acknowledgement.

Acknowledgement rechecks current credentials/HR, generation, normalized address, active employment,
Pending/passwordless state and expiry under writer protection. Superseded acknowledgements return
current persisted status, not the old transport result. Ordinary profile edits do not invalidate a token.
Email changes invalidate Pending invitations; explicit versioned resend rotates the hash. Two processes
using the same employee version cannot both resend. Activated roles are never restored or granted.

- PendingDelivery: committed but not confirmed. A dispatch claim can survive interruption. It is not
  automatically retried on restart, because transport acceptance is unknowable.
- PickupReady: private Development pickup succeeded, not email delivery.
- Accepted: positive transport acceptance only, never a promise of mailbox delivery.
- DeliveryFailed: definite rejection/invalid recipient. The account remains committed.
- DeliveryUnknown: timeout/disconnect/ambiguous result. Refresh before an explicit rotated resend.
- Activated, Expired, Unavailable: current account/expiry/lifecycle takes precedence.

An acknowledgement persistence failure returns authorized persisted status when readable; contention,
revocation or a cancelled client may instead produce an error. Never infer creation rollback from a
lost response. Refresh the directory/CSV batch; Created CSV outcomes remain durable and cannot become
creatable again. CSV refresh reads current invitation feedback rather than replaying stale row feedback.
HR has explicit refresh/resend controls; errors preserve creation drafts and warn against blind retries.
The failed-delivery attention filter includes uncertain outcomes; it is not a mailbox-bounce monitor.

## Interruption and recovery

Crash before dispatch can leave a claimed generation PendingDelivery. Crash after acceptance can leave
the same status. No startup worker reconstructs mail or sends restored events. Recovery is explicit
versioned resend after current authorized review, potentially producing a stale second email. GET/prefetch
never establishes a password; only anonymous token-authorized POST does so atomically and once.
Encrypted restore clears hashes, expiry, dispatch and recipient metadata alongside existing credential
invalidation. Original roles/lifecycle remain preserved; operator authorized recovery remains separate.

Migration AddInvitationDispatchProtection adds two nullable Identity columns, without inferred backfill.
Legacy invitation hashes/expiry remain unchanged and can redeem normally; old dispatch history cannot
be reconstructed. Use explicit resend to create a recipient-aware generation. Rollback drops this metadata
and loses dispatch protection: stop writers, back up, and invalidate outstanding invitations through an
approved operation before using an older binary. Never replay callbacks automatically after rollback.

## Local verification fixture

[PrivateActivationSmtp](../../tests/HRFlow.RegressionTests/PrivateActivationSmtp.cs) lives only in the test
assembly: an ephemeral loopback listener, fixed loopback sender and synthetic.invalid recipients.
It has no configurable relay or forwarding. Built-in MailMessage formats multipart UTF-8 MIME; tokens
and message content stay in private memory and never enter reporter output. Plain local SMTP is a test
fixture, not an approved authentication/TLS implementation. Bounded rejection, disconnect, timeout and
explicit barriers test coordinator outcomes. A positive sink acknowledgement is required: the local
SmtpClient can complete at EOF after DATA, which alone is not proof of acceptance.

Run backend fixtures sequentially with other API/browser fixtures stopped, then run browser gates:

~~~powershell
dotnet build HRFlow.sln
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~ActivationDeliveryTests
dotnet test tests/HRFlow.RegressionTests --no-build
npm.cmd --prefix tests/browser test
~~~

[Actual runs and limitations](../verification/activation-delivery-coordination.md) distinguish real HTTP,
controlled browser response loss and local SMTP from unimplemented provider verification. Do not set
production SMTP credentials: no supported production adapter exists in this slice.
