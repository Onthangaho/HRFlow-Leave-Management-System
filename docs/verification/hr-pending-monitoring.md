# HR Pending monitoring — local verification, 11 November 2026

Branch `feat/paginated-hr-leave-monitoring`, clean fetched main after confirmed merged PR #142,
base `0f18f7cde195d38feeb86080fa6ac0a7aff62c2b`. [Contract/design](../adr/0037-bounded-hr-pending-monitoring.md).
Implementation-phase evidence below predates publication. Commit, push and PR publication are now authorised; no tracker changes, merge, deployment or external-service enablement.
Historical queue/editor reports remain untouched.

## Environment and repeatable commands

Windows x64, Intel i5-1155G7 2.50 GHz, eight logical CPUs, 7.8 GiB reported RAM, Node 24.12.0,
.NET 8; browser tests use installed Edge/Chromium through `HRFLOW_BROWSER_CHANNEL=msedge`.
Disposable private file-backed SQLite with actual migrations/Identity fixtures, isolated ports,
owned-process cleanup, generated private credentials, no real recipients or employee store.

```powershell
# Root; run fixture suites sequentially, not alongside other offline/writer fixtures.
dotnet build HRFlow.sln --no-restore
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~HrMonitoring --logger "console;verbosity=normal"
dotnet test tests/HRFlow.RegressionTests --no-build --logger "console;verbosity=normal"
dotnet list HRFlow.sln package --vulnerable --include-transitive
$env:ConnectionStrings__DefaultConnection='Data Source=:memory:;Foreign Keys=True'
dotnet ef migrations has-pending-model-changes --project src/HRFlow.Infrastructure --startup-project src/HRFlow.Api --no-build
Remove-Item Env:ConnectionStrings__DefaultConnection
# src/HRFlow.Client
npm.cmd run build
npm.cmd run lint
npm.cmd audit
npm.cmd audit --omit=dev
# tests/browser (after backend fixture processes stop)
$env:HRFLOW_BROWSER_CHANNEL='msedge'
npm.cmd run typecheck
npm.cmd audit
npm.cmd audit --omit=dev
npm.cmd test -- hr-monitoring.spec.ts
npm.cmd test
# Optional synthetic screenshots from the retained HR test only:
$env:HRFLOW_MONITORING_SCREENSHOTS='1'
```

Model comparison uses isolated SQLite configuration only for metadata, not EF InMemory transaction
tests. It exited 0 with **no pending model changes**. EF first reported that API host configuration
requires an on-disk store, then continued using the offline design-time factory. This is not evidence
of successful application startup with an in-memory production store. No migration was generated.

## Before/after measurements

`HrMonitoringMeasurementTests`, separate fresh stores with 20 and 2,000 Pending requests, same canonical
synthetic owner and type, tied start dates across 400 days. Setup/migration/seeding/login are excluded
from HTTP timing. Read actual response bytes. First endpoint invocation is separate from five serial
warm calls; it is **not** a guaranteed cold OS/cache run. No CPU isolation, load or production-capacity
claim. Query counts are measured separately through the actual protected handler/context and exclude
bearer-gate/worker queries. Every measured command uses the same DbTransaction reference.

Initial pre-instrumentation baseline: **2/2**, 21.7299 seconds.

- 20: 7,001 bytes/20 rows; first 259.80 ms; warm 32.96,16.66,16.79,16.36,15.21 ms.
- 2,000: 700,001 bytes/2,000 rows; first 306.24 ms; warm 52.51,43.41,39.96,38.29,45.58 ms.

Instrumented baseline, separate **2/2**, 14.2792 seconds, **five protected SELECTs** at both sizes:

- 20: 7,001 bytes/20 rows; first 142.98 ms; warm 24.35,18.97,9.52,13.48,8.39 ms.
- 2,000: 700,001 bytes/2,000 rows; first 202.50 ms; warm 54.74,47.29,52.60,41.05,34.45 ms.

First post-change measurements (measurement cases passed, but overall focused run was 6/8):

- 20: 7,117 bytes/20 rows; first 200.76 ms; warm 15.97,22.16,12.38,8.89,9.19 ms.
- 2,000: 8,870 bytes/25 rows; first 186.36 ms; warm 28.17,26.00,14.53,14.67,14.82 ms.

Separate corrected **8/8**, 48.9008 seconds, **six protected SELECTs** at both sizes:

- 20: 7,117 bytes/20 rows; first 280.85 ms; warm 24.16,20.24,6.56,9.20,14.81 ms.
- 2,000: 8,870 bytes/25 rows; first 410.50 ms; warm 23.97,24.72,8.24,13.71,9.69 ms.

The extra filtered count increases query count by one, independently of rows. Small payload grew due
to the envelope. First-call results include substantial regressions; large warm samples improved
locally but small/warm results were mixed. JIT/cache/scheduling effects were not isolated. These data
prove bounded payload and constant query cardinality, not universal speed or load capacity.

## Failures, corrections and focused coverage

- Initial baseline instrumentation failed to build due to a missing interface import; corrected before
  the instrumented baseline. Later new test code referred to `NotificationEvents` instead of the actual
  `LeaveNotificationEvents`; corrected without changing persistence.
- First focused **6/8**, 41.9911 seconds: the new test put Guid.NewGuid inside an EF ExecuteUpdate
  expression, which SQLite could not translate, and expected 55 rather than 40 requests intersecting
  leap day (15 start on 1 March). A precomputed version constant and correct independent boundary
  expectations fixed the tests. The fixture's workspace path was also corrected to the existing reports
  route before its assertion was reached. No authorization, date or business assertions were relaxed.
- Corrected focused **8/8**, 48.9008 seconds: actual HR-only/combined access, Employee/Manager denial,
  current-role/credential revocation, inactive/pending denial, empty/extreme pages, all filters, escaped
  search, current-department change, leap boundaries and stable ID ties. Independent persisted rows
  reconcile all IDs/counts and the workspace's 57-request total, not page length. Successful real
  approve/reject removes the final row; HR decisions remain forbidden. Requests/snapshots/audits,
  document binding counts and notification event counts are unchanged by reads. List keys are allowlisted.
- First browser **4/5**, 28.915766 seconds: read-only verification SQL used LeaveRequestId rather than
  SupportingDocuments.RequestId. Corrected the inspection only.
- Next browser **4/5**, 80.579687 seconds: 60.089-second timeout at exact label lookup for the select.
  Independent numeric diagnostic **0/1**, 14.152271 seconds, showed exact-label matches 0, accessible
  combobox matches 1 and three selects. The accessible name was correct; use the role/name locator.
  No timeout increase, retry, production permission change or raw DOM/error output.
- Corrected browser **5/5**, 24.453858 seconds: actual HR Apply/Reset, employee/department/type/date
  filters, keyboard paging, actual Manager rejection and final-page clamp, row history links, persisted
  request/audit/binding-count inspection, light desktop/dark 390px mobile. Controlled response barriers
  separately cover supersession, 503/retry, retained drafts, permission-denial hiding and HR/combined/
  Employee replacement logins (same and different account). Safe reporter emits fixed coordinates/counts
  only. Initial diagnostic label text was corrected after this run to retain its distinct label/role counts;
  subsequent full-suite verification is separate.

Solution build passed with zero warnings/errors (latest focused build 2.50 seconds). Client build
includes TypeScript and passed (initial Vite 2.69 seconds; final build after search trimming 7.23 seconds); lint passed with the existing auth fast-refresh
warning. Browser typecheck passed. Client/browser full and production-only npm audits exited 0, and
all six .NET projects report no vulnerable packages. No lockfiles or dependencies changed.

## Complete gates (separate executions)

The complete backend command above passed **228/228**, exit 0, **35.6406 minutes**. This is a
separate execution from the 8/8 focused run. It includes existing activation/recovery, personal history,
Manager decisions/queue, employee editing, submission recovery, document/privacy and transaction gates.
The long duration was observed, not attributed conclusively to contention: a safe mid-run process
inspection found two API hosts and one regression executable, with no observed accumulating API hosts.
No retries, skips or timeout changes were introduced. Browser fixtures were started only after this
backend process exited. Browser typecheck was rerun successfully after the final test-only changes.

Additional monitoring samples inside the complete backend gate (not substituted for the paired
before/after focused measurements):

- 20: 7,117 bytes/20 rows; first 438.16 ms; warm 20.71,19.71,19.26,8.98,11.70 ms.
- 2,000: 8,870 bytes/25 rows; first 351.60 ms; warm 20.72,22.20,12.17,10.69,7.74 ms.
- Six protected SELECTs and one transaction at both sizes.

The subsequent complete default browser command passed **108/108**, exit 0, **582.984532 seconds**,
with retries disabled. This is a separate execution from the earlier focused 5/5 run. It uses the final
test code, including the real HR Overview monitoring link, corrected safe label/role diagnostic and
full-page screenshots. Actual HTTP tests cover filtering/paging, real Manager decisions, personal
submission/history, employee editing, onboarding, reports/exports, lazy routes and bearer refresh.
Controlled transport tests separately exercise error/retry, out-of-order responses and replacement
sessions; they do not prove a real network outage or scanner success. Existing Node SQLite experimental
and auth fast-refresh warnings were not suppressed. Browser screenshots were regenerated in this run
and visually reviewed for layout and synthetic-only contents.
Post-suite process inspection confirmed zero API, regression-fixture and browser-fixture hosts. The
first sandboxed process query was denied and was not used as cleanup evidence; the permitted read-only
inspection succeeded. Existing fixture-owned cleanup remains unchanged.

## Screenshots and limits

[Desktop light](screenshots/hr-monitoring/desktop.png), [mobile dark](screenshots/hr-monitoring/mobile-dark.png).
Opt-in screenshots contain only synthetic names/email/type/date/count information; no credentials,
activation links, document contents or private paths. Normal sensitive screenshots/traces/video remain
disabled. These are layout evidence, not comprehensive accessibility or physical-device testing.

Documentation checks cover 97 relative links/anchors, strict UTF-8, formatting/fences, whitespace,
sensitive-content patterns and PNG metadata; both instruction files are byte-identical. An auxiliary
PowerShell comparison initially failed to parse unsupported generic-call syntax; the corrected byte
comparison passed, with no product changes. These were implementation-phase checks; publication-stage staged checks are recorded separately below.

No hosted, real-ClamAV, production-built
browser, pitch rehearsal, target-host, load or screen-reader result is claimed. Other unbounded reads
and contract/deployment consequences are documented in ADR 0037. #92's calculation-dependent work
and unsigned legal/provider decisions remain outstanding.

## Publication review

The final review includes only the six source files, five retained test files, eight documentation
files and two reviewed synthetic screenshots. No schema, dependency, generated output, private fixture
store or credential is included. Source/test behaviour is unchanged since the separate completed
local gates above; product tests were not rerun merely for publication. Staged whitespace, sensitive
content, instruction equality and documentation checks are rerun before commit. Hosted execution
is pending publication and must be reported separately from local evidence.

## First hosted publication and responsive-readiness follow-up

PR #143, initial commit `5872d5f7b9fcd4eff02cf917509b00f4ea2bbfcd`, hosted Ubuntu
[run 43025894555](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/43025894555):
browser **107 passed / 1 failed**, 212.327186 seconds. Setup/build/audit/typecheck/Chromium installation
passed; the failure was in Browser regression gates, at `hr-monitoring.spec.ts:61:89`, the immediate
no-horizontal-overflow assertion after switching from desktop to mobile. The safe reporter retained
the source coordinate, not raw Playwright call logs or page contents. No actual width values were
recorded, so that first failure alone does not prove whether transient settling or persistent overflow
caused the false result. The independent hosted backend gate passed **228/228**, **15.5822 minutes**. The first workflow therefore failed because of its browser job, not its backend gate. It was allowed to complete before pushing the correction; it was not cancelled to replace the evidence.

The test previously sampled layout once immediately after viewport/media changes. It now uses the
existing bounded Playwright polling assertion to require both the requested 390px viewport and actual
rendered fit. The final no-overflow condition, timeout, permissions and product source are unchanged.
A new controlled test holds an explicit 700px minimum-width barrier, observes a failing fit sample,
asserts readiness has not completed, releases the barrier and requires successful rendered fit. No
sleep, retry, skip or blanket timeout increase was added. This proves the readiness helper waits for
layout, not that the original hosted failure's exact browser timing has been reconstructed.

Separate post-correction local focused browser execution: **6/6**, exit 0, **34.266061 seconds**,
including the actual HTTP workflow and controlled layout/session/error tests. Browser typecheck was
rerun separately. This is not a new local complete-browser or backend pass; the earlier 108/108 and
228/228 executions above remain distinct. The complete hosted suites must run on the correction head.
