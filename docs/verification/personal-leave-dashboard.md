# Personal leave dashboard verification

14 October 2026. Branch feat/personal-leave-dashboard from fetched origin/main
c96111f21690cb0f8263e94ae8d22c97c917db71. PR #112 confirmed merged 14 October 2026.
Initial working tree clean; unrelated/original stores preserved. Current-data #90 slice only.

## Actual local API and persisted-data evidence

Used a new independent copy of the synthetic CSV/employment fixture and private objects. No migration
needed. Real HR APIs created/activated synthetic Employee and Manager-only accounts and a high-balance,
overlap-enabled company policy/type for boundary verification. No external delivery or real data.

Executed with actual HTTP plus SQLite inspection:
- Employee, Manager-only and combined-role dashboard 200; HR-only 403. Current Employee role removal
  denied an existing JWT with 403. Inactive and Pending activation old-token requests denied 401.
  Temporary fixture flags/membership were restored only to continue verification.
- New personal accounts had zero Pending/history/upcoming and zero approved use. Passing another
  employeeId query could not change the server-derived owner or reveal another person's requests.
- Pending count 7, preview 5; upcoming/recent limits 10. Approved [today-3,today], [today,today+2],
  [today+90,today+92] included; [today+91,today+93] and [today-4,yesterday] excluded. Pending,
  Rejected and Cancelled excluded from Approved list.
- These initial Approved spans sum to 17 inclusive request-days, including overlaps separately;
  entitlement 1000, remaining 983. Full balance DTOs matched the existing balance endpoint and
  persisted dates. Subsequent verification approvals were also reconciled against that endpoint.
- UTC metadata and day-90 end checked. Temporary compiled verification tooling exercised the actual
  BusinessDate helper at 21:59:59/22:00 UTC rollover and leap-date rollover successfully.
- Timestamp ties sort deterministically by request ID descending. A synthetic legacy request with
  no audit/processing timestamp remained null with no invented event. Private description/note/
  correlation sentinels were absent from dashboard DTOs; authorized timeline returned 200.
- SQLite integrity_check ok; foreign_key_check empty.

Fixture construction is explicit: the existing submission validator forbids past start dates, so
negative-offset boundary dates were assigned only to newly created disposable Pending records before
real approval. Legacy absence/timestamp ties/privacy sentinels were constructed directly in the
synthetic copy. No production/original history or audit was rewritten; no past-date rule changed.

## Actual Chrome interactions

Temporary isolated Playwright controlled installed Chrome against the real TLS client/API; not static
rendering. Product dependencies were unchanged. Executed:
- Employee desktop/mobile Light/Dark, tablet width 768, no page overflow; primary action keyboard focus.
- Real Request Leave form submitted a request, Overview Pending count increased without reload.
  Existing history two-step cancellation then reduced count; persisted Cancelled status and exactly
  one matching Cancel audit inspected.
- Injected HTTP 503 refresh retained results with explicit stale warning; Retry fetched actual data.
- Delayed actual old response across different-account login: Manager-only count stayed zero, no
  previous owner request links. HR-only Overview made no personal dashboard request; CSV onboarding
  route remained usable. Combined-role Overview/Team/Administration composition and queue route passed.
- Stronger same-account relogin: held actual old snapshot, cancelled a request through another real
  session, relogged in, released old response. The newer different Pending count remained displayed.
- Actual manager queue approve/reject dialogs passed; processed rows disappeared and each status
  change had exactly one matching persisted audit. Owner Overview reflected the outcomes on entry.
- Actual approval through a separate manager session was reflected by polling with no reload; browser
  timer advanced 60 seconds for this check (not a measured real-minute wait). Persisted Approve audit
  and status inspected.
- Real role removal: 403 hid cached personal cards and suppressed the next periodic poll. Explicit
  retry recovered after restoring only the disposable membership.

Browser/source review found and fixed cached-card visibility on 403; no shared auth behaviour changed.
Earlier failed temporary runs referenced a deleted browser module, expected root instead of a guarded
return route, installed the accelerated clock after native timers existed, or hit direct fixture-write
contention with zero busy timeout. Corrected tooling/focused real runs passed; those failures are not
reported as product passes. Initial activation helper/management-route mistakes were corrected before
successful API verification. All temporary tooling/data is excluded from source review.

## Screenshots

Synthetic capture-time records; screenshots reflect their then-current snapshot, not latest fixture
counts after further approvals/cancellations. No secrets, private phone, activation links or machine paths.
- [Employee desktop Light](screenshots/personal-leave-dashboard/employee-desktop-light.png)
- [Employee desktop Dark](screenshots/personal-leave-dashboard/employee-desktop-dark.png)
- [Employee mobile Light](screenshots/personal-leave-dashboard/employee-mobile-light.png)
- [Mobile Dark activity/inbox](screenshots/personal-leave-dashboard/employee-mobile-dark.png)
- [Pending/upcoming desktop Dark](screenshots/personal-leave-dashboard/request-lists-desktop-dark.png)
- [Configured balance detail Dark](screenshots/personal-leave-dashboard/configured-balance-detail-dark.png)

## Executed build/model/audit checks

Backend HRFlow.sln --no-restore: succeeded, zero warnings/errors.
Frontend build after permission fix: 826 modules, main JS 570.67 kB / 169.38 kB gzip; passed with
existing Vite >500 kB warning. Lint exit 0, existing useAuth.tsx:223 Fast Refresh export warning only.
EF pending-model check: no changes since last migration. No migration/policy/calculator/dependency
change. Solution vulnerability check: no vulnerable packages in all five projects on configured feeds.
Client npm audit: zero vulnerabilities. Temporary browser tooling is not a client dependency.

## Explicit limitations/unexecuted checks

No exhaustive concurrent role/credential revocation versus read-snapshot orchestration; no measured
real-minute polling/hidden-tab duration drill; no full screen-reader/contrast/keyboard matrix; no full onboarding import/HR employee CRUD/monitoring/report/document-scanner regression; no new
backup drill (no schema change); no large-history/load/Linux/deployment verification. Role-preserving
combined removal, long-content name stress and stale credential combinations were not exhaustively rerun.

#85/#22/#75 statutory/cycle-dependent completion stays deferred. #90 remains open; #91/#92 not started.
Implementation phase made no commit, push, PR, merge, deployment or issue closure. See [contract/design](../adr/0019-personal-leave-dashboard.md).

Final review: 21 intended source/documentation/screenshot files. Working-tree diff and new-file
whitespace/UTF-8/known-secret/raw-token/machine-path checks passed. Instruction files byte-identical;
reviewed synthetic PNGs each below 300 kB. Fixture logs contained no known credentials or raw
JWT/invitation URLs. Implementation phase did not stage/commit/push/open a PR. Temporary services stopped; original fixture pointer restored.

## Independent-review finalisation (14 October 2026)

Reviewed the complete source, API contract, documentation and six synthetic screenshots; no further
source defect was found. Confirmed the existing deferred transaction rechecks credentials before the
authoritative authorization/data reads, and the new handler reuses LeaveBalanceCalculator. Confirmed
issue #90 remains open and matches this slice. Updated living status for independent review.

Finalisation checks: intended-file and sensitive-content scans, instruction byte comparison, and
staged whitespace/diff review. Earlier build, audit, API, persisted-row and interactive Chrome results
above are implementation evidence, not finalisation reruns. No source change required another build
or browser run. All listed unexecuted checks and #85/#22/#75 dependencies remain outstanding.
