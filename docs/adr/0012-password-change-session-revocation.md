# Own-account password change and bearer revocation — issue #80

Checked 9 October 2026. Branch feat/password-change-session-revocation, base
0e9b8defa9e496ff5d87de8484762934871b5ca9 (merged PR #103). This slice is not deployed.

## Contract and authority

POST /api/v1/auth/password accepts currentPassword and newPassword only. The authenticated
Identity actor comes from IInitiatingCredential, never a JSON target ID. Active, activated
accounts can change their own credentials, including HR-only and combined-role accounts.
There is no HR password editing, forgotten-password/reset flow or email recovery.

Success is 204 after commit, with no new token pair: all prior sessions, including the caller,
end. Wrong current password or Identity validation errors are 400; absent/revoked/pending/
inactive authentication is 401; SQLite contention is 409; rate limit is 429; oversized body
is 413. Passwords are required, untrimmed and at most 256 characters. JSON body is bounded
to 4096 bytes. Current Identity validators enforce password strength. The per-IP/process
fixed window permits ten attempts/minute with no queue; production proxy/distributed abuse
protection is still an operational review item. Existing permission-loss 403 semantics remain.

The thin controller delegates to an Application service interface implemented by Infrastructure.
Identity ChangePasswordAsync verifies the existing password and validates the replacement.
See [Microsoft's ChangePasswordAsync contract](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.identity.usermanager-1.changepasswordasync?view=aspnetcore-8.0)
and [JWT bearer validation guidance](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/configure-jwt-bearer-authentication).
The existing issuer, audience, signing key, expiry and explicit 30-second skew remain unchanged.
SecurityStamp updates alone do not revoke this application's JWTs.

## Credential generation and serial boundaries

ApplicationUser.CredentialVersion is a persisted random GUID. Newly created accounts start
with a generation; every access JWT includes credential_version. Refresh retains the current
generation. OnTokenValidated requires a present, parseable matching claim as well as persisted
active/activated state. It still replaces stale role claims with current Identity memberships.
No positive revocation cache is introduced.

IInitiatingCredential exposes only actor ID and the original generation, with no HTTP object
in Application or Domain. RequestCredentialValidator rechecks proof through the caller's
DbContext immediately after BEGIN IMMEDIATE for writes and as the first SELECT of deferred
read snapshots. All existing protected business routes use these shared boundaries; their
existing role, owner, department, reporting, policy, status and version checks remain intact.
Login, refresh, activation and startup seeding are anonymous/system operations and retain
their own authoritative checks; absence of initiating bearer proof is not an invitation proof.

Inside the existing writer reservation, current-password verification, Identity password/
stamp persistence, credential-generation rotation and revocation of every outstanding refresh
row commit together. Failures roll back all those changes. No transaction callback replay is
added; existing three-second contention handling and SQLite's single-writer limit remain.
Two requests using one prior generation cannot both commit a password change.

Login and refresh use the same writer reservation: issuance ordered before the password
commit produces proof that becomes invalid; issuance ordered afterward requires current
credentials/refresh proof. Existing outstanding refreshes are revoked, including newly rotated
pairs issued just before the change. A protected write admitted at the early API gate must
recheck the original generation after waiting for the writer. An operation ordered before
password change can complete; one acquiring its authoritative boundary afterward is denied.

An already-authorized coherent read snapshot may finish against its old snapshot. This is
not retroactive cancellation of authorized in-flight responses. SQLite journal mode determines
whether that read delays the writer or continues on its earlier WAL snapshot. Subsequent API
use and refresh from other tabs fail; no channel instantly clears remote tabs visually.

## Rollout and downgrade

20261008221049_AddCredentialVersion adds one non-null GUID TEXT column. Existing Identity
rows receive independent SQLite randomblob-generated values. Outstanding refresh rows get
a UTC revocation time; previously revoked timestamps remain unchanged. This intentional
session invalidation requires fresh login for pre-upgrade access/refresh sessions. Passwords,
activation eligibility, roles, employee lifecycle/reporting, requests and AuditEntry rows are
unchanged. No historical credential-change event is invented.

Down removes only generation metadata; it cannot restore previous passwords or refresh
eligibility. Passwords changed during this release remain changed. An older binary lacks
explicit JWT generation checks: downgrade reduces security. Stop writers, back up the complete
SQLite database (including WAL through a consistent snapshot), invalidate signing credentials/
old sessions and review the rollback operationally. Re-upgrade assigns fresh random generations;
do not reuse the lost values. Disposable down/up preservation was checked, not production rollback.

## Client and privacy

Account → Change password is available to every authenticated role in the shared shell.
Labelled current/new/confirmation fields have appropriate autocomplete and accessible error
associations. Confirmation is client-only; Identity remains authoritative. Password fields
live only in component/request memory, never navigation state, storage, notices or query cache.

The password POST is explicitly skipAuthReplay. A 401 ends the initiating session without
refresh/replay. Other existing valid same-session expiry refresh behavior is unchanged.
A synchronous busy guard prevents duplicate clicks. On confirmed success the initiating
epoch clears credentials and protected caches, then a generic login notice survives redirect.
A new login clears that notice. Logout, account switch and same-account relogin invalidate
late callbacks through the existing interceptor and epoch guard.

Recoverable errors retain input. An unconfirmed network outcome explains that the operation
may have committed and directs the user to sign out and try normal login with the new password,
then the old password if necessary; no automatic resubmission or success claim. Logs record
actor/action/correlation/outcome identifiers, never passwords, token or stamp values. Existing
leave AuditEntry persistence remains the only leave audit mechanism and is not extended here.

See [executed verification and limitations](../verification/password-change-session-revocation.md).
