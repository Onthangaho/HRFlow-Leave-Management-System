# ADR 0009: JWT expiry and current permissions

## Decision and scope

Issue #74 closes an authentication gap: `ValidateLifetime` was explicitly false, and legacy
reads/personal writes did not repeat current permission checks inside database protection.
PR #73 was verified merged before creating the isolated security worktree from fetched main.
The unrelated roadmap documentation remains on its original documentation branch.

JWT validation now explicitly requires signed tokens and expiry, validates lifetime and keeps
issuer, audience and signing-key validation. The named `JwtClockSkewSeconds` constant is 30.
This tolerates small server clock drift; expiry may remain acceptable for up to 30 seconds,
and `nbf` may be accepted up to 30 seconds early. It replaces the library's five-minute default.
Keep servers' UTC clocks synchronized. This is validation configuration, not a changed token
issuance period; existing access/refresh lifetime configuration remains in effect.

After cryptographic validation, the existing active-account check remains. The API replaces
request-local token role claims with current Identity memberships so existing route policies
make an early current-role decision. This is not the authoritative operation check: a role or
lifecycle change can commit between authentication and the operation.

The new Application `CurrentAccountAuthorization` service reuses the scoped context/role lookup.
Legacy personal history/balances, queue/monitoring, employee management reads and reference
selectors now check current active capabilities in the same existing deferred snapshot as data.
Configuration reads check live HR inside that snapshot. Writes use private response projections
within their existing transaction, avoiding a nested read transaction. Department/role controllers
delegate to Application instead of querying EF/Identity themselves.

Submission and owner cancellation check current Employee OR Manager after the SQLite writer
reservation. Existing Manager decision and HR management checks already run there and remain.
No HR-only decision authority, self-approval, extra audit mechanism, changed calculation,
schema migration, account deletion or session persistence is introduced.

The [complete endpoint inventory](../security/endpoint-authorization-inventory.md) identifies
capability, scope and authoritative location for all 29 protected operations.

## Consistency and revocation

Read authorization/data use `BeginTransaction(deferred: true)` on the same scoped EF context.
The first SELECT establishes the snapshot, without a writer reservation. A read already
authorized can finish with its earlier snapshot while membership changes. Subsequent reads
recheck membership; the API does not retroactively retract prior HTTP responses.

Mutations reuse `BEGIN IMMEDIATE` before authoritative reads. Changes to employee roles,
reporting/lifecycle and leave/configuration operations follow a valid serial order across
connections/processes sharing SQLite. A request that authenticated before a committed revocation
must fail its post-reservation check. Status changes and their existing domain audits remain
atomic. Three-second per-command contention handling and no application callback replay remain.
SQLite still has one database-wide writer; this is not a throughput or multi-provider redesign.

## Refresh and client sessions

The corrected #74 contract retains valid same-session refresh and one bounded authentication
retry. Invalid/expired/revoked or replayed refresh proof ends only its initiating session.
Inactive accounts remain denied. Permission loss is 403 and does not trigger refresh.
The existing deactivation mutation retains `skipAuthReplay` and `retry: false`; its 401 ends
the session without another PATCH. Other existing replayable operations retain their prior
bounded authentication behavior; no general business-operation retry is added.

Axios now stamps the session epoch synchronously at dispatch and preserves it on retries.
Retries from another epoch fail before token injection/dispatch. Stale successful and failed
responses are rejected as cancellations rather than handed to callers. Refresh flights stay
scoped to the initiating epoch. AuthProvider keeps token refresh within its epoch, creates a
fresh boundary for every new login (including the same account), and makes logout a boundary
even with a pending login/no established session. Delayed login success is guarded too.
Existing account/session query keys, cancellation and cache clearing remain in use.

No JWT denylist or token-version schema is added. Logout clears the browser session; it does
not revoke every still-valid bearer token server-side. Active/current capabilities gate those
tokens until expiry. Password-change/global sign-out semantics remain separate backlog work.
Live permission changes are not pushed into client navigation; server denial is authoritative.

## References and evidence

Official documentation checked on 8 October 2026:

- [Microsoft TokenValidationParameters](https://learn.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.tokens.tokenvalidationparameters)
- [Microsoft lifetime validation and skew](https://learn.microsoft.com/en-us/dotnet/api/microsoft.identitymodel.tokens.validators.validatelifetime)
- [Axios interceptors, including synchronous dispatch](https://axios-http.com/docs/interceptors)
- [Existing SQLite writer reservation](0002-consistent-leave-decisions.md)
- [Existing deferred read design](0006-manager-team-leave-summary.md)
- [Verification and reproducible scenarios](../verification/jwt-current-permissions.md)
