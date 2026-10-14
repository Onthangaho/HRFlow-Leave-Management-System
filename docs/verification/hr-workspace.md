# Current-data HR workspace verification

14 October 2026. Clean branch feat/hr-workspace from fetched origin/main
5a87f31115935051c77597e829058d3517a9f454; PR #114 confirmed MERGED. #92/#85/#22/#75 inspected.
Independent synthetic store/private-object copy; original stores/unrelated work preserved.
No product feature beyond this #92 slice, no schema/dependency changes, staging/commit/push/PR/deploy.

## Executed real API and SQLite checks

Two local TLS API processes shared the disposable store during interaction/session checks.
- HR-only/combined HR 200; Employee/Manager-only 403. Revoked HR 403 using old token; inactive and
  Pending activation HR 401. Combined HR removal denied workspace while personal/Manager endpoints
  still returned 200. Only disposable flags/memberships restored for continued verification.
- Period report DTO exactly equals existing department endpoint. Independent SQL reconciled clipped
  Approved request-days, distinct employee IDs and inactive contributions, including overlaps and a
  request spanning multiple months. Filtered totals equal each department breakdown; zero-result
  departments retained. Operational counts independent of period/department filter.
- Leap Approved request 28 Feb-2 Mar 2028: four inclusive days, bucket days 2+2; request counts 1+0.
  Bucket Pending/Approved counts, days and inactive days reconcile to report totals. 366 dates accepted;
  367/missing/reversed rejected 400; unknown department 404.
- Real future schedule revision: active confirmed starter flagged missing-current/future-only; directory
  flags match Overview counts. Future-start profiles excluded from applicable schedule attention.
  Null legacy facts remain Unknown. Inactive history contributes to report but not active input attention.
- Real HR creation made passwordless Pending activation accounts. DeliveryFailed/PendingDelivery
  values were deliberately assigned only in synthetic Identity rows to test aggregation/filtering:
  this does NOT claim a new external delivery failure drill. Pending activation, active lifecycle and
  current safe delivery counts independently matched directory records.
- Actual CSV preview/partial confirmation: one Created, one remaining Ready and one Invalid row.
  Created account count increased once; Created row excluded from unfinished attention. Other HR's
  review omitted and direct read 404. A fresh Ready/Invalid preview was checked before/after: no accounts
  created; persisted counts appeared in the real workspace. Expired/cancelled reviews excluded by rule.
- Real HR department reassignment moved historical current attribution; original decisions/audits not
  rewritten. Reporting assignment restored only for further synthetic workflows.
- Final compiled-backend check after shared-rule extraction: report equality, directory attention,
  SQL active/Pending counts and five-row preview, integrity_check ok, foreign_key_check empty.
- Summary field checks excluded raw reasons/notes/descriptions/phone/security/activation metadata.

Fixture construction: existing submission rejects past starts. Newly created disposable Pending
records had boundary dates set directly BEFORE actual approval to exercise ongoing leave. No original
history was rewritten. Delivery-state injection as above is explicit. Temporary preview harness
expected 200 instead of existing 201; corrected continuation passed. Early extraction syntax/constant
build errors were fixed; only successful final builds are reported below.

## Executed real Chrome workflows

Temporary isolated Playwright controlled installed Chrome against the REAL TLS client/API, not
static rendering. Synthetic records only; product dependencies unchanged.
- HR-only administrative Overview, no personal/Manager panels. Combined role composes all three
  capabilities with one Overview navigation entry. Employee/Manager-only no HR panel, Manager direct
  HR report route denied. Existing personal/Manager Overview remained usable.
- Real period Apply, draft changes retain old applied heading, Reset uses available server SAST month.
  Valid empty period announces confirmed zeros. Injected 503 retains filters/applied data with explicit
  stale warning; real Retry recovers. Revoked HR hides cached operations/report/chart data.
- Future-only schedule/delivery-failed links open actual directory filters. Existing Edit saved a
  canonical synthetic name without changing manager. Actual New Employee form persisted active
  profile + Pending activation + null PasswordHash; no initial password supplied. Overview refreshed.
- Own import deep link displayed real Created/Ready/Invalid outcomes. Delayed linked read announces
  loading and disables competing upload controls, then loads owned rows. Unknown directory filter
  defaults to All. Existing monitoring/report links worked. Actual policy form created a record,
  inspected in SQLite and visible in policy cards.
- Held real HR response after different-account login exposes no HR panel. Held old active count after
  same-account relogin could not replace the newer count after HR creation through the second API.
- Exact trend/department table cells match real API data in BOTH themes. SVG label fills verified
  Light rgb(17,24,39), Dark rgb(241,245,249); actual tooltips and chart application focus/ArrowRight.
  Screenshot review found initial invalid theme token; corrected to established foreground/grid/bar/
  hover tokens, rebuilt and reran real chart checks and screenshots.
- Keyboard Tab through native date segments to next input; desktop 1366/tablet 768/mobile 390,
  Light/Dark, no page-level overflow; reduced-motion setting and no chart animations.

Temporary browser corrections: waited for the route before selector lookup, used actual named Edit
buttons/policy card headings, selected Employee role explicitly, accounted for native date segment
Tab stops, and scrolled lazy charts into view/used actual SVG labels. These corrected tooling runs
are not presented as passes until the focused real workflows succeeded. Source selector labels,
dark chart contrast and saved-review loading protections were improved and affected checks rerun.

## Actual build/model/audit/diff checks

- Final backend HRFlow.sln --no-restore: 0 warnings/errors. After moving shared EmploymentAttention,
  restarted disposable API and reran the compiled report/attention/authorization checks above.
- Final frontend build: 830 modules; main JS 594.70 kB /174.79 kB gzip (base #91: 574.97/170.38).
  HrCharts lazy chunk 1.73/0.84; shared BarChart 356.49/103.08. Existing Vite >500 kB warning remains.
- Final lint exit 0; existing useAuth.tsx:223 Fast Refresh warning only. Local npm.ps1 execution policy
  blocked one attempted check; npm.cmd completed builds/lint without altering execution policy.
- EF pending-model check: no changes since last migration, explicit disposable connection.
- Solution vulnerability check: no vulnerable packages in all five projects on configured feeds.
  Client npm audit: zero vulnerabilities in all categories. No manifest/lockfile/package changes.
- Complete working/new-file whitespace, UTF-8 and known-secret/token/path scans; instruction byte
  equality and screenshot-size/privacy review. Temporary tooling/data/logs excluded from review patch.

## Synthetic screenshots

Capture-time actual records; counts/expiry may differ after later mutations or preview expiry.
Component screenshots show real operational/detail charts; mobile Overview captures the top/filters.
No credentials/tokens/invitation URLs/private phone/real personal data/machine paths.
- [Overview desktop Light](screenshots/hr-workspace/overview-desktop-light.png) / [Dark](screenshots/hr-workspace/overview-desktop-dark.png)
- [Operations desktop Light](screenshots/hr-workspace/operations-desktop-light.png) / [Dark](screenshots/hr-workspace/operations-desktop-dark.png)
- [Attention desktop Light](screenshots/hr-workspace/attention-desktop-light.png) / [Dark](screenshots/hr-workspace/attention-desktop-dark.png)
- [Charts desktop Light](screenshots/hr-workspace/charts-desktop-light.png) / [Dark](screenshots/hr-workspace/charts-desktop-dark.png)
- [Overview mobile Light](screenshots/hr-workspace/overview-mobile-light.png) / [Dark](screenshots/hr-workspace/overview-mobile-dark.png)
- [Trend mobile Light](screenshots/hr-workspace/trend-mobile-light.png) / [Dark](screenshots/hr-workspace/trend-mobile-dark.png)

## Explicit unexecuted limits

No exhaustive concurrent role/credential revocation-versus-snapshot scheduling, measured real-minute/
hidden-tab polling, large-organisation/load/Linux/deployment, full screen-reader/contrast/keyboard
matrix, new backup drill, full deactivation/resend/delivery/scanner/leave-decision/cancellation regression.
Delivery-state aggregation was tested with synthetic persisted states, not a new outage/retry drill.
An already-authorized snapshot may finish in its valid earlier serial order; no instant remote-tab
revocation/update claim. Initial month depends on browser SAST clock until server data; Reset/attention
use server business date. Attention reads all profiles/live own reviews, with per-active-account
activation lookup; response limits do not constitute a load guarantee. Statutory/cycle/holiday work
remains #85/#22/#75-dependent. #92 stays open. Implementation verification did not commit, push, open a PR, merge, deploy or close issues.
[Full API/metric contract](../adr/0021-current-data-hr-workspace.md).

Final review: 42 intended source/documentation/screenshot files; complete patch includes new files.
Instruction copies byte-identical; all twelve reviewed PNGs below 300 kB. Working/new-file
whitespace, UTF-8, known-secret/raw-token/machine-path checks and fixture-log scans passed.
Fixture APIs/client stopped and original fixture pointer restored; nothing was staged or committed during implementation verification.

## Independent-review finalisation

Reviewed the complete source/documentation/new-file diff against #92. No additional product changes were required. Earlier build, API, SQLite and browser results above are implementation evidence, not newly rerun checks. Finalisation reran working and staged whitespace checks, instruction byte comparison, intended-file checks and sensitive-content scans. Only the 42 intended files are included; temporary fixtures, tooling and generated output are excluded. The outstanding checks above remain unexecuted.
