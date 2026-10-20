# Bounded employee-directory verification — 20 October 2026

## Baseline, scope and evidence discipline

PR #122 was confirmed MERGED (15 October 2026 20:32:02 UTC). Clean working tree; fetched main and
created `perf/bounded-employee-directory` at `a7dafadf781a46004dc8c5239f2cf891339a7c10`.
Unrelated worktrees and private artifacts were preserved. Read both instructions, planning/requirements,
readiness findings, directory/form/attention/image code and retained fixture/tests. All issue titles
were searched (open and closed): no dedicated pagination issue was found. This addresses the
[readiness directory finding](../planning/08-current-readiness-review.md), not completion of #77.
No issue was created/edited. Implementation evidence below predates PR publication; finalisation does not include hosting or deployment.

## Repeatable local measurement

Windows x64; Intel Core i5-1155G7 @ 2.50 GHz, 4 cores/8 logical processors, 7.8 GiB physical RAM;
SDK 9.0.305, .NET runtime 8.0.23, Node 24.12.0, local Chrome. No CPU/network throttling or dedicated
benchmark host. Synthetic file-backed SQLite stores use real migrations, seven generated fixture
actors and the same `DirectoryFixtureData` generator before/after. Added profiles have deterministic
names/numbers, Identity/role linkage, confirmed 2020 starts, no schedules or images; the final added
profile has Manager membership. Only the seven fixture actors are used for authentication. No real
employee database or recipient is used.

`DirectoryMeasurementTests` starts fresh API processes/store for each size, logs in as HR, and makes
six serial GETs to `employees?pageSize=25`. Before implementation, the old endpoint ignored paging.
Body bytes are UTF-8 HTTP JSON bytes without requested compression, excluding headers. The first
directory call has unprimed directory query/JIT work, **not a flushed disk/OS cache**; login/readiness
already ran. Five following calls are warm. Fixture/process setup is excluded. Different processes
and background system work cause timing variation; these are observations, not capacity or SLOs.

Before, measured on base product code plus the fixture only:

- **32 profiles:** 32 returned; **22,836 bytes**. First call **153.81 ms**; warm calls
  **23.23, 40.64, 20.48, 19.49, 17.99 ms** (median **20.48 ms**).
- **1,007 profiles:** 1,007 returned; **716,061 bytes**. First call **560.14 ms**; warm calls
  **332.78, 323.58, 442.20, 394.32, 385.04 ms** (median **385.04 ms**).

After, from the final backend invocation using the same generated datasets:

- **32 profiles:** 25 returned; **18,545 bytes**. First call **390.30 ms**; warm calls
  **22.80, 35.55, 16.49, 59.64, 15.10 ms** (median **22.80 ms**).
- **1,007 profiles:** 25 returned; **18,548 bytes**. First call **569.09 ms**; warm calls
  **161.52, 192.96, 189.59, 137.74, 171.68 ms** (median **171.68 ms**).

Large-response reduction is about **97.4%**, with a lower observed warm median. Small-store median
and first-call timings did **not** improve; no universal latency improvement is claimed. An initial
prototype function-wrapped both GUID join keys and worsened warm large-store calls to 1.22–1.39s;
measurement caught this. The corrected allowlisted text-key projection removed that regression.
Another intermediate run hit a keyless-projection null-check translation error; retained tests
caught it before completion. Neither prototype is the reported final implementation.

Database command evidence: a retained DbCommandInterceptor observes **three data commands** for an
unfiltered nonempty page at 1,007 profiles (count/page/batched roles). This excludes auth/credential
checks and transaction commands. Before, source issues one activation and one role query per linked
row plus collection/schedule/actor-name reads; that is a source model, **not a measured SQL trace**.
An explicit department adds a validity read. Query command count is bounded; count/search cost still
depends on dataset cardinality and matching/joins. No index migration was necessary.

Real browser measurement at 1280×720, no row images present:

- Before: **32 mounted rows / 64 directory-image attempts**; **1,007 rows / 2,014 attempts**.
  Counts include started/cancelled/refetched requests under Development StrictMode, not successful
  downloads. One initial large baseline interaction timed out; a fresh rerun completed and produced
  the reported count. Absence probes are work even when responses are 404.
- After: **25 mounted rows / 0 directory-image attempts** for both 32- and 1,007-profile stores. Fresh separate browser invocations passed in 15.195s and 18.158s including fixture startup/teardown; these are not page-load timings. Own header-image requests are excluded.

## Retained tests and actual workflows

New backend cases exercise stable pages/counts, duplicate-name ID ties, empty/out-of-range pages,
ASCII employee-number search plus department/status, cross-page attention and expired invitation
semantics, assigned-manager detail and Manager choices beyond the first page. Additional Manager
memberships exercise independently paged choices. Real HTTP denies Employee/Manager, revoked HR,
Pending and inactive accounts; invalid paging/search/department/attention/sort/status returns 400.
Persisted users/memberships/request/audit counts and original detail versions are inspected; read
queries do not call SaveChanges. This does not claim a byte-level forensic comparison of every table.

The browser fixture now has 75 extra synthetic directory rows by default, so existing stale-form
and optional image browser callers explicitly search instead of assuming the target is on page 1.
No diagnostic API, production bypass or scanner substitute is added. Default screenshots/traces/video
remain off; only opt-in reviewed synthetic directory screenshots are retained.

New browser cases cover:

- Real HTTP paging and draft-versus-applied search, original edit draft across list refresh, actual
  HR Overview attention link, keyboard filter traversal and mobile page-width check.
- Real HR create → independent Manager search beyond the first directory page → original Preserve
  edit → resend → schedule append → deactivate → inactive directory result, plus API inspection of
  persisted Manager, lifecycle and schedule history.
- Real CSV upload/map/preview/confirm → bounded directory search, with no passwords in the CSV.
- Real HTTP deactivation of the last filtered page followed by explicit refresh: count shrinks,
  out-of-range page clamps, and inactive totals are checked.
- Controlled transport image presence/content: only viewport rows request images, URLs are revoked
  across page/permission changes, all created URLs are reclaimed, and 403 hides cached protected data.
  This verifies UI transport/lifecycle, **not** malware scanning or server image authorization.
- Controlled held directory responses across logout and same/different-account replacement; no old
  marker/rows are displayed. Existing default tests separately retain stale refresh and non-replay
  boundaries plus real leave submission/decision/history and read-only HR monitoring.

Early focused browser runs exposed test navigation/selector assumptions (full reload loses the
intentionally in-memory session; Manager checkbox versus select; attention section versus combobox).
Explicit filter names and precise role locators resolve them; failed runs are not counted as passes.
The default 9 existing tests and new cases are executed together in the final invocation, not an
aggregate invented from historical runs.

## Commands executed

```powershell
dotnet build HRFlow.sln --no-restore
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~EmployeeDirectoryTests
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~DirectoryMeasurement --logger 'console;verbosity=normal'
dotnet test tests/HRFlow.RegressionTests --no-build --logger 'console;verbosity=normal'
npm.cmd run build --prefix src/HRFlow.Client
npm.cmd run lint --prefix src/HRFlow.Client
npm.cmd --prefix tests/browser run typecheck
$env:HRFLOW_BROWSER_CHANNEL='chrome'
npm.cmd --prefix tests/browser test
dotnet ef migrations has-pending-model-changes --project src/HRFlow.Infrastructure --startup-project src/HRFlow.Api --no-build
dotnet list HRFlow.sln package --vulnerable --include-transitive
npm.cmd audit --prefix src/HRFlow.Client
npm.cmd audit --prefix tests/browser
git diff --check
```

The model check used a fresh temporary connection setting, not an existing store. For repeatable
image-attempt measurements, set `HRFLOW_DIRECTORY_FIXTURE_ROWS` to `25` then `1000` and optionally
`HRFLOW_DIRECTORY_MEASURE=after`; run only `directory-measure.spec.ts`. Optional count-only JSON
outputs go to the existing ignored `.local-verification` folder; raw fixture state is never copied.
Remove these environment overrides before the default full suite. Set `HRFLOW_DIRECTORY_SCREENSHOTS=1`
only for the reviewed synthetic capture. No CI artifact upload is added.

## Final executions and screenshots

Final solution build: **0 warnings / 0 errors**, 13.54s. Final complete backend invocation: **87 passed / 0 failed / 0 skipped**, about **5.55 minutes**. Final frozen-source combined browser invocation: **17 passed**, **68.690s**, with no retries/skips. The 17 include nine existing gates plus eight new measurement/directory cases; real HTTP versus controlled transport is identified in test titles. These are new local runs, not previous PR evidence. Initial complete browser invocation
passed **16 checks in 78.295s** before the added different-account directory case and final capture
framing; it is earlier evidence, not the final combined browser run.

Frontend build/lint/typecheck passed. Final main bundle **610.88 kB / 179.21 kB gzip**, versus the
recorded base **605.66 / 177.94 kB**. Existing >500 kB Vite warning and `useAuth.tsx:223` Fast Refresh
export warning remain; this is not an unrelated route-splitting effort. Model check: no pending
changes. Solution transitive audit: no vulnerable packages for all six projects; client and browser
npm audits: zero vulnerabilities. No dependency, lockfile or migration changes.

- [Desktop light directory](screenshots/bounded-directory/desktop.png)
- [Mobile dark results](screenshots/bounded-directory/mobile-dark.png)

## Limits

At the implementation evidence cutoff, no hosted CI had run for this branch. PR finalisation will inspect the new hosted run separately; its actual result belongs to the PR, not an inferred local pass. Earlier #122 Ubuntu
82/backend and 9/Chromium are historical, not evidence for this change. Local Chrome is not hosted
Linux/Chromium. Positive real-ClamAV image browser cases were not rerun; their updated HR-directory
selector is typechecked only. Default backend gates retain image processing, fail-closed scanner,
publication/recovery and transaction cases; that does not prove all document/scanner recovery.

No image-bearing large-directory latency measurement: the controlled PNG verifies viewport/URL behaviour only. No realistic concurrency/load, slow/mobile hardware, exhaustive accessibility/assistive technology
or cross-platform permission rehearsal. No staffing capacity, production/legal compliance or statutory
calculation claim. Header/Profile real scanner workflows are not reimplemented or certified here.
HR workspace employee/schedule/activation aggregates, department/role choices, individual schedule
history, owner approved-history balance inputs and bounded-date team/report materialization remain
unbounded by row count. They require separate measurement; this slice does not silently optimise them.
Existing exports bound output, not all upstream aggregate work.

See [API/design decisions](../adr/0027-bounded-employee-directory.md) and [retained test commands](../../tests/README.md).

## Final review

Reviewed the final 1280×1000 light and 390×844 dark screenshots: synthetic names/emails only, no passwords, tokens, invitation links, document contents or machine paths. Desktop is 116,706 bytes; mobile 49,667 bytes. Reviewed all collection/cache callers, including deactivation reload, schedule/resend and onboarding invalidation. Both instruction copies are byte-identical. Relative-link, whitespace/new-file and sensitive-literal/artifact checks are recorded after the final diff review. No hosted/platform/scanner results are inferred from this local run.

Final file review checked **35 intended files**, strict UTF-8 text and **149 local link targets** with no missing files. Tracked/new-file whitespace checks passed and instruction hashes matched. Sensitive-literal/artifact scans found no unexpected databases, private files, credentials, logs or generated output. These bounded scans supplement source/screenshot review; they are not a universal secret-detection claim. Finalisation stages only these intended files after review; no product checks were rerun merely for these documentation tense corrections. Separate page requests take fresh snapshots, so concurrent edits may move rows between pages; stable sorting is not a multi-request frozen export.


## PR finalisation coverage

Source/API/caller and screenshot review found no further executable change. Documentation checks and staged whitespace, sensitive-content and instruction-equality checks are rerun during finalisation. The recorded 87 backend and 17 browser passes above are implementation executions, not new finalisation reruns. The unchanged hosted workflow runs the complete backend suite and all 17 default browser tests (eight real-HTTP, nine controlled-transport cases). Separate 32/1,007-row browser measurements, opt-in screenshots and positive real-ClamAV browser tests remain local-only commands; hosted defaults do not execute those variants.
