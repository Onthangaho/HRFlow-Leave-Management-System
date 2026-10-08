# ADR 0010: Optional Manager decision notes

## Decision and API contract

Extend issue #21's existing queue rather than rebuild it. The previously optimistic-mutation
wording is corrected to server-confirmed outcomes; the issue remains open for review.

POST /api/v1/leave-requests/{id}/approve and /reject accept an optional JSON body:

```json
{ "decisionNote": "Coverage discussed with the team." }
```

An omitted body, missing property, null, empty or whitespace-only note means no note.
Trim leading/trailing whitespace; retain interior whitespace and line breaks. The limit is
500 trimmed UTF-16 code units, consistently counted by C# string.Length and JavaScript
string.length; supplementary Unicode characters use two units. No truncation is performed.
Both decisions use the same limit; rejection notes are not required. Oversized input returns
400 ProblemDetails. Success remains 204. Current permission denial remains 403 (inactive
bearer accounts 401), missing requests 404, status/contention conflicts 409, and approval
balance/overlap failures 400. The route identifies the request; authentication identifies the
actor. The body contains no actor or request identifiers.

## Persistence, protection and privacy

AuditEntry remains the only audit mechanism. DecisionNote is separate from Reason, which
continues to hold HR deactivation cancellation context. The existing domain transition
appends exactly one immutable decision entry with its note, actor, UTC time, states and
correlation. Application validation and the domain factory both enforce the bound. Only
Approve/Reject entries can carry notes. No edit/delete-audit API is added.

The existing non-deferred SQLite writer reservation is acquired before authoritative reads.
Current active Identity Manager membership, current same-department reporting scope, no
self-decision, Pending status and approval-time policy/history/balance/overlap validation
remain unchanged. Status and audit/note SaveChanges and commit together; rollback clears
tracked work. The three-second per-command contention timeout and absence of application
transaction replay remain unchanged. Separate API processes coordinate through the database.
SQLite's database-wide single writer remains a throughput limitation; direct SQL writers can
bypass domain length validation and this protocol. No provider-portability claim is added.

Owners with personal capability, current eligible Managers and HR see ordinary decision
notes through existing authorised personal history/timeline scopes. Authorization and reads
continue to share a deferred snapshot. Non-HR sensitive deactivation context is still generic;
raw reasons and correlation metadata remain HR-only. Notes render as React text, never HTML.
Managers are told not to enter medical or sensitive personnel details into ordinary notes;
this guidance does not automatically redact content. Notes are intentionally visible to the
owner, unlike restricted HR cancellation context. Actor names remain current display names.
Structured decision logs include request, actor/action and correlation identifiers, not notes.

## Migration and legacy data

20261008204514_AddManagerDecisionNotes adds only nullable TEXT DecisionNote with model
maximum length 500 to AuditEntries. No updates/backfill, changes to prior audits, reason
columns, request states or actor/timestamp data occur. Legacy and non-decision notes remain
null. SQLite TEXT does not itself enforce the metadata length; API/domain validation does.
The existing unique transition index and foreign keys are unchanged. Down removes the new
column and therefore loses any new decision notes; a rollback requires that explicit data-loss
review. The disposable down/reapply verification used legacy null-note data only.

## Queue and session behavior

The shared native ConfirmationDialog starts on Cancel, traps keyboard focus including the
textarea, supports Escape when idle and restores the opener or queue refresh fallback.
Approval uses the primary action style; rejection remains visibly destructive. The dialog
identifies employee, type and inclusive calendar dates without timezone shifts.

The loaded request/draft live outside queue rows. Background refetches cannot replace them.
Errors retain the note. A 409 disables confirmation until an explicit availability refresh;
an unavailable request stays disabled and directs the user back to the queue/history. A
refresh does not save or replay the note. Dirty dismissal requires a deliberate discard.
Client trimmed-length validation, pending controls and a synchronous in-flight guard prevent
duplicate clicks. Unknown network outcomes are not described as confirmed decisions; check
history before retrying an ambiguous outcome.

Only a successful server response closes the dialog and announces the outcome. TanStack
mutation retry is explicitly false. Existing valid same-session access-token refresh and its
bounded retry remain unchanged; auth/session code and non-replayable lifecycle writes are
untouched. Mutation callbacks and notices verify the initiating login epoch, including
same-account relogin. The shell resets protected drafts on session replacement. Queue keys
include account/session; reads use cancellation signals and refetch on entry. Successful
same-session decisions invalidate queue, team, reports and timelines for that account only.
Other accounts refresh their history/balances on entry/focus/manual refresh; this tab cannot
invalidate another account's cache.

See [actual verification, screenshots and limitations](../verification/manager-decision-notes.md).
