# 0034: Durable own-account submission recovery

27 October 2026. Implemented for local review on `feat/leave-submission-recovery` from merged
PR #138 (`49f3373477e9052f2d4f68d1317f3e6ce8dbe81d`). The implementation phase did not publish or change external services. Publication authorised for independent review on 9 November 2026; no merge or deployment.
Search of existing issues found related submission, #83/#84, #93 and #76 scope, but no dedicated
submission-recovery issue. No tracker change or closure is proposed automatically.

## Contract and compatibility

`POST /api/v1/leave-requests` keeps its existing body and HTTP 200 GUID response. An optional
`submissionKey` is a nonempty UUID. Existing callers may omit it: their existing behaviour remains,
but **unkeyed calls have no durable duplicate-execution protection**. All maintained form submissions
now use keys. Existing direct API regression fixtures deliberately remain compatible unkeyed callers.
Actor/employee scope is always server-derived; a supplied `employeeId` is overwritten.

For keyed requests, a successful transaction records `(OwnerId, Key, RequestId, PayloadHash, CreatedUtc)`.
A unique database index on `(OwnerId, Key)` protects all API processes. A second independent key
means a separate intentional request; Pending overlap/balance semantics remain unchanged.

`POST /api/v1/leave-requests/submission-outcome` accepts the **original complete keyed body** and returns
HTTP 200 with a nullable `requestId`: a UUID string for a committed outcome, or JSON `null` when absent.
It is a read-only POST with `Cache-Control: no-store` and a 16 KiB body limit; it never executes a
missing submission. A null result means no committed outcome was visible in that read snapshot,
**not proof the original operation stopped**. A subsequent explicit POST with the identical key/body
can safely complete or recover that same attempt. Neither operation is automatically replayed by
the form's Axios refresh mechanism.

Both paths require current active/activated Employee-or-Manager access, current credential version
and the original owner's request association inside existing database protection. HR-only cannot
use personal submission/recovery; combined roles remain additive. Another owner using the same UUID
has a different scope and sees no original outcome. Recovery never grants access from possession of
a UUID alone. Missing/invalid authentication and permission loss remain 401/403. Invalid shape is a
safe 4xx; valid changed business inputs under an existing key conflict with 409.

## Canonical identity, privacy and ordering

The only extra payload persistence is a SHA-256 digest of deterministic UTF-8 JSON format 1:

- Type ID, start/end `.NET DateTime.Ticks`, reviewed type and policy version UUIDs.
- Description trimmed exactly as submission normalises it; blank/whitespace becomes null. Internal
  characters/newlines are preserved, with no Unicode or statutory reinterpretation.
- Distinct document UUIDs sorted by UUID value. Ordering is irrelevant. Duplicate IDs in incoming
  requests remain invalid under the existing five-distinct-document shape rule.

Date ticks retain the server-parsed DateTime value; hashing adds no further timezone conversion.
UI submits offset-free midnight ISO date-times as before. Existing DateTime parsing of offset-bearing
API input is unchanged; retain consistent host timezone configuration across lineages. Different time
components remain different inputs. No second copy
of descriptions, medical information, file bytes, filenames or original body is stored. Hashes/keys
are not exposed through broad summaries or logs. Format 1 is a durable contract; future changes must
preserve recovery compatibility rather than blindly rehash old outcomes under new rules.
The digest is private linked metadata, not anonymised data; privacy, retention and legal-hold
decisions still apply.

The initial credential-checked deferred read distinguishes a known outcome before file verification.
If absent, existing checksum verification occurs **outside** writer protection. A competing identical
submission may bind the drafts meanwhile: a recoverable verification conflict is retained until the
writer gate, where the newly committed outcome takes priority. The writer reservation rechecks current
credentials, personal roles, ownership and payload identity before returning any recovered ID.

Only a missing outcome proceeds to fresh date, manager, type/policy version, requirements, balance,
overlap and document metadata/binding checks. Keyed elapsed-date checks run there, not in pre-handler
shape validation. Unkeyed legacy callers retain their original early shared date validation as well;
all new attempts recheck dates inside the writer gate. Validation keeps the existing FluentValidation
HTTP 400 category. A recovered outcome can therefore survive elapsed dates, changed policy, already
bound evidence and later Approved/Rejected/Cancelled state. These are **not** new submissions.

Request, immutable submission snapshot, Submit audit, bindings, notification outbox and outcome commit
in the same existing SQLite transaction. Its callback is not automatically replayed. Even though the
existing submission helper calls SaveChanges before outcome insertion, neither SaveChanges commits
the enclosing transaction. A late insert failure rolls everything back. Recovery skips notification
and audit creation entirely. Preview stays read-only and shares no execution key/reservation.

## Form state and recovery

An explicit initial submit captures one fresh key and an immutable in-memory payload, separately from
editable fields. A network/5xx/uncertain result retains it, disables new submission and offers
**Check submission outcome**. No visible outcome permits only **Retry original submission**, using
that original key/body, never the edited draft. An authoritative original rejection permits normal
correction/new attempt. Errors during uncertain recovery do not silently release it.

Dates, descriptions, document selections and reviewed versions remain editable/retained; edits never
rewrite the attempted payload. Recovery success says the request is recorded, without incorrectly
claiming its current state is still Pending. Preview never changes the attempt or reviews versions.
Duplicate clicks are gated; transport is abortable; completions check initiating identity and session
generation, including same-account relogin. Cache invalidation targets the initiating account namespace.
The existing shell, responsive themes and keyboard flow are retained, with a focused uncertainty group.

The attempt is intentionally **not persisted in browser storage**. Refresh/route exit loses local
recovery inputs; a before-unload warning is installed while uncertain, and the form directs users to
history before a new attempt. There is no automatic restoration or replay after navigation/logout.
An API client retaining its original body/key can still explicitly recover after fresh authorised
login. A general cross-device attempt catalogue is outside this focused slice.

## Migration, retention and encrypted recovery

`20261027000212_AddLeaveSubmissionOutcomes` adds a table/index/RESTRICT request and owner foreign keys.
Existing requests/accounts/profiles/documents/history remain unchanged; **no historical keys are
invented**. Generated designer and snapshot match the model. No dependency change.

Outcomes have no automatic expiry/cleanup. Retain the identity for the lifetime of the request; casually
deleting it would permit duplicate execution. Any future approved retention policy must retain a safe
execution tombstone or explicitly retire the replay contract. This adds bounded per-key storage, with
growth proportional to successful keyed submissions, not failed checks.

Downgrade drops outcomes and permanently loses duplicate/recovery protection for those keys while
retaining requests. Re-upgrade cannot reconstruct them from history. Coordinate stopped writers and
backups; do not resume old keyed clients after such a downgrade as if their attempts were absent.
No downgrade execution is claimed unless explicitly recorded in verification.

The existing encrypted database snapshot includes associations and hashes; attachment integrity and
publication markers use the existing backup tooling unchanged. Restore does **not** erase successful
outcomes or dispatch notifications/submissions. Existing mandatory security invalidation removes old
password/session/invitation authority. A receipt never restores authority or bypasses current access;
operator recovery remains the existing restricted procedure, not first-admin provisioning.
See [recovery runbook](../operations/backup-restore.md). Events/requests committed after the snapshot
cutoff remain in the documented unrecovered window: reconcile them operationally before another attempt;
this table cannot recreate missing post-cutoff history or prove cross-lineage exactly-once execution.

[Executed checks and remaining limitations](../verification/leave-submission-recovery.md).
