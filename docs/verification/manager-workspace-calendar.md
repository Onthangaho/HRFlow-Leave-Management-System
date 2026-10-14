# Manager workspace/calendar verification

14 October 2026. Branch feat/manager-workspace-calendar from fetched origin/main
42cd7152bdc213bcbe2c5db95433f74e21260c28. Initial working tree clean; PR #113 verified MERGED.
Issue #91 title/scope verified. Original stores/unrelated work preserved; implementation phase made no staging/commit/push/PR.

## Executed API and persisted-row checks

New independent synthetic database/private-object copy, real TLS APIs in two separate processes
sharing SQLite. New passwordless synthetic Manager/report accounts activated privately, no real
recipients. No raw invitation/credentials appear in review artifacts.

- New Manager empty attention count/preview and empty team result; personal dashboard 200.
- Seven eligible Pending requests, count 7 and ordered preview 5. Self/non-report/other-department
  requests excluded. Only Approved intersects team range; Pending/Rejected/Cancelled excluded.
- Two overlapping approvals for the same report intersect today; inactive report approvals remain
  labelled. Manager's own Approved request excluded. No emails/descriptions/decision notes in summaries.
- Employee/HR-only APIs 403; combined-role Manager capability 200. Actual Identity Manager removal
  denied old JWT with 403. Inactive/pending activation denied old-token reads 401.
- Inclusive 62 days accepted; 63, missing/reversed rejected 400. Leap-February/year-crossing API ranges
  accepted. Helper boundary/union checks described below.
- Real HR Assign updates moved a report away/back. Attention and current Approved visibility changed,
  while preserved Approved rows each retained exactly one original matching Approve audit.
- Self, HR-only and Employee approve/reject calls 403; own request remained Pending and audit count
  unchanged. Final SQLite integrity_check ok, foreign_key_check empty.

Fixture construction: existing submission disallows past starts. Only a newly created disposable
Pending request's dates were set to negative offsets before real approval to exercise ongoing leave.
A synthetic report's department was changed directly after API setup to exercise invalid historical
reporting scope that normal management prevents. No original history/policy/calculator changed.

Temporary fixture failures: GUID case representation initially caused a foreign-key failure; that
store was stopped and preserved, then a new isolated copy used. A role-removal check initially
removed zero rows because Identity key representation differed; exact persisted Identity key lookup
fixed the temporary harness, and real removal/denial passed. Not product passes or hidden failures.

## Executed real Chrome interactions

Isolated temporary Playwright controlled installed Chrome against actual TLS client/API; not static
rendering. Product dependencies unchanged. Synthetic interactions:

- Manager-only Overview retains personal dashboard, five-row attention and calendar. Daily count
  today is one despite two overlapping Approved requests. Inactive report shown/excluded.
- Keyboard focus/Enter on Previous month; September/October navigation, Today, selected date and
  whole-range list controls. No decision controls added to calendar.
- Injected 503 calendar refresh hid metrics/list rather than zeroing them, kept month, and real Retry
  recovered. Revoked Manager membership hid old attention/calendar; explicit retry recovered after
  restoring only the disposable membership.
- Actual queue Approve dialog -> server success -> Overview attention 7 to 6. Persisted Approved
  status and exactly one matching audit. Later real owner cancellation from the second API process
  changed count 6 to 5. No whole-page reload required.
- Employee/HR-only protected Team URL denied within signed-in SPA. Fresh protected URL redirected
  to login then denied Employee after authentication. Current session persistence is intentionally
  in-memory; first harness incorrectly expected it to survive a full browser navigation. Corrected
  SPA and actual login-return deep-link checks passed. Combined roles retained Personal/Team/HR,
  one Overview, and working employee-management navigation.
- Held actual attention responses across different-account login exposed no Manager panel. Held
  count 6 across same-account relogin could not replace new count 5 after the cancellation.
- Held actual team snapshot before HR reassignment could not restore former report visibility after
  same-account relogin. Different-account login could not expose the held Manager calendar. Real
  reporting assignment restored afterward only for continued disposable verification.
- Actual Manager personal Request Leave form succeeded; Pending request and exactly one Submit
  audit inspected, self remained excluded from attention. Initial harness expected different success
  wording; corrected follow-up real form submission/persistence check passed.
- Visible-page 60-second polling caused actual attention/upcoming/calendar HTTP reads with an
  accelerated browser clock. This is not a measured real-minute/hidden-tab-duration drill.
- Desktop 1366, tablet 768 and mobile 390 widths: no page-level overflow, Light/Dark. Reduced-motion
  browser setting used. Final six screenshots show count 5 and then-current synthetic records.

## Helper/build/model/audit checks actually executed

Temporary tool transpiled the actual calendar.ts (no test framework/file committed): 4,800 months
1900-2299 all Monday/Sunday bounded adjacency, 28-42 dates; leap-day/year-boundary assertions and
active employee overlap union passed. No calendar months converted to entitlement days.

- Backend dotnet build HRFlow.sln --no-restore passed: 0 warnings/errors.
- Final frontend build passed: 828 modules, main JS 574.97 kB /170.38 kB gzip (previous dashboard
  570.67 /169.38). Existing Vite >500 kB warning remains; no unrelated optimization.
- Final lint exit 0: only existing useAuth.tsx:223 Fast Refresh warning. New helper/export and
  redundant-rename warnings were corrected and final lint rerun. Final naming of range/preview
  constants also reran the actual date helper and frontend build/lint; behaviour unchanged.
- EF has-pending-model-changes: no changes since last migration; explicit disposable connection.
- Solution vulnerability check all five projects: no findings on configured feeds. npm audit: zero
  in all categories. No package, lockfile, schema or migration changes.
- Complete working/new-file diff, whitespace/UTF-8/sensitive-content scans and instruction byte
  comparison performed; reviewed synthetic screenshots are each below 300 kB.

## Screenshots

Synthetic records only, no secrets/invitation links/real personal data/machine paths. Desktop captures
show component detail; mobile captures the list-first calendar viewport, not static rendering.
- [Attention Light](screenshots/manager-workspace-calendar/attention-desktop-light.png)
- [Attention Dark](screenshots/manager-workspace-calendar/attention-desktop-dark.png)
- [Calendar desktop Light](screenshots/manager-workspace-calendar/calendar-desktop-light.png)
- [Calendar desktop Dark](screenshots/manager-workspace-calendar/calendar-desktop-dark.png)
- [Calendar mobile Light](screenshots/manager-workspace-calendar/calendar-mobile-light.png)
- [Calendar mobile Dark](screenshots/manager-workspace-calendar/calendar-mobile-dark.png)

## Explicit limitations / unexecuted checks

No exhaustive role/credential revocation-versus-snapshot race scheduling, measured real-minute/hidden
polling, large-team/load/Linux/deployment checks, full screen-reader/contrast/keyboard matrix,
new backup drill, full HR CRUD/onboarding/reporting/document-scanner regression or complete rejection
regression. Existing role denial/reassignment/queue approval/submission and personal Overview were
actually exercised as above. Two processes served the shared store; this is not a new transaction
stress guarantee. Team as-of is labelled CLIENT receipt, not server snapshot time; default/Today uses
browser SAST date and depends on browser clock accuracy. Separate panels are separate read snapshots.

No statutory/cycle/holiday/policy/charging changes; #85/#22/#75 dependencies remain. #92 not started.
Implementation phase made no commit, push, PR, merge, deployment or issue closure. [API/metric design](../adr/0020-manager-workspace-calendar.md).

Final fixture APIs/client stopped and original fixture pointer restored. Nothing was staged during implementation. Complete
review patch includes all 24 intended files and new screenshots; no temporary tooling is included.

## Independent-review finalisation (14 October 2026)

Reviewed complete source/new-file diff, API/date contracts, documentation and all six synthetic
screenshots. Confirmed current active/activated Manager/credential checks share deferred read
snapshots with scope/data, self exclusion, overlap union, inactive labels and bounded adjacent-date
ranges. No further source defect found; no code change or additional build/browser run required.

Finalisation reruns: intended-file and sensitive-content scans, instruction byte comparison, working
and staged whitespace checks and staged diff inspection. Earlier build/model/audit/API/SQLite/Chrome
checks remain implementation evidence, not finalisation reruns. Issue #91 rechecked open/matching;
it is referenced without closure. All outstanding checks and clock/timestamp limitations above remain.
