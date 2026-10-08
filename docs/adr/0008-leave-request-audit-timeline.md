# ADR 0008: Single-request audit timeline

## Existing mechanism and issue #31

Issue #31's AuditLog/interceptor description is satisfied through the existing AuditEntry
aggregate mechanism, not a second audit table or interceptor. LeaveRequest.Create records
Submit (null -> Pending) for new requests with its employee as actor. Approve/Reject/Cancel
append one existing AuditEntry inside the domain transition. Owner Cancel has no reason;
HR deactivation Cancel retains its existing acting HR employee and reason prefix.

Application supplies correlation metadata through IRequestCorrelationContext. Its API adapter
uses the request TraceIdentifier selected by the existing logging middleware. Valid supplied
X-Correlation-ID values (1-128 ASCII letters/digits, dash, underscore, dot or colon) are reused;
invalid values fall back to the server identifier. Response header, completion/exception logs
and new audit entries use that identifier. There is no HTTP dependency in Domain. Correlation
IDs are diagnostics, not authentication credentials or trusted user identity.

The existing SQLite writer reservation runs before authoritative reads. EF tracks the request
and its audit collection; existing SaveChanges/transaction commits them together. Submission
saves its new aggregate inside that transaction. HR deactivation saves employee state and all
Pending cancellations together. Failure/rollback leaves no persisted transition audit. There
is no application replay or additional audit interceptor. Direct database writers bypass this
domain protocol; the system does not promise auditing for arbitrary external SQL changes.

Audits contain actor employee IDs, UTC timestamps, action, old/new status, optional cancellation
reason and optional bounded correlation ID. No entity serialization, Identity data, password,
credential or token payload is added. Existing transition uniqueness remains unchanged.

## Migration and legacy truth

AddAuditCorrelationMetadata adds only nullable TEXT CorrelationId (model max length 128) to
AuditEntries. Existing rows retain null metadata; no backfill, renaming, deletes or invented
submission events occur. Existing timestamps, actors, states and deactivation reasons remain
unchanged. Down removes only the new column. SQLite does not enforce TEXT length itself;
the domain validates the bound. The regenerated model also reflects the pre-existing
computed reason capacity of 523 characters (500 plus the prefix), correcting the old snapshot
metadata of 522; this migration issues no reason-column alteration and preserves all values.

GET /api/v1/leave-requests/{id}/timeline returns an explicit request summary and events,
with submissionRecorded false when no actual Submit/null -> Pending event exists. The UI
says "Submission event was not recorded for this legacy request" and never synthesizes it.
Events order by Timestamp then audit ID, a deterministic tie-breaker, not an invented order
within identical timestamps. TimestampUtc is emitted with explicit UTC kind/Z; the UI always
labels UTC. Actor names are CURRENT display names, not historical snapshots.

## Authorization and privacy

The authenticated Identity actor is server-derived. Authorization and all timeline inputs
share the existing deferred SQLite snapshot with no writer reservation:

- Current active Employee/Manager capability allows reading owned requests.
- Current active Manager membership allows current same-department direct reports' requests,
  including inactive reports. Manager self-history uses owner access, never implies self-decision.
- Current active HR membership allows organisation-wide read-only history, including inactive
  employees. HR does not gain decision controls.

Role-less/revoked access returns 403; inactive bearer accounts receive 401. Unknown and
out-of-scope request IDs both return 404 with the same message, avoiding request enumeration.
No audit create/edit/delete endpoint exists. Summary contains request ID, employee display
name/active status, leave type, dates/status and submissionRecorded. Events contain audit ID,
actor employee ID/current name, action, UTC time and state transition. Only current HR receives
raw cancellation reason and correlation metadata. Other permitted readers get a generic
employee-deactivation explanation and null correlation ID. Ordinary UI never displays IDs or
correlation metadata prominently. Existing personal-history DTOs expose neither raw reasons
nor correlation metadata; their audit ordering and UTC output are also made consistent.

As in ADRs 0006/0007, the first SELECT establishes the snapshot. A read already authorized
may complete against its earlier snapshot during a concurrent revocation; subsequent reads
recheck current membership. Narrow BUSY/LOCKED handling remains safe 409 with the existing
three-second per-command timeout and no application retry. Forced statement interleavings
are not implied by ordinary verification.

## Client behavior

Protected /leave-requests/:id/history offers role-aware workspace links and a reusable semantic
ordered timeline. Entry points are personal history, manager queue/team view and HR Pending
monitoring. Loading, refresh, retry, missing/forbidden and honest legacy/empty states are separate.
Refresh errors hide previous details; successful refetches identify previously loaded history.
No timeline mutations or HR decision actions are introduced.

Axios consumes cancellation signals. Query keys contain account, login session epoch and
request ID; queries require a known role and refetch on entry. Account/session/request changes
remount protected content with no previous-data placeholder. Applicable decisions, owner
cancellation, lifecycle/reporting/profile and configuration writes invalidate this account's
timeline prefix, guarded by the initiating session. Other accounts refresh on entry/focus/manual
refresh. No dependency, authentication replay or session persistence change is introduced.

See [executed verification and limitations](../verification/leave-request-audit-timeline.md).

## Optional Manager decision notes (#21 follow-up)

[ADR 0010](0010-manager-decision-notes.md) adds a separate nullable DecisionNote to existing
AuditEntry. Approve/Reject append it to the same immutable transition; Reason privacy,
correlation filtering, read snapshots and legacy event truth remain unchanged.
