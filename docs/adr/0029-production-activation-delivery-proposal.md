# ADR 0029: Production activation delivery proposal

Status: **Proposed, not approved or implemented**. Reviewed 20 October 2026 for open #79.
Base: merged PR #127, commit 40c2617577ebf4d661d6db1c596156426d18fe90.
No service connection, message sending, purchase, provider selection or production enablement.

## Recommendation and decisions requiring approval

Recommend one bounded synchronous authenticated SMTP adapter behind the existing Infrastructure
IActivationDelivery boundary, using an organisation-operated relay if its owner approves it. Require
TLS with certificate validation and a dedicated sending identity; no anonymous public relay, implicit
fallback, personal mailbox password or certificate bypass. Prefer OAuth where supported; the actual
organisation relay, authentication method, sender permissions and egress policy remain unapproved.
This is a transport architecture recommendation, not evidence that the owner has such a relay.
If no suitable relay exists, pause activation delivery: a transactional HTTPS API is a viable alternative,
not an automatic paid fallback. Implement only one approved production transport, not a framework of
providers. Keep Development private pickup separate.

Owner decisions, all **Unassigned / Pending**:

- D1: relay versus API, provider/account availability, budget and operational owner.
- D2: owned sender/domain, reply/help address, DNS controls and authorised recipient domains.
- D3: transport authentication, secret store/access/rotation and certificate ownership.
- D4: recipient and link processing, provider message retention, support access, data location,
  contractual/privacy review and deletion/legal-hold policy. TLS is not end-to-end secrecy: the relay,
  receiving mail system and recipient mailbox can read the activation link.
- D5: approve explicit rotated resend after uncertain outcomes; accept possible duplicate/stale emails.
- D6: release/deployment approval after private local tests and real provider/domain verification.
  No such approval is supplied by this document. #79 remains open.

## Existing source behaviour (inspection, not newly executed tests)

[AccountActivationService](../../src/HRFlow.Infrastructure/Services/Auth/AccountActivationService.cs)
is the shared coordinator. Prepare generates 32 cryptographically random bytes, hex encodes them,
stores SHA-256 of a purpose prefix plus the value, sets UTC expiry and PendingDelivery. Default
expiry is 1,440 minutes; configured range 5-10,080. The nullable unique hash is account-bound by its
Identity row. Only the in-memory caller holds the raw value; there is no recoverable-token outbox.

[Manual creation](../../src/HRFlow.Infrastructure/Services/Employees/EmployeeManagementService.cs)
checks delivery configuration before work, then current HR and shared creation validation inside
[SqliteWriteTransaction](../../src/HRFlow.Infrastructure/Services/SqliteWriteTransaction.cs). Its
non-deferred writer reservation precedes credential/validation reads; Identity, roles, profile,
employment facts and invitation hash commit together. Delivery runs after commit. Thus an error or
cancelled response after commit is not proof account creation failed; retrying creation is wrong.

[CSV confirmation](../../src/HRFlow.Infrastructure/Services/Employees/CsvOnboardingService.cs)
uses the same CreateInsideReservationAsync core per selected server-held row, adding schedule and
Created/Prepared outcome in that transaction. Delivery and a later actor-authorised batch-outcome
update are separate operations. A crash or HR revocation can leave batch delivery feedback behind
Identity state. Repeated confirmation skips Created rows; it does not recreate accounts or resend
lost invitations. HR must inspect the employee and use explicit versioned resend.

Resend rechecks current HR, target active/profile version, pending activation and absence of a password
inside the writer reservation. Prepare replaces the hash and expiry, and employee.Update rotates the
profile version, so concurrent resends using one original version cannot both commit. Delivery follows
commit. Pending recipient email changes clear hash/expiry and mark DeliveryFailed; HR must resend.
Deactivation does not erase the invitation hash; redemption checks active profile, so it is denied.

Current DeliverAsync reads recipient outside the writer reservation, invokes delivery, then enters a
second protected transaction. It updates the state only if RequiresActivation and the hash still match.
It does **not** currently guard recipient/version, target active status or expiry at acknowledgement;
it can return a local success state even if no matching state was updated. Only IOException and
UnauthorizedAccessException are converted to DeliveryFailed; network-provider exceptions are not a
completed contract. Cancellation or failed acknowledgement can leave PendingDelivery after delivery.
The scoped credential check can also reject acknowledgement after initiating-session revocation.
These are gaps to resolve, not claims of existing production support.

[IActivationDelivery](../../src/HRFlow.Infrastructure/Services/Auth/IActivationDelivery.cs) currently
returns no result. [DevelopmentActivationDelivery](../../src/HRFlow.Infrastructure/Services/Auth/DevelopmentActivationDelivery.cs)
only writes restricted local pickup, and success becomes PickupReady, never Sent. Startup
[DeploymentConfiguration](../../src/HRFlow.Api/Services/DeploymentConfiguration.cs) rejects any
nonempty Activation:Delivery outside Development; DI registers only pickup. Consequently production
manual creation, CSV confirmation and resend remain gated, rather than silently losing invitations.

Redeem uses the protected transaction to match hash, pending/passwordless state, UTC expiry and active
profile; Identity AddPasswordAsync and hash consumption/Activated commit atomically. Validation failure
rolls back; repeated redemption cannot overwrite a password or grant roles. The
[controller](../../src/HRFlow.Api/Controllers/ActivationController.cs) exposes POST only for redemption.
The [page](../../src/HRFlow.Client/src/features/auth/components/ActivationPage.tsx) reads the fragment
into memory, removes it from the address bar and posts only on explicit password submission. A GET,
including a mail scanner fetching the page, must never consume a token. No auto-login follows.

[Restore](../../src/HRFlow.Infrastructure/Services/Operations/BackupRestoreService.cs) clears passwords,
rotates credential versions/security stamps, revokes refresh tokens, clears invitation hashes/expiry
and invalidates unfinished imports. Delivery must not reconstruct links from restored state or send
restored work automatically. Follow the [recovery runbook](../operations/backup-restore.md); activation
is not the account-recovery/first-administrator command.

## Practical transport comparison and official sources

Sources retrieved 20 October 2026. Live technical guidance, not contractual promises, pricing checks
or verification against an actual organisation account. No free-tier or licensing availability assumed.

### Organisation authenticated SMTP

An existing mail team can own sender permissions, routing, credentials, incident response and recipient
processing. Application configuration needs host/port, TLS mode and dedicated authentication material.
This may avoid a new processor, but depends on existing infrastructure and administrator approval;
it is not inherently cost-free. Maintain a reviewed MIME/SMTP library rather than writing the protocol.

Microsoft's [application submission guidance](https://learn.microsoft.com/en-us/exchange/mail-flow-best-practices/how-to-set-up-a-multifunction-device-or-application-to-send-email-using-microsoft-365-or-office-365),
sections Client SMTP submission / SMTP relay, distinguishes authenticated mailbox submission from
connector-authenticated relay. It recommends OAuth; client submission requires TLS 1.2 or later and
sender permission. Do not conflate this with unauthenticated Direct Send or assume these exact
settings apply to another organisation's relay. Its [OAuth guide](https://learn.microsoft.com/en-us/exchange/client-developer/legacy-protocols/how-to-authenticate-an-imap-pop-smtp-application-by-using-oauth),
section Use client credentials grant flow, documents application SMTP permissions and Exchange
service-principal configuration. Tenant/mail administrators must approve least privilege. This design
does not depend on Basic Authentication availability or a retirement-date assumption.

### Transactional-email HTTPS API

A dedicated processor offers structured acceptance/errors and provider identifiers, but introduces
account approval, API-key custody, processor retention and commercial terms. Sender/domain ownership
and deliverability still need work. HTTP is convenient, not proof of mailbox placement or exactly-once.

As a concrete comparison, Postmark's [Email API](https://postmarkapp.com/developer/api/email-api),
Send a single email / Request headers / Body format / Response, requires a server token and confirmed
sender, returns MessageID/ErrorCode, and exposes open/link-tracking controls. Disable both tracking
features for activation. Its [delivery webhook guide](https://postmarkapp.com/developer/webhooks/delivery-webhook),
What is a delivery webhook, defines receiving-server acceptance, not inbox placement. No webhook is
proposed for the first slice. Account costs, retention/location and organisation approval are unresolved.
The attempted sender-signatures user-guide URL could not be retrieved; sender-confirmation claims
here rely on the retrieved Email API, not that missing page. Do not infer DNS verification completion.

## Proposed configuration and custody contract

These are proposed keys/constraints, not currently accepted configuration:

- Explicit delivery mode Disabled (default), approved SMTP, or Development-only pickup. No production
  pickup/sink fallback. Replace the startup blanket rejection only for the implemented approved mode.
- Preserve trusted Activation:ApplicationOrigin: HTTPS outside Development, exact origin without
  userinfo/path/query/fragment, never Request Host/forwarded Host. Preserve current expiry bounds/UTC.
- Fixed sender and reply/help address, bounded lengths, single validated mailbox each, reject CR/LF.
  One recipient per invitation, no CC/BCC, employee data in headers, attachments or tracking pixels.
- SMTP host/port allowlisted by deployment operator, mandatory TLS and normal trust/hostname checks.
  Explicit authentication mode; secrets/certificates from restricted secret storage, never committed
  config, CLI arguments, exception messages or normal diagnostics. No arbitrary user-selected endpoint.
- Proposed total transport deadline 15 seconds, maximum configurable 30; includes DNS/connect/TLS/auth/
  submission, linked cancellation. One attempt per explicit operation, no SDK/HTTP resilience retry.
  Validate bounds at startup; timeout classification depends on whether submission could have begun.
- Bound concurrent sends per host process (propose two), include queue wait in deadline; CSV remains
  bounded to existing 100 rows and sequential row flow. This is not a cross-process global rate limit;
  provider quotas/429 require explicit recovery, not automatic retry or rate guarantees.
- Plain text plus escaped minimal HTML, expiry UTC stated accurately, no diagnoses/personnel details.
  Do not log To, message body, links, hashes, authorization headers or provider exception bodies.
  Disable HTTP/SMTP protocol/body tracing and URL rewriting/tracking. Provider dashboards and private
  sink contents hold secrets: restricted operators only, never screenshot/export into review artifacts.
- Operations owns secret rotation, sender changes, alarms for safe aggregate failures/age, account
  suspension and incident response. App logs allowlisted actor/attempt identifiers, category and counts
  only; provider IDs if retained are bounded private metadata, not echoed provider responses.

## Proposed attempt and acknowledgement protocol

Reuse the existing activation mechanism; no second activation entity or notification outbox with links.
An explicit nonsecret invitation generation/attempt ID, recipient revision and safe outcome metadata
may require a small migration; confirm during implementation. Profile version alone is broad: unrelated
employment edits must not invalidate a good link. The token hash remains the invitation authority.

1. Under existing writer protection, recheck initiating credentials/current HR, target active/pending,
   expected employee version and recipient. Rotate invitation, capture invitation generation/recipient
   revision and mark PendingDelivery atomically with creation/resend. Never save raw token or message.
2. Commit and release reservation. Before network submission do a short consistent eligibility read
   for the captured generation/recipient. If superseded/inactive/expired, do not submit. Large MIME/network
   work is outside SQLite protection. It is impossible to guarantee this read prevents an immediately
   subsequent deactivation/change from racing with already-dispatched mail; stale email may arrive.
3. Submit once with bounded deadline. Map expected exceptions/statuses into allowlisted results;
   never swallow programmer/integrity faults with blanket success. Transport Accepted means relay/API
   accepted responsibility, not delivered/read. Keep PickupReady separate. Avoid UI label Sent unless
   explicitly qualified as transport accepted. No provider callback may activate an account.
4. Short protected acknowledgement must compare invitation generation/hash, captured normalized recipient
   revision, pending/passwordless state, active profile and unexpired UTC expiry. A late old attempt cannot
   overwrite Activated, a newer invitation or changed email. Persist/return the authoritative guarded
   result, not the adapter's local result when the compare failed. Return Superseded/Unavailable or
   refresh-required safe feedback; retain no secrets. Background/system acknowledgement, if introduced,
   must have a narrow internal boundary and never impersonate an HR session or bypass business writes.
5. Acknowledgement failure is not a failed account transaction and not proof the email failed. Return
   committed-account feedback plus unknown delivery state where the response is possible. Client reloads
   current status; never repeats creation or automatically replays resend after refresh/session replacement.

First slice deliberately has no durable recoverable-token retry. A committed PendingDelivery can remain
unresolved after crash. HR explicitly refreshes and resends, rotating the hash and invalidating every
older link. A retained in-memory raw value must not be reused after an ambiguous send. Durable automatic
retry would need a separately approved encrypted secret/outbox, key custody/rotation/recovery/retention
and a claim protocol; it is not permission to persist plaintext or derive links from the hash.

## Failure and recovery matrix (proposed)

- Configuration invalid/unapproved: fail before account/row creation or resend rotation; actionable
  safe configuration category. Do not probe/send merely to validate startup.
- Transaction validation/rollback: no committed account/invitation or email; preserve existing data.
- Crash after account commit, before send: PendingDelivery, account remains created; explicit resend
  only. CSV Created row stays Created; delivery failure never changes it into creatable Ready.
- Definite TLS/auth/recipient rejection before acceptance: DeliveryFailed, no password established;
  HR corrects configuration/address then reloads and explicitly resends a new invitation.
- Connection loss/timeout/cancellation after possible submission: DeliveryUnknown (new proposed state),
  not Delivered/Failed certainty; mail may arrive. Warn HR that resend invalidates an older email.
- Accepted response: Accepted (new proposed state), not mailbox delivery. Nonreceipt remains a support
  problem; HR verifies address/expiry and chooses explicit resend. No automatic resend on UI polling.
- Crash after acceptance, before acknowledgement: persisted PendingDelivery/unknown. Do not reconstruct
  acceptance from absence of error. Explicit resend can produce duplicate messages with only newest link valid.
- Resend races: one expected version commits; old send/ack is superseded. New request must reload;
  no automatic callback replay. Provider message IDs are not an exactly-once guarantee.
- Email change: invalidate old invitation within employee edit; suppress stale acknowledgement. An
  already-accepted message cannot be recalled. New address requires explicit versioned resend.
- Deactivation/activation during send: no new eligible acknowledgement; existing redemption rules deny
  inactive accounts or already-consumed invitations. Never restore roles or employment to deliver.
- Expiry: reads may project Expired; no implicit extension. GET/prefetch/link preview does not consume.
- CSV delivery failure or cancelled HTTP after committed row: directory/status is authoritative; use
  existing resend, not another import. Batch safe feedback should reconcile with live invitation generation.
- Recovery/restore: no automatic sending; cleared hashes invalidate old mail. Re-establish authorised
  operational access through documented recovery, then perform fresh current-HR review/invitation.

## Layer/workflow implementation order

1. Domain contract: add only safe delivery states/attempt identifiers if required. Keep framework-free.
   Do not change password eligibility, medical privacy, leave transitions or role permissions.
2. Application contracts: safe typed outcome and shared create/CSV/resend status semantics; retain
   current version/credential checks and non-replayable operation rules. No controller orchestration.
3. Infrastructure: one reviewed MIME/SMTP adapter, validated configuration and safe exception mapping;
   generation-aware preparation/acknowledgement plus minimal persistence migration if needed.
   Never call network inside SqliteWriteTransaction; retain no raw token in persistent queues.
4. API: thin safe status mapping; preserve request bounds/rate limiting. No public resend/GET redemption,
   registration, message-content endpoint or diagnostic bypass. Startup remains disabled until approval.
5. Client: pending activation separate from Active/Inactive; Accepted/Unknown explanation, refresh and
   explicit resend confirmation. Keep drafts/versions, duplicate protection and initiating-session guards.
   No token display, download, toast or notification preview. Update HR attention filters for new states.
6. Operations: update configuration/recovery runbooks, secret custody and deployment acceptance checklist;
   no first-admin or password-recovery repurposing. Export/backup must not add message secrets.

## Local retained verification plan and acceptance criteria

Proposed tests, **not executed in this documentation phase**. No paid account needed. Prefer a controlled
loopback SMTP fixture for deterministic protocol barriers plus optional private Mailpit inspection of real
MIME. [Mailpit runtime options](https://mailpit.axllent.org/docs/configuration/runtime-options/), General,
SMTP relay/forwarding, documents external forwarding switches and disabling version checks. Pin/review
the tool version during implementation; current docs may differ from installed versions.

- Disposable migrated SQLite and generated synthetic identities only. Bind sink UI/SMTP to loopback
  isolated ports; deny outbound network at process/container level, disable relay/release/forwarding,
  reverse DNS/version checks and debug logging. Do not rely on defaults. Restrict private sink storage,
  authentication/TLS fixture keys and test credentials. Owned-process shutdown before directory deletion.
- Emit real multipart MIME, validate sender/recipient/header injection/escaping, expiry and link origin.
  Test fixture reads message privately in memory and redeems the link through actual browser/API; assert
  hash-only persisted state, password established once, unchanged roles/active status and normal login.
- GET/prefetch and browser load leave hash/password unchanged. No auto-submit with password. Test
  expiry, malformed links, used links, password validation and no automatic sign-in.
- Explicit barriers cover crash before send, accepted-but-dropped reply, timeout, ack failure/restart,
  concurrent resend, email change, deactivation, role/credential loss and separate-process contention.
  Inspect persisted generation/hash/state, employee versions, Created CSV outcomes and no orphan account.
- Manual create and CSV use same formatter/coordinator. Delivery failure never recreates an employee.
  Unknown UI, explicit resend, draft retention and delayed same-/different-account completion covered.
- Transport tests prove deadline/no automatic retry and that another SQLite writer can proceed while
  SMTP is blocked. Retain strict production private-pickup/sink denial and disabled-mode creation refusal.
- Seed private canary secrets and capture application/provider-client diagnostics privately. Fail on
  token/link/password/header/address leakage; report fixed labels/counts only. No sensitive screenshots,
  trace/video, raw assertion values, sink message files or CI artifacts. Keep canaries out of test output.
- Restore clears invitation authority and never triggers delivery. Existing creation/import/activation,
  password/session, role, recovery and notification tests remain required regressions.

Local sink acceptance is not provider verification. Before enablement separately verify organisation
account/sender authority, DNS SPF/DKIM/DMARC design, TLS/authentication, quotas, real recipient consent,
bounces/nonreceipt response, provider retention and target-host egress/monitoring. Those checks require
owner approval and remain unexecuted. This design neither sends email nor certifies deliverability.

## One bounded next implementation prompt

After D1-D5 owner approval, implement only the approved authenticated SMTP activation adapter and the
safe generation-aware acknowledgement/status changes described here, with hash-only persistence and
explicit rotated resend (no automatic durable retries). Add the private loopback retained fixture,
manual/CSV/resend failure and race gates, HR safe feedback and configuration documentation. Keep
production mode disabled by default and require explicit approved configuration; no external sends in
verification. Do not add general email notifications, webhooks, password reset, new account recovery,
paid-service dependencies for tests, or automatic delivery from restored data. Return exact source,
migration consequences, local evidence and remaining provider/domain gates; do not enable deployment.
