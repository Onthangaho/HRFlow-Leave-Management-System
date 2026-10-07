# Employee deactivation UI verification

UI follow-up to completed issues #25/#26 and merged PR #69. Checked on 2026-10-07.
PR #69 was confirmed MERGED; this branch was created from fetched origin/main after a clean
working-tree check. No issue was created or closed. The implementation verification phase
made no commit, push or PR; this report records its evidence for subsequent draft review.

## Implemented contract

- The HR employee directory has labelled search (name/email/department/manager/roles) and
  All/Active/Inactive filtering. Text status badges remain. Active records offer Edit and
  a separate Deactivate action; inactive records offer only read-only View details.
- HR-only directory/detail responses add identityUserId (opaque linkage for reliable
  self-deactivation), deactivatedAtUtc, deactivatedByName and deactivationReason. The actor
  name is the current preserved employee display name, not a historical name snapshot.
  No public selector, audit mechanism, logging payload, schema or migration changes.
- PATCH /api/v1/employees/{id}/deactivate still sends only expectedVersion and trimmed reason.
  A dialog retains its originally loaded version despite background refetches. Reason is
  required, with a 500-character input limit. No pending count is predicted; success uses
  the committed cancelledRequestCount.
- Conflict input is retained. Explicit Reload employee confirms reason discard, fetches a
  fresh HR detail snapshot, then replaces the version; an inactive response opens details.
  Closing/Escape/reporting navigation confirms discard when a reason has been entered.
  The reporting path filters active direct reports for reassignment through existing edits.
- Saving disables inputs/actions and blocks closing; a synchronous in-flight guard also
  prevents duplicate calls before React renders the pending state. No mutation retry and
  no Axios authentication replay are permitted for deactivation. A 401 ends the session;
  403 and recoverable conflicts display safe server feedback while retaining the reason.
- Account-scoped invalidations cover employees, personal history/balances, approval queue,
  monitoring and configuration/reference queries for the initiating account only. A live
  session epoch getter guards completion, cache updates and notices even after logout and
  later login by the same account. The directory remounts on each epoch. Explicit reload
  uses an abort signal. Self-deactivation ends access, clears protected caches and routes
  to sign-in with a generic confirmation and the returned count, without name/reason data.
- Shared native dialogs retain safe Cancel initial focus, Escape, focus restoration and
  keyboard containment; textarea participates in the focus loop and titles use unique IDs.
  Details use a neutral closing action. Existing focus/reduced-motion styles remain.

## Executed checks

Temporary tooling, two API processes and all SQLite files used a newly created disposable
verification location outside the repository. Existing databases were not modified. Secrets,
HTTP token responses, logs and verification tooling remain outside published documentation.
No automated test files were added.

- Real HTTP and independent SQLite inspection: HR deactivation 200 with count 2; exactly
  two Pending requests became Cancelled, each with one audit containing the correct HR
  actor, reason, timestamp and Pending -> Cancelled transition. Approved, Rejected and
  previously Cancelled rows/audits remained equal to their original snapshots.
- Blank/501-character reasons: 400. Missing target: 404. Stale and repeated original/current
  version requests: 409 without duplicate audit entries. Last-active-HR and active-report
  safeguards: 409. Reports were reassigned before manager deactivation succeeded.
- Employee/Manager API calls: 403. Revoked HR old-token write: 403. Inactive HR old-token
  write: 401. Inactive login, refresh, selector/history/balance/submission access: 401.
  Inactive profile edit: 409; inactive manager assignment: 400.
- A disposable late audit-insert failure returned 500 and rolled back employee state/version,
  request status and audits. The temporary trigger was removed.
- Eight submission/deactivation races across two API processes: deactivation 200 in all;
  submission 401 in two and 403 in six; no requests persisted for those race employees.
  Eight approval/deactivation races: approval 409/deactivation 200 in all; each request
  persisted Cancelled with exactly one terminal audit. This run exercised deactivation
  winning the races; it does not demonstrate both execution orders in this UI follow-up.
- Active login/refresh, submission/approval, HR creation/editing, directory and history
  continued returning the expected successful HTTP responses.
- HR detail matched persisted lifecycle time, actor name, reason and account linkage;
  no password/hash/token fields were present. A final real HTTP detail read returned 200
  with an explicit UTC Z suffix after the projection adjustment; the reason stayed unchanged.
  Leave-type selector contained no lifecycle
  reasons or account linkage.
- An additional HR account self-deactivated successfully (200, count 0). Its old token was
  rejected with 401, another HR stayed active, and SQLite recorded the self actor correctly.
  This verifies server behavior, not browser sign-out or notification rendering.
- An independent SQLite BEGIN IMMEDIATE held the writer: HTTP 409 after 3,180 ms, safe
  ProblemDetails, unchanged employee state. Three-second provider contention handling is
  unchanged; no application replay. UI conflict presentation remains browser-unverified.
- Temporary React static rendering confirmed Active rows contain Edit/Deactivate, Inactive
  rows contain View details with neither Edit nor Deactivate, and filtered-empty messaging.
  The first SSR harness attempt failed on CommonJS loading; correcting the temporary loader
  produced the stated passing render result. This is not a browser interaction check.

- Temporary Axios adapter exercised the actual authentication interceptor: lifecycle 401
  sent once, never refreshed/replayed, and cleared the current session; an old-epoch delayed
  401 sent once and did not clear the new session. This does not verify browser callbacks.

## Commands

Frontend: npm run build succeeded (TypeScript + Vite, 233 modules transformed).
Frontend: npm run lint exited 0, with the existing useAuth Fast Refresh warning and no errors.
Final git diff --check exited 0. Both instruction files were checked byte-for-byte identical.
Backend: dotnet build HRFlow.sln --no-restore succeeded, 0 warnings, 0 errors.
The cached restore assets were used; a fresh online restore was not executed.

## Unexecuted browser scenarios and screenshots

The computer-use inventory returned no apps or browsers. Opening the in-app browser returned
"Browser is not available: iab". Therefore **no real browser workflows or screenshots were
executed/produced for this follow-up**. Prior screenshots are not reused as new evidence.

Still requiring independent browser verification: reason validation and duplicate clicks;
stale/already-inactive reload/discard; active-report/last-HR/contention permission messages;
self-deactivation sign-out/count; direct HR URL denial; deliberately delayed completion during
logout/account switching (including same-account relogin); search/filter; Tab/Escape/focus
restoration; desktop/mobile overflow and readability; screen-reader announcements; existing
employee creation/editing and leave forms. Source/render/build/API checks do not prove these
interactions. No Safari/Firefox, accessibility assistive-device or production load checks.

The implementation is reviewable but browser acceptance is not complete. No reactivation,
hard deletion, unrelated redesign, dependency upgrade or new issue number is included.
