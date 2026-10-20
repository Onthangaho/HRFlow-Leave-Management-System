# Client route-loading verification — 20 October 2026

## Scope and source inspection

PR #125 confirmed merged, 20 October 2026 16:45:55 UTC. Clean fetched main base
`65b5143c46db74db0032829b67afe471be39fc5f`; branch `perf/client-route-loading`. Read both
instruction files, planning/readiness, App, shell, Overview/workspaces, account/notification modules,
auth/session/provider boundaries and retained fixtures. Issue search across current/closed titles
found related #77/#76 and dashboard items, no dedicated route-loading issue. No issue was created
or edited and this does not complete #77. [ADR 0028](../adr/0028-deferred-client-features.md).

Baseline App eagerly imported all major routes except reports. Overview eagerly imported three
workspaces. Header imported the inbox/image editor's modules for their badge/avatar. Reports and HR
charts were already split. Now substantial routes and each permitted workspace load on demand;
badge/avatar logic is moved unchanged into small modules. Router, shell, AuthProvider, QueryClient,
role/API guards, original versions, mutations and session generations are not changed.

## Production measurement method

Local Windows x64, Node 24.12.0, .NET runtime 8.0.23/SDK 9.0.305, installed Chrome; Intel i5-1155G7
2.50GHz, 8 logical CPUs, 8 GiB RAM (Node OS inventory; CIM access unavailable). Loopback HTTP, no
network/CPU throttling, headless browser, normal reduced-motion fixture. Fresh migrated file-backed
SQLite, seven synthetic role accounts plus 75 directory profiles, isolated API/storage/ports and
generated private credentials. No existing database or real recipient used. Production build mode
describes the client bundle, not production API deployment.

The opt-in fixture builds into private temporary storage with its API origin configured, then serves
Vite preview. A new context per role gives a cold cache; same-context logout and full protected
Overview re-entry gives a warm cache, not persistent authentication. Start from `/` to avoid retained
Router return-location state. Time includes navigation, automated login and ready workspace UI:
configured balances for personal capability; HR loaded snapshot and chart caption; then network idle.
Secondary routes wait for mounted profile fields, team calendar or idle directory before recording.
These are automation/readiness durations, not paint/LCP or human task-time benchmarks.

Resource Timing reports JS resource entries, `encodedBodySize` and `transferSize`; numbers below are
**bytes**, not raw-source or gzip estimates. Warm validation/cache entries have zero encoded body
but nonzero reported transfer overhead. Different browsers/cache policies can report differently.
Only asset basenames and numeric metrics are written to ignored local output. Hashes vary with the
private API port; default build gzip estimates below are separate evidence.

One cold and one warm sample per role in each final comparison, not a statistical distribution.
The API fixture is shared across roles in Employee/Manager/HR order; browser-cold does not mean
cold OS/disk or a newly restarted API for each role.
The baseline source was temporarily restored from HEAD after preserving every edited source buffer;
the same measurement ran, then a finally block restored all edits. No reset/discard of user work.
Baseline comparison invocation: three passed, **21.047s**. Final split comparison: three passed,
**47.085s**. No other verification process ran during those comparison invocations. The earlier 23.441s split
measurement preceded the final workspace-key correction and is not used for the final CSV.

## Before/after observations

CSV below: initial request-entry count, encoded body bytes, transfer bytes, Overview readiness ms;
then additional JS entries/body bytes/transfer bytes and completed secondary transition ms.
Secondary routes: Employee Profile, Manager Team leave, HR Employee management.

```csv
build,role,cache,initial_js,initial_body,initial_transfer,overview_ms,secondary_js,secondary_body,secondary_transfer,transition_ms
baseline,Employee,cold,1,177661,177961,2161,0,0,0,112
split,Employee,cold,6,142860,144660,4579,3,8485,9385,550
baseline,Employee,warm,1,0,300,1072,0,0,0,89
split,Employee,warm,6,0,1800,1800,3,0,900,534
baseline,Manager,cold,1,177661,177961,1327,0,0,0,105
split,Manager,cold,9,148363,151063,2135,1,271,571,697
baseline,Manager,warm,1,0,300,995,0,0,0,93
split,Manager,warm,9,0,2700,1785,1,0,300,683
baseline,HR,cold,3,280665,281565,1608,0,0,0,433
split,HR,cold,11,250883,254183,2713,3,12593,13493,1118
baseline,HR,warm,3,0,900,1024,0,0,0,172
split,HR,warm,11,0,3300,1768,3,0,900,707
```

Initial cold encoded bodies fell for every role, while request counts grew. All final local
Overview and secondary-transition samples were slower; these isolated samples do not establish
causation or a universal speed improvement. The benefit demonstrated is avoiding unrelated feature bytes initially, with a request/latency trade-off.
Background OS/memory pressure was not controlled; these single timing samples cannot isolate causation.
Real bandwidth, compression, HTTP versions, cache headers, CPU and hosting can change that trade-off.

Employee initial split assets: entry, JSX runtime, auth, Axios, Personal dashboard and problem helper.
Manager adds Manager workspace, export helper and calendar utilities. HR omits Personal/Manager
workspace code and adds HR workspace, report API/helper/calendar and existing HR charts/BarChart.
The underlying calendar utility is shared, not Manager-authorized data. Guards remain authoritative.
Secondary Profile adds AccountPages, schedule and confirmation modules; Team page adds its 271-byte
wrapper because Overview already loaded the calendar/workspace; HR directory adds employee,
schedule and confirmation modules. No unrelated role module was requested in retained role tests.

## Fresh build sizes and import graph

Baseline: main **610.88 kB /179.21 kB gzip**, report page **11.84/3.81**, HR charts **1.73/0.85**,
BarChart **356.49/103.09**. Four JS chunks, about 980.94 kB total raw JS from printed sizes. Fresh split build with manifest: main
**360.37 kB /112.55 kB gzip**, 31 JS chunks, **992,369 total raw JS bytes**.
Total packaged JS grew slightly; this change reduces initial delivery, not the total application footprint. Existing >500 kB Vite warning disappears without
changing its threshold or adding chunk rules. Existing `useAuth.tsx:223` Fast Refresh export warning
remains. No packages changed. Build estimates and preview encoded bytes are not interchangeable.

Final route/shared chunks below use Vite printed decimal kB/raw and gzip estimates:

```text
entry 360.37/112.55; BarChart 356.52/103.10; Axios 50.20/18.85
auth 17.93/6.13; JSX runtime 8.55/3.26; calendar 1.80/0.94; problems 0.60/0.38
Employee management 34.34/9.60; Leave configuration 19.03/5.76
Account pages 17.23/5.48; HR workspace 15.74/4.56; CSV onboarding 12.24/4.29
Leave reports 12.02/3.89; Manager workspace 8.86/2.99; My leave 8.83/2.44
Personal dashboard 8.60/2.83; Approval queue 8.37/3.04; Request leave 8.09/2.89
Timeline 7.24/2.40; Documents 5.61/2.37; Schedules 5.36/2.21
Inbox 4.34/1.76; HR monitoring 3.83/1.29; Activation 3.16/1.38
Change password 2.75/1.27; Team wrapper 0.27/0.22; HR charts 1.74/0.85
Export helper 3.20/1.56; Confirmation 1.57/0.82; report API 0.77/0.39; shared API 3.07/0.77
```

Manifest inspected: entry statically imports JSX runtime and auth; auth shares Axios. Its 17 dynamic
edges are three workspaces plus substantial route modules (Profile/Settings share AccountPages).
HR workspace dynamically imports HR charts; report/chart code shares the BarChart chunk. No bundler
configuration changed. The shared graph is the bundler's output, not a manual vendor-chunk policy.

## Execution history and corrections

Measurement development was not uniformly green: the initial dev-server probe (three passed, 26.227s)
was rejected as production evidence. An early production shell-only probe (three passed, 19.039s)
did not prove dashboard completion. Stricter readiness exposed the warm Router return-location
problem (three failed, 49.699s). Isolated probes failed at 9.974s (invalid test hook), 22.329s and
22.809s (warm destination); safe numeric/status probes identified the fixture issue. Those helper
probes were removed. After protected Overview entry, a three-case baseline passed in 18.492s and
an initial split comparison in 20.630s. The final measurements above additionally require completed
secondary UI and use the final cached-import retry design; earlier numbers are not substituted.

Initial client compilation failed on an unused import after moving the notification hook; corrected.
New lint findings were resolved without suppressions. A first eight-case production test-development
run had three timeouts (wrong profile label), two failures (denial selector and cached module retry)
and three passes, 226.859s. Separate cached-import check failed, 23.343s. A later two-case check had
one denial-selector failure and one successful built-module recovery, 26.854s. The denial text is a
paragraph plus a distinct heading; the shell can still title a denied URL, so assertions now check
the denied heading and absence of the actual onboarding content inside main, not the shell title.
No production permission logic was changed. Correct labels were matched, not assertions skipped.

The first complete production browser invocation (before the helper/selector corrections) had
**25 passed /3 failed**, **272.088s**: the denied-content selector and both stale-refresh cases.
Some controlled old-data cases took about 32s in that run. Their helper had proved only shell presence,
allowing the network barrier to intercept initial dashboard mounting before the intended manual
Refresh action. It now waits for loaded balances and an enabled Refresh button (or the HR loaded
snapshot), including replacement login. No token/session/mutation assertion changed. A subsequent
focused production invocation passed **7/7**, **27.099s**, covering direct denial, mobile dark/keyboard,
HR routes/export and all four old-data/stale-refresh cases. Final complete gates are recorded below.

The subsequent 30-case production invocation had **28 passed /2 failed**, **216.551s**. Both
failures were delayed-module assertions matching a preferred-name marker legitimately saved by an
earlier test and now displayed in the header. Assertions now target the absence of the old profile
controls within main and the correct replacement workspace capabilities. They also await the held
response and actual module evaluation before checking the UI, rather than merely releasing transport.

The expanded 33-case default invocation had **32 passed /1 failed**, **171.709s**. Its Employee+HR
setup selected the intentionally cross-department reporting fixture, unsuitable for a versioned HR
employee update. A valid same-department fixture is reused for that role combination; no reporting
validator is relaxed. The same run exposed pre-existing duplicate sibling keys for Manager and HR
Overview workspaces. Stable capability prefixes now distinguish them without changing normal session
or navigation keys. Vite's client-console forwarding was also discovered; fixture development mode
now disables it and uses silent server logging. Raw browser errors/content are not a diagnostic feed.

## Final gates

- Frozen-source complete default browser invocation: **33 passed /0 failed/skipped**, **174.449s**,
  zero retries. Seven nonempty role combinations, explicit profile saves with persisted own-profile
  checks and unchanged canonical names; one initiating UI login/profile save and the same header node across navigation. Extra role
  combinations use explicit versioned HR setup before those UI counters; they are not production bypasses.
- Frozen-source complete production-built browser invocation: **33 passed /0 failed/skipped**,
  **161.930s**, zero retries, separate from the default run. Each full suite includes 18 real HTTP,
  14 controlled-transport cases and one pure diagnostic-contract case; do not sum executions into
  one test count. All seven combinations, real profile persistence, direct URLs/history, static
  module failure recovery, mobile dark/keyboard, submission/decision/history, onboarding/editing,
  report export and replacement-session gates passed.
- Complete backend invocation: **88 passed /0 failed/skipped**, **15m45s**, real migrated SQLite and
  existing separate-process, recovery, transaction/audit and permission gates. This is one new full
  invocation, not a sum of focused runs. No backend test/source changed.
- Audited locked solution restore/build passed, **0 warnings/errors**, build **21.75s**. Initial model
  command safely refused unspecified connection configuration; rerun with a fresh disposable path:
  **no model changes since last migration**. No database fallback or schema edit.
- Final frontend build (including manifest inspection), lint and browser typecheck passed. A type-only
  generic React inference correction initially failed compilation, then passed with explicit projected
  component return type; emitted runtime hashes matched the previous build before the key correction.
  Final build **2.71s**. Existing Fast Refresh warning remains; no new lint warnings or Vite size warning.
- Client and browser npm audits: **zero vulnerabilities**. Solution vulnerability check including
  transitives: no vulnerable packages reported for all six projects. No dependency/lock-file change.

Some gates overlapped on independent disposable stores and local memory pressure was observed;
regression durations are not performance measurements. The production measurements ran separately.

## Exact final gate commands

```powershell
dotnet restore HRFlow.sln --locked-mode -p:NuGetAuditMode=all -warnaserror:NU1901,NU1902,NU1903,NU1904
dotnet build HRFlow.sln --no-restore
# Set ConnectionStrings__DefaultConnection to a fresh disposable path for this offline check.
dotnet ef migrations has-pending-model-changes --project src/HRFlow.Infrastructure --startup-project src/HRFlow.Api --no-build
dotnet test tests/HRFlow.RegressionTests --no-build --logger "console;verbosity=minimal"
npm.cmd run build --prefix src/HRFlow.Client -- --manifest
npm.cmd run lint --prefix src/HRFlow.Client
npm.cmd --prefix tests/browser run typecheck
npm.cmd audit --prefix src/HRFlow.Client
npm.cmd audit --prefix tests/browser
dotnet list HRFlow.sln package --vulnerable --include-transitive
$env:HRFLOW_BROWSER_CHANNEL='chrome'
Remove-Item Env:HRFLOW_LOADING_MEASURE,Env:HRFLOW_LOADING_OUTPUT,Env:HRFLOW_BROWSER_PRODUCTION -ErrorAction SilentlyContinue
npm.cmd --prefix tests/browser test
$env:HRFLOW_BROWSER_PRODUCTION='1'
npm.cmd --prefix tests/browser test
```

Production measurement uses the same production flag plus `HRFLOW_LOADING_MEASURE=1` and an absolute
ignored `.local-verification/client-loading-<label>.json` output. Clear measurement flags before gates.
Only three measurement cases run with that switch; they are not the default/production 33-case gates.

## Repeatable commands and limits

See [retained test commands](../../tests/README.md) and [tests](../../tests/browser/loading.spec.ts).
All browser runs use zero retries, one worker, existing 60s tests/10s assertions, screenshots/traces/
video off. Module transport cases hold/abort real code requests using explicit barriers and real API
login; they are labelled controlled transport, not independent network outage or deployment drills.
No scanner substitute or positive real-ClamAV claim. No hosted workflow has run for this unpublished
branch. Comprehensive accessibility, mobile hardware, real network/hosting, load/capacity and API
aggregate work remain separate; no legal approval or statutory calculations are enabled.


## Final production probe asset names

These are actual requested basenames from the cold samples, not API paths or credentials.
Hashes differ from the default environment build because the isolated API origin differs.

```json
{
  "employee": {
    "initial": [
      "index-WnRXziT6.js",
      "jsx-runtime-B-hcVAMW.js",
      "axios-Wm-TwCWI.js",
      "useAuth-ChCPdDQl.js",
      "PersonalLeaveDashboard-CPR1dYqc.js",
      "problems-DFWxTRA0.js"
    ],
    "secondary": [
      "AccountPages-CZguOhB9.js",
      "ConfirmationDialog-BVOEp4ny.js",
      "ScheduleWorkspace-CQ6GPV2m.js"
    ]
  },
  "manager": {
    "initial": [
      "index-WnRXziT6.js",
      "jsx-runtime-B-hcVAMW.js",
      "axios-Wm-TwCWI.js",
      "useAuth-ChCPdDQl.js",
      "PersonalLeaveDashboard-CPR1dYqc.js",
      "problems-DFWxTRA0.js",
      "ManagerWorkspace-Dd42iuBa.js",
      "ReportExport-DPuMGp9i.js",
      "calendar-BzhfXpt4.js"
    ],
    "secondary": [
      "TeamLeavePage-BzRYkJ2H.js"
    ]
  },
  "hr": {
    "initial": [
      "index-WnRXziT6.js",
      "jsx-runtime-B-hcVAMW.js",
      "axios-Wm-TwCWI.js",
      "useAuth-ChCPdDQl.js",
      "HrWorkspace-Cm8VzFOD.js",
      "problems-DFWxTRA0.js",
      "ReportExport-DPuMGp9i.js",
      "api-Dxm-EGsq.js",
      "calendar-BzhfXpt4.js",
      "HrCharts-Ca3iS-_Z.js",
      "BarChart-_OtQ1y3D.js"
    ],
    "secondary": [
      "EmployeeManagementPage-DuQTZtzO.js",
      "ConfirmationDialog-BVOEp4ny.js",
      "ScheduleWorkspace-CQ6GPV2m.js"
    ]
  }
}
```

## Final review checks

All 23 intended source/test/documentation files passed strict UTF-8, formatting, bounded sensitive-literal
scans and 98 relative file/anchor checks. Both instruction files are byte-identical. Whitespace and
reverse-application validation of the complete patch, including eight new files, passed. Backend,
provider/auth/session, dependency/lock, schema, CI and legal decision/example files remain unchanged.
Build output, metrics, preserved-source buffers and disposable fixtures are excluded from the diff.
At the implementation evidence cutoff, no files were staged and no publication had occurred.
Finalisation reviewed the complete intended patch against fetched main without additional source
changes. The three complete gate executions above were not rerun during finalisation. Fresh staged
whitespace, sensitive-content, relative-link and instruction-equality checks precede publication.
Hosted results are reported on the PR separately; no issue edit, deployment or statutory implementation is authorised.

Finalisation staged whitespace checking found surplus EOF blank lines in four new files; removed
without changing executable statements. The repeated staged whitespace check passed. Earlier
implementation whitespace evidence did not catch those new-file EOF lines.
