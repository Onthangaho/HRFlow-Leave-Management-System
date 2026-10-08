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

## Initial browser blocker (2026-10-07; superseded below)

The computer-use inventory returned no apps or browsers. Opening the in-app browser returned
"Browser is not available: iab". Therefore **no real browser workflows or screenshots were
executed/produced for this follow-up**. Prior screenshots are not reused as new evidence.

At that time, the following browser scenarios were unexecuted: reason validation and duplicate clicks;
stale/already-inactive reload/discard; active-report/last-HR/contention permission messages;
self-deactivation sign-out/count; direct HR URL denial; deliberately delayed completion during
logout/account switching (including same-account relogin); search/filter; Tab/Escape/focus
restoration; desktop/mobile overflow and readability; screen-reader announcements; existing
employee creation/editing and leave forms. Source/render/build/API checks do not prove these
interactions. No Safari/Firefox, accessibility assistive-device or production load checks.

At the initial review, browser acceptance was not complete. Subsequent real-browser evidence follows below. No reactivation,
hard deletion, unrelated redesign, dependency upgrade or new issue number is included.


## Real Chromium follow-up ? 2026-10-08

The connected browser inventory still had no browser, but the explicitly requested isolated
Playwright option successfully launched installed Chromium 154.0.8037.98 headlessly. It used
the real Vite client and API with a new disposable SQLite database, synthetic accounts and
requests. Existing databases were not modified. Temporary tooling, private credentials,
tokens and logs remain outside the repository; no dependency or automated-test files were added.
The HTTP/SQLite, static-render and Axios-adapter evidence above remains distinct from these
actual browser interactions.

### Discovered defects fixed

- Escape from the inline discard prompt retained the reason but left focus on BODY. It now
  returns focus to the reason textarea. The final keyboard run asserted the focused element.
- Shared native dialogs were pinned to the top-left after the CSS reset. Explicit auto margins
  now center them; desktop/mobile browser bounds and screenshots confirm the placement.
- After successful self-deactivation, the protected-route redirect overwrote login navigation
  state, hiding the returned cancellation count. The generic count-only confirmation now
  lives in in-memory auth state, survives that redirect and clears on the next login/logout.
  No employee name, email, reason, token or persisted session is added to this notice.

### Executed interaction and persistence evidence

- Desktop 1440x1050 and mobile-width 390x844: directory search, All/Active/Inactive filters,
  empty results, text badges, Active Edit/Deactivate and inactive read-only details passed.
  Browser assertions found no page-level horizontal overflow. The table intentionally scrolls
  horizontally inside its container on narrow screens. Read-only details include lifecycle
  time, actor and reason and offer no edit/reactivation action.
- Dialog initial focus was Cancel. Forward and backward Tab wrapped through the reason
  textarea, reporting action and footer buttons. Blank reason validation focused the textarea.
  Escape closed a clean dialog and restored its opener; a dirty reason prompted discard.
  Keep reason and Escape from discard retained input and restored reason focus. Explicit
  discard closed the dialog and restored the invoking Deactivate action. Details Escape
  restored View details focus. Centered modal bounds were asserted at both viewport widths.
- A real API profile update made the loaded deactivation version stale. The attempted PATCH
  returned 409 with the original version, leaving the reason and active state unchanged.
  Keep reason retained input; explicit discard/reload cleared the reason and loaded the new
  version. Only a deliberate new submission then succeeded using that returned version.
- External deactivation while the dialog was open caused a real conflict. Explicit reload
  switched to inactive details and updated directory actions without replaying the PATCH.
- Two click events produced exactly one PATCH. Its real server response was held in Playwright
  after commit; saving controls stayed disabled and Escape could not dismiss the dialog.
  Releasing the response produced one success. This is real API transport gating, not a
  fabricated success response or static-render check.
- Active-report 409 retained the reason; reporting navigation opened the active direct-report
  view for existing edits. An independent SQLite BEGIN IMMEDIATE caused a real contention
  409 that displayed recoverable feedback and retained input. No automatic replay occurred.
- An actual browser deactivation displayed the server count of 2. Independent SQLite reads
  confirmed two Cancelled requests, each with exactly one Pending -> Cancelled audit and
  the correct acting HR employee, timestamp and full synthetic reason. Inactive details
  showed the preserved lifecycle information.
- Actual successful PATCH responses were held across SPA logout/login as another Employee
  account and, separately, as the same HR account. No full reload or auth-state injection was
  used. DOM observation found no old success notices/protected-content leaks; request counts
  showed no old-session employee refetch. The same-account fresh directory read showed the
  now-inactive record. These delay scenarios were repeated after the final auth fix.
- Successful HR self-deactivation with one Pending request reached login, displayed count 1,
  removed protected employee/account content and denied browser Back. Logging in and out as
  another account cleared the generic confirmation rather than carrying it into a later session.
- External HR deactivation while its browser dialog was open caused a real server 401.
  Exactly one PATCH and zero refresh calls were observed; no replay occurred, access ended
  and protected content disappeared. Separately, removing current HR membership caused a
  real 403 with clear permission feedback and the entered reason preserved.
- A disposable-only setup left one active HR administrator. The actual self-deactivation
  form returned last-active-HR 409, retained its reason and left the account active/access intact.
- Existing employee create/edit forms saved via the real APIs. Reloaded data confirmed both
  Employee/Manager roles were prefilled and retained, manager Preserve kept the relationship,
  and the edit version rotated. Policy create, dirty-discard and unused-delete dialogs worked;
  initial focus, Tab/Shift+Tab, Escape, draft retention and delete opener restoration passed.
  API reads confirmed the created policy was deleted after explicit confirmation.
- Authenticated Employee and Manager SPA navigation to the HR route showed denial. This used
  browser history navigation, not auth-state injection; address-bar full reload is intentionally
  a different case because sessions remain in memory only. The actual leave form submitted,
  personal history loaded, HR monitoring displayed the request, and its assigned manager
  approved it; SQLite confirmed Approved with exactly one matching manager approval audit.

### Final checks and iteration notes

Frontend build passed after the fixes: TypeScript and Vite, 233 modules transformed.
Frontend lint exited 0 with the existing useAuth Fast Refresh warning and no errors.
No backend files changed in this follow-up; backend build was not rerun. Final diff checks
and identical instruction-file checks were performed before committing.

Temporary verification iterations corrected an ESM file URL, an ambiguous Manager label
locator and a details lookup hidden by the retained Active filter. An incomplete intermediate
auth edit was caught by compilation, corrected, and followed by the passing build/browser
runs above. These failed attempts are not counted as passing checks.

### Screenshots and limitations

All seven PNGs were visually reviewed: only synthetic names/example.invalid addresses and
synthetic reasons are present; no credentials, tokens, real personal data or machine paths.
Desktop images are 1440x1050; mobile images are 390x844. Individual files are below 150 KB.

- [Desktop directory](screenshots/employee-deactivation-ui/directory-desktop.png)
- [Mobile directory](screenshots/employee-deactivation-ui/directory-mobile.png)
- [Desktop deactivation](screenshots/employee-deactivation-ui/deactivation-desktop.png)
- [Mobile deactivation](screenshots/employee-deactivation-ui/deactivation-mobile.png)
- [Desktop inactive details](screenshots/employee-deactivation-ui/inactive-details-desktop.png)
- [Mobile inactive details](screenshots/employee-deactivation-ui/inactive-details-mobile.png)
- [Completed offboarding details](screenshots/employee-deactivation-ui/completed-inactive-details.png)

This verifies headless installed Chromium and responsive viewport behavior, not a physical
mobile device, touch/soft-keyboard behavior, Safari/Firefox, screen-reader/assistive-device
compatibility, all zoom settings or production load. Delays held an actual successful response
after server commit; every possible network failure/ordering is not claimed. Protected cache
clearing was checked through resulting DOM, access and fresh request behavior, not private
TanStack internals. Keep PR #70 draft for independent review; do not infer readiness or merge.
