# HR department leave reporting verification

Executed 8 October 2026 on `feat/hr-department-leave-reporting` for verified open issues #29
(Department-level leave reporting query) and #30 (HR Admin reporting dashboard).
PR #71 was confirmed merged; the starting tree was clean and the branch was created from
fetched origin/main. No commit, push, PR, merge or issue closure was performed.

## Reporting contract

See [ADR 0007](../adr/0007-hr-department-leave-reporting.md) for definitions documented before
implementation, the response fields, date limit, authorization and read-snapshot design.
Pending/Approved requests intersect the inclusive selected dates. Distinct Approved employees
include inactive history once per person. Approved request-days sum clipped per-request
calendar durations; same-type and cross-type overlap remain counted separately. This is not
unique people-days absent or active-staff coverage. Attribution is the current department.
Inactive Approved requests/employees/request-days are explicitly identified inside totals.

## Executed API and SQLite checks

A newly created disposable database and real Development API/client were used. Private
temporary tooling, tokens/passwords, signing configuration, logs and databases remain outside
the repository. Existing databases were preserved; no permanent automated test files were added.
Five seeded departments and three synthetic departments existed. Department fixture insertion
used independent SQLite, since department CRUD is deliberately out of scope. HR accounts,
profiles, assignments, policies/types and leave transitions otherwise used real HTTP APIs.

- Before adding leave data, the report returned 200 with zero metrics for existing departments.
- Initial November result: **2 Pending requests, 6 Approved requests, 3 distinct Approved
  employees, 12 clipped Approved request-days**. Inactive contributions were **1 Approved
  request, 1 employee and 2 request-days**. All eight departments were present, including
  zero-record departments. Each overall metric equalled its filtered department sum.
- Operations contributed 10 request-days and Support 2. One active Operations employee had
  same-type and cross-type overlapping approvals, contributing 2 + 3 + 3 = 8 request-days
  but one distinct employee. The inactive employee contributed the other 2 Operations days.
  These summed durations were never presented as unique absence.
- A request beginning in October counted only its November portion; another ending in
  December counted only November 30. A November 1 single-day report returned 3 Approved
  request-days across 2 distinct employees; November 30 returned 1 Approved request-day
  and 1 Pending request. Wholly outside requests, Rejected and Cancelled requests were excluded.
- Filtering Operations returned its identical metrics; selecting the valid Empty department
  returned 200, one department and zero metrics. An unknown department returned 404.
  Missing/malformed/reversed dates, an empty department GUID and 367 days returned 400;
  an exactly 366-day range returned 200.
- Employee/Manager-only accounts returned 403; combined HR/Manager returned 200. An existing
  HR JWT returned 403 after current HR membership was removed through employee management.
  An inactive HR account's old JWT returned 401.
- A real department/manager reassignment moved the active employee's preserved leave into
  Support: Operations became 2 request-days and Support 10, while overall metrics remained
  identical. Returning the assignment restored the original breakdown. All ten existing
  audit rows remained unchanged during reads/reassignment/lifecycle checks.
- A future no-leave period returned zero totals for all eight departments. Property checks
  confirmed department aggregates only, without employee identifiers/names, emails or reasons.
- A separate Node/SQLite connection/process held BEGIN IMMEDIATE and an uncommitted department
  change. The API returned 200 with the last committed 10 Operations request-days in under
  two seconds; the temporary update was rolled back. This verifies no writer reservation or
  uncommitted attribution for reporting. It is not a forced between-handler-statements test.

An initial verification assertion expected four departments instead of the actual eight seeded/
synthetic departments. The tooling was corrected and rerun on a fresh disposable database;
the earlier database was retained. No production record was modified to fit a verification result.

## Executed real browser interactions

Chromium **154.0.8037.98** used the real client/API, 1440px desktop and 390px mobile viewports,
with reduced-motion preference enabled. Interception only delayed actual responses or simulated
one 503; it did not replace interaction with static rendering or Axios-adapter checks.

- Dashboard navigation and current-month defaults worked. Editing dates/department retained
  October/All departments result labels until Apply. Applying November or one department
  changed the response-labelled results. Reset applied the current month/all departments.
  Reversed and oversized ranges displayed validation without relabelling existing results.
- Pending count and department metrics were visible together. Recharts exposed keyboard
  arrow exploration and useful request-day tooltips. Native date-input segment tab stops were
  traversed to the department selector, whose keyboard focus had a visible 3px outline.
- Desktop/mobile pages had no document-width overflow; the full table is horizontally
  scrollable on mobile. Focusing its scroll region and pressing ArrowRight moved it to the
  metric columns; a separate mobile table screenshot captures those units and values.
  All metric headings/units and inactive contributions were inspected
  in the screenshots. No chart/bar/tooltip animation was enabled.
- A delayed real refresh marked existing results as refreshing/aria-busy. A simulated 503 hid
  result rows, retained filters, suppressed raw diagnostics, and explicit Try again recovered.
- Delayed actual December responses released after logout/login as another HR account and
  after same-account relogin did not replace the new session's October report or filters.
- Employee and Manager-only accounts had no Leave reports link and were denied on direct SPA
  navigation to the HR route. Combined HR/Manager accounts received the HR capability.
- A further real browser check revoked current HR membership while its old HR JWT remained
  on the page: refresh returned 403, hid aggregates, retained November inputs and showed a
  clear permission message. Explicit retry recovered after HR membership was restored.
- Existing HR Pending monitoring and employee creation/editing remained usable. Browser
  creation returned 201, editing returned 200 and persisted the changed profile with its
  manager preserved. Manager team/history labels and queue approval worked; approval returned
  204 and persisted one matching Approve audit. Personal history/submission worked; the new
  request returned 200 and persisted Pending. No page errors occurred in the main browser run.

The regression approval changed the fixture's November totals to **1 Pending, 7 Approved,
3 employees and 13 request-days**; final screenshots show this later dataset. Inactive
contributions remain 1 request/employee and 2 days. Initial 12-day arithmetic evidence above
was inspected before that additional approval; the distinction is deliberate.

Browser tooling initially assumed a date input had only one Tab stop; Chromium's native
date segments required additional Tabs. The department selector also needed an explicit
accessible name; that product correction was made and the interaction run passed afterward.

## Dependencies and executed builds

Issue #30 explicitly requests Recharts. The only added direct dependency is **recharts 3.10.1**
(`^3.10.1` manifest), with its 38 new locked dependencies/peers, including react-is 19.3.0.
Existing locked package versions did not change. Its documented peer range supports React 19.
The reporting route is lazy-loaded; no other package was upgraded or forced audit fix used.

- Before install and final `npm audit --json`: **0 total vulnerabilities**, at every severity.
- `dotnet build HRFlow.sln --no-restore`: **Build succeeded, 0 warnings, 0 errors**.
- Final frontend build: **810 modules transformed**, completed in 1.25s; initial JS 490.91 KB
  (148.68 KB gzip), reporting/chart chunk 368.51 KB (106.78 KB gzip).
- Final lint exited 0 with the existing `react(only-export-components)` Fast Refresh warning
  in the auth hook. No new lint warning or chunk-size warning was reported.
- No migration is required. The first backend compile caught an incorrect namespace import;
  it was corrected before the successful build and API verification.
- `git diff --check` exited 0. New-file whitespace checks passed, both instruction files are byte-identical, and
  changed-document credential/path scans returned no matches. Complete source/lockfile and
  screenshot review found only intended changes; disposable/generated artifacts are excluded.

Official chart references used: [Recharts installation](https://recharts.github.io/en-US/guide/installation/),
[BarChart accessibility layer](https://recharts.github.io/en-US/api/BarChart/) and
[animation controls](https://recharts.github.io/en-US/guide/animations/).

## Screenshots and privacy

Screenshots use synthetic department aggregates only, without accounts, tokens, credentials,
employee names, reasons or machine paths. Source PNGs are linked below; full mobile captures
include the vertically stacked cards and chart/table rather than pretending they fit one screen.
The five PNGs are approximately 18–151 KB each and were visually inspected.

- [Desktop dashboard](screenshots/hr-department-leave-reporting/desktop.png).
- [Mobile dashboard](screenshots/hr-department-leave-reporting/mobile.png).
- [Mobile table metric columns after keyboard scrolling](screenshots/hr-department-leave-reporting/mobile-table.png).
- [Empty selected department](screenshots/hr-department-leave-reporting/empty.png).
- [Safe failure and retry](screenshots/hr-department-leave-reporting/retry.png).

## Unexecuted checks and limitations

- No physical device, other browser engine, screen reader/assistive-device or formal contrast
  certification; desktop/mobile refers to real Chromium viewport interaction, not physical phones.
- No production-scale/load benchmark, pagination or forced read-contention timeout. The range
  is bounded but matching aggregate inputs are materialized; very large history needs reassessment.
- No instrumentation forcing a concurrent change exactly between authorization and report
  SELECTs, or two-API-process stress test. Snapshot guarantees follow the shared deferred
  transaction, documented SQLite behavior and the executed independent reader/writer check.
- No exhaustive regression of every policy/lifecycle dialog. Executed regressions are listed
  above. No automated suite or clean npm-ci check is claimed.
- Other accounts' caches cannot be invalidated remotely. Entry/focus/manual refresh reads current
  state; an authorized read already in flight may return its earlier snapshot. The three-second
  provider timeout is per command, not an overall HTTP deadline; rollback journaling may delay
  writer commits. Recharts adds a separate approximately 107 KB gzip reporting chunk.
