# Manager team leave summary: verification

Executed 8 October 2026 on `feat/manager-team-leave-summary`, for verified issues #27/#28.
PR #70 was merged; the starting tree was clean and the branch was created from fetched
origin/main. These implementation verification checks preceded commit and PR preparation;
no merge or issue closure was performed.

## Disposable setup and API evidence

One Development API and the real Vite client used a newly created disposable SQLite database.
Temporary scripts, database files, logs, account passwords and tokens stay outside the
repository. Existing databases were preserved. No automated test files were added.
The first setup attempted past-date submissions, which correctly returned 400; verification
then used future dates and a fresh disposable database. An intermediate fixture used an
invalid November 31 date, also correctly returning 400; it was corrected to November 30
before running the complete successful fixture. These were verification-tool errors.

Synthetic accounts were created through HR APIs and authenticated through real login.
A 200-calendar-day overlap-enabled policy/type provided room to verify overlapping approvals
without changing business rules. Submissions and approvals/rejections/cancellations used
their real endpoints. Independent SQLite connections inspected persisted request/audit rows.

Executed outcomes:

- Main manager summary: 200 with five Approved rows after reassignment, for two active
  employees and one inactive report. Two overlapping approvals for one active person still
  yielded a distinct-active-person count of two.
- Non-reports, another department, self, Pending, Rejected, Cancelled and wholly out-of-range
  requests were excluded. Cross-department and self checks also used deliberately malformed
  legacy relationships in disposable SQL fixtures; these are not achievable through the
  validated management APIs. Those relationships were restored after checking.
- Requests starting before November or ending after it were included with full dates.
  November 1 and November 30 single-day boundary queries each returned two intersecting rows.
- A real HR reassignment removed an Approved request from the old manager's view and added it
  to the new manager's view. All eleven existing audit rows remained byte-for-field unchanged
  after summary reads, reassignment and deactivation of a report with only completed requests.
- Empty team and empty future result returned 200 with `[]`. Combined HR/Manager membership
  received 200. Employee and HR-only tokens received 403. Removing Manager membership via HR
  update made the existing Manager JWT receive 403. An inactive manager's old token received 401.
- Missing, malformed, reversed and oversized date ranges received 400.
- Response property inspection confirmed exactly the documented eight fields, without email,
  lifecycle reason, Identity link or audit payload.
- While a separate Node/SQLite process held BEGIN IMMEDIATE and an uncommitted reassignment,
  the API still returned the last committed five-row scope in under two seconds. After commit,
  it returned three rows; restoring the relationship returned five. This confirms the read
  does not reserve the writer or expose uncommitted reporting changes. The snapshot boundary
  is established by the first handler read; this verification did not inject a pause between
  individual handler statements.

## Actual Chromium interaction evidence

Real Chromium 154.0.8037.98 ran against the real client/API with synthetic data. Browser
request interception was used only to delay real responses or simulate one 503; it was not
static rendering or an Axios-adapter substitute.

- Desktop 1440px and mobile 390px viewports displayed complete dates, employee grouping,
  the distinct-active-person count and inactive historical badge. Document width did not
  exceed viewport width. Screenshots below were visually inspected; names are synthetic.
- Tab and Shift+Tab moved through month controls, and Enter changed the month. Previous,
  Next and This month controls, empty results and a delayed actual-read loading state worked.
- A simulated 503 produced a safe error without its raw diagnostic payload. November stayed
  selected, coverage rows were hidden, and explicit Try again restored real data.
- Delayed actual summary responses were released after logout/login as another Manager,
  and after logout/relogin as the same Manager. Neither exposed the previous team's data.
  Query cancellation and login-epoch isolation were exercised through the real SPA.
- Employee and HR-only accounts had no Team leave dashboard link and were denied on direct
  SPA navigation to the Manager route. A combined-role account could open it.
- An actual approval through the existing queue returned 204, cleared that Pending queue and
  appeared in the team view on re-entry: the employee group changed from two to three rows.
  Persisted status was Approved with exactly one matching Approve audit.
- Personal history loaded; actual leave submission returned 200 and opened personal history.
  The new request was persisted as Pending. HR employee creation returned 201 and a profile
  edit returned 200; persisted name changed and the assigned manager was preserved.
- HR pending monitoring and employee-directory navigation remained usable. No page errors
  were observed in the main interaction run.

Screenshots show the initial five-row verification dataset, before the subsequent queue
approval. PNGs are approximately 45–78 KB each:

- [Desktop approved team leave](screenshots/manager-team-leave-summary/desktop.png) — 1440 × 1224.
- [Mobile approved team leave](screenshots/manager-team-leave-summary/mobile.png) — 390 × 1488.
- [Empty month](screenshots/manager-team-leave-summary/empty.png) — 1440 × 1050.
- [Safe failure and explicit retry](screenshots/manager-team-leave-summary/retry.png) — 1440 × 1050.

## Build and review evidence

Executed outputs: `dotnet build HRFlow.sln --no-restore` reported **Build succeeded, 0 warnings,
0 errors**. The final `npm run build` reported **235 modules transformed**, built in 1.17s,
with a 489.93 KB JavaScript bundle (148.50 KB gzip). `npm run lint` exited 0 with the existing
`react(only-export-components)` Fast Refresh warning in the auth hook. `git diff --check`
exited 0; Git emitted normal LF/CRLF conversion warnings, not whitespace errors. An earlier
frontend build caught unused session guards while editing; the guards were connected and
the final build/lint passed. No additional automated test suite is claimed.

Final browser smoke additionally confirmed the approved employee group had three rows,
This month returned October 2026, and keyboard focus had a visible 3px outline.

No dependency or migration changes are required. Instructions remain identical; documentation and screenshots contain
synthetic data, no credentials/tokens or machine-specific paths. Review includes new files.

## Unexecuted checks and limits

- No physical mobile device, other browser engine, screen reader, assistive device or formal
  contrast audit. Keyboard/focus checks cover this page's native links/buttons, not new dialogs
  (none were introduced). No claim of exhaustive accessibility certification.
- No production-scale/load benchmark, two-API-process read stress test, or forced contention
  timeout on this new read wrapper. Only BUSY/LOCKED are mapped by source inspection; the
  existing write timeout verification is not claimed as new summary timeout evidence.
- No instrumentation forcing a write exactly between authorization and projection statements;
  consistency follows the explicit shared SQLite snapshot and documented provider behavior.
- No exhaustive regression of every lifecycle or policy dialog. Executed approval, submission,
  history, HR create/edit and monitoring checks are listed separately above.
- Another account's browser cache is not invalidated remotely. Page entry/focus/manual refresh
  rechecks current scope; an already-running authorized read can return its earlier snapshot.
- Disposable verification tooling required corrections for past dates, an invalid date, a
  cached-month loading expectation, and asynchronous/ambiguous locators. Final successful
  runs used uncached loading data and actual labelled controls; no product behavior was inferred
  from those failed tooling attempts.
