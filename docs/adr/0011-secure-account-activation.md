# Secure account activation — issue #79

## Lifecycle and authority

HR creation no longer accepts a password. POST /api/v1/employees creates the existing
profile and assigned Identity roles atomically, but RequiresActivation is true and no
password is established. Activation is independent from Employee.IsActive. Both must
permit access: pending accounts fail login, refresh and bearer validation. Authoritative
Application read/write checks also recheck account eligibility inside their existing
snapshot/reservation. Roles remain assigned and visible in the HR edit DTO; activation
never adds/restores roles, employment status or reporting relationships.

Current active, activated Identity HR membership is required for creation and resend,
including a recheck after BEGIN IMMEDIATE. The recipient anonymously redeems a valid
invitation; they do not need HR or an existing login. Issue #79 and its catalogue wording
were corrected accordingly; the issue stays open. No public registration is added.

New manager assignments require active, activated current Manager membership in the same
department; preserve existing cycle/direct-report checks. Pending HR accounts do not count
as available administrators for last-HR protection. Existing-account development seeding
now preserves roles as well as passwords, activation and employment state; it must not
repair revoked capabilities on restart.

## Token and transaction design

Custom opaque invitations are 32 cryptographically random bytes, encoded as 64 hex
characters. Only SHA-256 of a purpose prefix plus the secret is stored, with a unique
nullable index on AspNetUsers.ActivationTokenHash. The stored row binds the proof to that
account; redemption accepts no employee/account ID. This is separate from refresh/reset
password purposes. High token entropy makes offline hash guessing impractical; the raw
secret is never returned by an API or included in ordinary logs.

Activation:ExpiryMinutes defaults to 1440 and accepts 5–10080 minutes. Dates are UTC;
redemption is denied at or after the exclusive expiry. Password input is bounded to 256
characters, then Identity's configured validators and AddPasswordAsync establish the
password. There is no custom weaker password validator and no overwrite/reset flow.
[Identity AddPasswordAsync](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.usermanager-1.addpasswordasync?view=aspnetcore-8.0)
and [Identity implementation](https://github.com/dotnet/aspnetcore/blob/v8.0.23/src/Identity/Extensions.Core/src/UserManager.cs)
were consulted on 8 October 2026.

The existing SQLite non-deferred writer reservation is acquired before reading the token,
account and active profile. Identity's password save, RequiresActivation=false, UTC
activation time and token/expiry removal commit together. A second process reads the
consumed token afterward and fails. Invalid passwords and persistence failures roll back
all changes and keep the invitation usable. No application transaction replay is added;
existing three-second SQLite contention protection returns 409. Unknown database failures
remain generic 500 and are not misclassified as expected conflicts.

Resend requires the original expectedVersion and rotates the existing employee version.
It replaces the hash/expiry under protection, invalidating the prior invitation. Profile
editing preserves pending state and roles; changing the normalized recipient email clears
the old invitation and requires explicit resend. Deactivation cannot be bypassed by
redemption/resend. A competing operation must follow the existing serial/version order.

## Delivery and failure states

IActivationDelivery is an Infrastructure boundary. The only supplied provider is
DevelopmentActivationDelivery. Creation/resend fail clearly before side effects unless
Activation:Delivery=PrivatePickup in Development and a trusted application origin is set.
There is no real email sending and production onboarding is disabled until an approved
provider is implemented/configured; this is not a production-ready email implementation.

Configure development through environment variables or local user secrets:

- Activation:Delivery = PrivatePickup
- Activation:ApplicationOrigin = http://localhost:5173 for local demonstration; a trusted
  HTTPS origin otherwise. No credentials, path, query or fragment are allowed. Never use Host.
- Activation:ExpiryMinutes = 1440 (optional).
- Activation:PickupProfile = Default (optional, 1–64 ASCII letters/digits/hyphens).

Private text pickup lives below the current OS user's LocalApplicationData/HRFlow/
ActivationPickup/{profile}, outside the repository and public serving directories. The
folder disables inherited Windows permissions and permits only the current OS identity;
Unix uses owner-only permissions. The private file contains the recipient and invitation
link. An authorised local demonstrator transfers the invitation privately to the synthetic
recipient. Do not publish these files or screenshots of them. Operators must remove stale
pickup files after use; automatic private-file retention/cleanup and real delivery are not
implemented. A non-absolute/unavailable private location fails delivery.

Account creation/resend commits PendingDelivery BEFORE delivery. No external call holds
the SQLite writer reservation. Success records PickupReady; filesystem failure records
DeliveryFailed, preserving the passwordless pending account and allowing explicit resend.
State acknowledgement uses a short separate protected write, conditional on the current
hash, so late acknowledgement cannot overwrite a newer invitation or activation. Process
crash, cancellation or acknowledgement contention can leave PendingDelivery (or a lost
HTTP acknowledgement): HR must inspect the directory and explicitly resend rather than
replaying automatically. Resend invalidates old pickup copies even if their files remain.
Activated indicates first-password establishment. Expired is computed on HR reads from
UTC expiry. NotRequired preserves legacy eligibility without inventing an event/date;
Unavailable denotes an unlinked account needing investigation, not automatic repair.

## API and client contract

- POST /api/v1/employees retains fullName, email, departmentId, roles and optional managerId;
  password is removed. 201 Location retains employeeId/version and adds safe
  invitationDeliveryState. Maximum creation body is 4096 bytes.
- GET /api/v1/employees and /{id} remain HR-only, consistent-snapshot reads and add
  requiresActivation, invitationDeliveryState and nullable activatedAtUtc. No secret/hash
  is projected. These are the invitation status/reload contracts.
- POST /api/v1/employees/{id}/activation/resend takes expectedVersion, returning 200 with
  invitationDeliveryState. Empty version is 400; missing 404; stale/inactive/established
  account or contention 409; lost permission 403 (unavailable authentication 401).
  Body limit is 1024 bytes. Refresh status to obtain the rotated profile version.
- POST /api/v1/auth/activation takes token/password only, anonymously, with 4096-byte body
  limit. Success is 204, with no automatic login or access/refresh token issuance.
  Missing/tampered/expired/used/inactive-target invitations share a safe 400 message.
  Identity validation errors are returned only for a valid invitation. Contention is 409.
- Redemption and resend share a fixed-window limit of 10 requests/minute per source IP,
  per API process, with no queue; excess is 429. This is not distributed abuse protection:
  trusted proxy/IP handling and perimeter/distributed limits need review before production.

Links use /activate#token=... so the secret is not sent in an HTTP URL/query, request-path
logging or Referer. The page captures it in memory and removes the fragment from browser
history; its referrer policy is no-referrer. Redemption sends the secret/password only in
the JSON body; current logging does not log request bodies, headers or query data.
Sensitive delivery exceptions are not logged. Response is no-store. Private pickup/browser
history/endpoint hardening must be reviewed for any eventual hosted delivery channel.

HR forms retain drafts after recoverable failures, display activation separately from
Active/Inactive and filter pending Managers from new assignments. Resend confirmation
uses the existing accessible dialog, original version, duplicate-click guard, explicit
stale refresh and initiating-session callback guard. Resend and creation are explicitly
non-replayable; redemption uses an anonymous Axios call without auth refresh/retry. Existing
JWT expiry/refresh code is unchanged. Activation requires password confirmation, keeps
input after failure and links to normal login after success.

## Migration and rollback

20261008212712_AddAccountActivation adds five AspNetUsers columns and the nullable hash
index only. RequiresActivation defaults false, delivery state NotRequired; nullable dates
and hash remain null for existing records. Existing passwords, roles, active status,
profiles, requests and AuditEntry rows are not altered. Passwordless legacy accounts are
not automatically repaired/invited; investigate them explicitly. No activation date or
historical event is fabricated.

Down removes these columns/index, losing invitation hashes, expiry, delivery/activation
metadata. It retains passwords already established and all employee/leave/audit history.
Stop writers/back up first. Do not run an older binary against pending accounts as a
production downgrade: the old binary lacks the pending-access guard. Inventory/disable
pending accounts through an approved operational plan before rollback. Disposable legacy
up/down preservation was executed, not a production rollback rehearsal.

See [verification and limitations](../verification/secure-account-activation.md).
