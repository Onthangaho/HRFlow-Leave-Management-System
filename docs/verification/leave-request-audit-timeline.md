# Leave audit and request timeline verification

Executed 8 October 2026 on feat/leave-request-audit-timeline for verified open issues #31
(AuditLog entity + write-on-state-change interceptor) and #32 (Audit history view on a leave
request). PR #72 was confirmed merged. The starting tree was clean; origin was fetched and
the branch created from origin/main before editing. Nothing was committed/pushed/opened/merged.

## Decisions and migration

[ADR 0008](../adr/0008-leave-request-audit-timeline.md) documents reuse of AuditEntry/domain
transitions instead of a duplicate interceptor, snapshot access rules, DTO privacy and session
behavior. New Submit/null -> Pending events use the requesting employee; existing decisions
and deactivation reasons retain their original mechanism. Correlation metadata uses the same
validated request identifier as completion/exception logging and response headers.

The migration adds nullable CorrelationId only. A COPY of a previously disposable synthetic
pre-migration database was used, preserving the source database and all repository databases.
Before migration it had 11 audit rows and 13 leave requests. The generated snapshot reflects
the existing computed reason limit (523 rather than the old snapshot's 522); Up only adds
CorrelationId and never alters reason values or the database reason column. Every existing audit property
and every request row was compared after migration and remained equal; correlation values
were all null. No initial events or timestamps were fabricated.

Private temporary scripts, credentials/tokens, signing configuration, databases and logs are
outside the repository. No automated test files or package changes were added.

## Executed real HTTP and persisted-row checks

- New submissions each persisted exactly one Submit event, null -> Pending, with the employee
  actor. Approve, Reject and owner Cancel each appended exactly one matching terminal event.
  Repeating each operation returned 409 without adding an event. SQLite rows were inspected,
  not inferred only from HTTP status.
- Submitted and decision/deactivation correlation IDs matched supplied valid response IDs;
  the deactivation identifier was also present in structured request completion logging.
  An oversized identifier fell back to the server identifier. Non-HR timeline DTOs returned
  null correlation metadata; the ordinary UI showed none.
- A forbidden manager decision returned 403; policy-failed approval returned 400 after current
  entitlement was temporarily lowered in disposable data. Each remained Pending with only
  its Submit event. Entitlement was restored. A policy-failed submission created no request/audit.
- SQLite triggers injected late audit INSERT failures for approval, submission and HR
  deactivation. These deliberately returned 500 (unexpected injected failures), without any
  partial status change or terminal audit. Failed submission persisted neither aggregate nor
  initial event. Failed deactivation retained active status, original version and Pending
  request. Triggers were removed before successful operations.
- Successful deactivation returned one cancelled request and persisted one HR-actor Cancel
  with the exact deactivation reason/correlation. HR could inspect the inactive employee's
  history and raw reason; the assigned Manager got a generic deactivation explanation and
  no correlation ID. The inactive owner's old token returned 401. Historical Approved records
  remained available to HR.
- Active owner, assigned Manager, HR and combined-role HR could read in-scope timelines.
  Unrelated employees/unassigned Managers and unknown request IDs returned the same 404.
  HR reassignment changed manager access immediately; the original Manager lost access and
  the new assigned Manager gained it, without rewriting audits.
- Removing current HR through the real management API denied its old HR JWT's unrelated
  history (404, because its remaining Manager capability had no reporting scope). A current
  Manager membership was temporarily removed directly in disposable Identity rows and its
  old JWT returned 403; the membership was restored. The management safeguard correctly
  blocked attempts to remove Manager capability while active reports remained.
- A Manager submitted their own request after HR assigned a valid manager. Owner timeline
  returned 200, self-approval remained 403 with only Submit persisted, and their assigned
  manager's successful approval appended one event.
- Legacy requests without Submit returned submissionRecorded=false, including a request
  with no events at all. Existing events emitted UTC Z timestamps. Equal timestamps on NEW
  disposable events were ordered by audit ID exactly as the SQLite ordered query. Existing
  migrated timestamps were not modified for this ordering check.
- Renaming an actor through HR management changed the returned display name, demonstrating
  current names rather than historical snapshots; the profile was restored. DTO checks found
  no emails, Identity identifiers, passwords or tokens.
- Existing employee management, HR reporting/monitoring, manager team/queue, personal history
  and balance endpoints returned 200 with their intended accounts.

## Executed real browser checks

Chromium 154.0.8037.98 interacted with the real client/API using synthetic accounts. Desktop
was 1440px wide and mobile 390px, with reduced-motion preference. Interception only delayed
actual responses or supplied one simulated 503; static rendering was not substituted.

- Personal history View history showed Submit and Approved, actor, explicit UTC time and
  initial/terminal state transitions. Legacy history displayed the exact missing-submission
  explanation. Manager queue/team and HR Pending monitoring View history entry points worked.
- The manager's deactivation timeline showed the generic explanation without sensitive text;
  HR's read-only timeline showed the authorized synthetic reason. No timeline decision buttons
  or diagnostic IDs were displayed.
- Mobile had no document-width overflow. Keyboard Tab reached a link/button with visible
  focus. Ordered timeline, readable wrapping and desktop/mobile screenshots were inspected.
- A simulated 503 hid old details and suppressed raw diagnostics. Explicit Try again recovered
  real history. Delayed actual requests released after logout/login as another account and
  same-account relogin could not expose old detail content. An unrelated account's direct SPA
  route showed the same unavailable response as a missing record.
- Existing report dashboard and directory edit remained accessible. A separate actual browser
  regression created an employee (201), edited it (200) preserving its manager, approved leave
  (204), owner-cancelled leave (204), and submitted via the real form (200). Each successful
  leave transition had one corresponding persisted audit. The submitted request's View history
  displayed its initial event. An unauthenticated direct detail URL reached login with no
  protected content. No page errors occurred in the main timeline interaction run.

## Screenshots

Only synthetic display names/dates/reasons appear; no emails, tokens, credentials or machine
paths. All four PNGs were visually inspected and are approximately 44-56 KB each.

- [Desktop new lifecycle](screenshots/leave-request-audit-timeline/desktop.png)
- [Mobile new lifecycle and visible focus](screenshots/leave-request-audit-timeline/mobile.png)
- [Legacy missing submission](screenshots/leave-request-audit-timeline/legacy.png)
- [HR-only synthetic cancellation context](screenshots/leave-request-audit-timeline/hr-cancellation.png)

## Executed builds and review

- dotnet build HRFlow.sln --no-restore: final Build succeeded, 0 warnings, 0 errors.
  One repeat build hit the running disposable API's Windows DLL lock; stopping that process
  and rerunning succeeded. This was a tooling lock, not a source compile failure.
- Frontend build passed (812 modules transformed); lint exited 0 with the existing auth-hook
  Fast Refresh warning. No dependency upgrade or audit change was made.
- git diff --check and new-file whitespace checks passed. Changed-document credential/path
  scans returned no matches; complete diff and screenshot review found only intended changes.
  Both instruction files are byte-identical.

Verification tooling first compared SQLite null-prototype objects with ordinary JSON objects;
normalizing the comparison corrected that false failure. Reusing a disposable DB filename
with WAL state retained caused another invalid starting assertion; final migration/HTTP checks
used a fresh copied filename. Browser tooling initially used outdated dashboard link names;
the actual labels were used in the successful rerun. A regression attempt began before API
restart readiness; the later ready-server run passed. These are not product verification passes.

## Unexecuted checks and limitations

- No two-API-process race/stress or forced interleaving between authorization and timeline SELECTs
  in this change; consistent reads use the existing documented deferred transaction. No forced
  read-contention timeout or production-load benchmark. A previously authorized in-flight
  snapshot can finish after a role change; subsequent reads recheck roles/relationships.
- No physical mobile device, other browser engine, screen reader or formal accessibility audit.
  Keyboard evidence is basic Tab/focus navigation, not assistive-device certification.
- No exhaustive configuration/lifecycle dialog regression, migration Down exercise, or server-
  generated correlation log comparison; supplied-ID/header/audit/log matching was executed.
- Full-page authenticated refresh remains deliberately deferred session persistence; direct
  unauthenticated URLs reach login. No audit CRUD, historical actor snapshots, pagination or
  arbitrary external-SQL auditing is promised. No automated suite was added.
