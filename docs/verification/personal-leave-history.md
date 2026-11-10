# Personal leave history — local verification, 10 November 2026

Branch `feat/paginated-personal-leave-history` starts from fetched `origin/main`
`5e716e88d3ba29805e23c5fddb442859f16ba735` after confirmed merged PR #139.
[ADR/API contract](../adr/0035-paginated-personal-leave-history.md). This is local implementation,
not hosted CI, production capacity or statutory verification. Publication for independent review is authorised; no merge or deployment.

## Measurements

Disposable migrated file-backed SQLite, synthetic owner with 20 or 2,000 requests, one Submit audit
per request, repeated tied dates, same fixture-generation method before/after. Fixture setup and login
are outside the timed HTTP operation. One fresh API fixture per dataset; first endpoint call then
five serial warm calls, full response bytes read. "First" is not a cold OS/database cache guarantee:
login/startup have already touched the store. Windows x64, Intel i5-1155G7 @2.50GHz, eight logical CPUs,
approximately 8 GiB RAM, Node 24.12.0. No throttling, load simulation or CPU isolation. CIM hardware lookup was
permission-denied; hardware was read through Node `os`. Reproducible command from root:

```powershell
dotnet test tests/HRFlow.RegressionTests --no-restore --filter FullyQualifiedName~PersonalHistoryMeasurementTests --logger "console;verbosity=normal"
```

Separate baseline execution, **2/2 passed**, 16.8572 seconds, before changing the endpoint binary:

- 20 requests: 20 returned / **7,257 bytes**; first **125.15 ms**; warm **11.16, 10.48, 5.86, 5.89, 8.05 ms**.
- 2,000 requests: 2,000 returned / **725,790 bytes**; first **192.91 ms**; warm **50.74, 51.54, 65.67, 49.97, 49.83 ms**.

First post-change sample, part of a separate **7/7** focused execution (59.8122 seconds):

- 20 requests: 20 returned / **7,372 bytes**; first **450.10 ms**; warm **29.01, 25.49, 17.72, 24.42, 21.69 ms**.
- 2,000 requests: 25 returned / **9,194 bytes**; first **214.33 ms**; warm **17.38, 18.55, 10.32, 10.77, 10.39 ms**.

The larger response is bounded and substantially smaller; warm larger-store samples improved here.
**First-call and small-store timings regressed.** Added count/envelope overhead, runtime compilation,
cache state and machine scheduling are possible influences, not isolated proven causes. This does
not establish universal speed improvement or capacity. Later repeated measurements remain separate
executions and do not replace these samples.

The handler interceptor measured **six SELECTs**, all inside the transaction, at both 20 and 2,000
requests. This includes its real credential/current-role checks, count and page/audits, but excludes
HTTP bearer validation and unrelated worker SQL. Baseline SQL count was not instrumented; no measured
before/after query-count reduction is claimed. Count still visits all matching history. Existing
balances and other queue/report projections retain their own full-history reads; they are not silently
limited to this page. No new schema/index or dependencies.

## Executed regressions and earlier failures

- Initial backend build failed with two incomplete contract-reference errors; initial client build
  found stale array/query-function references. Corrected rather than suppressing types.
- A subsequent test build found three references to a misnamed fixture helper; corrected to the
  existing `PendingAsync`. No production bypass or diagnostic HTTP endpoint.
- Initial focused backend+measurement: **7/7 passed**, 59.8122 seconds. Includes real HTTP scope,
  filter/order/boundary checks, unchanged persisted audits, two-scale protected command counts and
  filtered final-page cancellation.
- Expanded focused backend execution added revoked credential and actual submission/outcome recovery.
  Its credential test failed because EF/SQLite cannot translate `Guid.NewGuid()` inside ExecuteUpdate;
  generating the value before the query corrects the fixture. Complete result recorded below.
- Initial focused browser: **1 pass / 1 failure / 2 timeouts**, **144.668908 seconds**. Two cases could
  not locate an exactly labelled select; HR replacement also hit an incorrect final-route assumption.
- Independent label probe: **0/1 passed**, **10.320451 seconds**, safe diagnostics `exact=0 selects=3`.
  Confirmed wrapping-select labels included option text; explicit `htmlFor`/`id` associations replace
  them. Login test setup now explicitly opens Overview and waits for its real role projection.
- Corrected focused browser: **5/5 passed**, **76.964928 seconds**, before the subsequent enhancement
  that retains cancellation confirmation across a background-error/retry cycle. Not final-source proof
  of that enhancement; final complete execution is recorded separately below.
- A root-context npm test command failed before discovery (no root package.json). A .NET audit command
  rejected unsupported `--no-restore`; the corrected command ran. These are command errors, not passes.

No retries/skips/time-limit increases or relaxed assertions were introduced. Browser source diagnostics
expose fixed filename/coordinates and allowlisted numeric control counts only; raw call logs, payloads,
screenshots/traces/video remain disabled. Tests use generated private credentials, disposable stores,
owned-process shutdown and strict existing permissions. No actual descriptions/files/tokens are retained.

## Coverage and current checks

Backend filters exercise empty, 55-row multi-page and tied dates; all four statuses; leap-day inclusive
intersection; unavailable types; invalid status/date/GUID/page/size/extreme integer inputs; owner scope
with ignored overposted employeeId; Employee/Manager/combined/HR-only; role revocation, inactive,
pending-activation and stale credentials. Real cancellation preserves exactly one extra Cancel audit.
Successful keyed submission and read-only durable recovery appear once in filtered history. Reads do
not add outcomes, notifications or transitions. See the separate complete migration/model and broader regression execution below.

Browser checks use actual HTTP submissions and cancellation with independently read timelines, Apply
versus drafts, paging, mobile width/dark/reduced-motion, keyboard Reset, retained confirmation through
refresh and controlled 503/retry. Controlled held-response tests cover out-of-order filters and same-/
different-account replacement. They are transport barriers, not claims of packet-loss hardware testing.
The complete retained workflows additionally exercise submission recovery, evidence/history links,
Manager decisions, HR workflows, real bearer refresh and non-replayable writes.

All four explicit npm full/production-only audits (client and browser) reported **0 vulnerabilities**.
`dotnet list HRFlow.sln package --vulnerable --include-transitive` reported no vulnerable packages in
all six projects. Manifests/lockfiles remain unchanged. Frontend build, lint and browser typecheck
passed; existing auth fast-refresh, Vite plugin-timing and Node experimental SQLite warnings remain.
Solution build before the final test-only additions passed **0 warnings/errors**, **12.04 seconds**.
Final-source builds/gates and documentation checks are recorded separately below.

## Limitations

No hosted CI, positive real-ClamAV, production-built browser, physical-device/screen-reader or realistic
load verification for this slice. Current calendar-day charging, Medical privacy, immutable snapshots
and submission-recovery guarantees remain unchanged. No database migration or downgrade operation is
needed: rollback concerns are API/client deployment compatibility, not lost records. Offset paging
can shift between independent snapshots when records change; it is not a cross-page export snapshot.
No standalone cancellation dialog redesign, bound-evidence removal, retention/payment/legal rule,
provider or external-service change. Historical reports remain dated evidence.

## Subsequent executions and final-source changes

The expanded focused backend run finished **7 passed / 1 failed / 8 total**, **3.1784 minutes**.
Its only failure was the test-side Guid generation inside ExecuteUpdate described above. Separate
corrected scope/revocation execution: **1/1 passed**, reported test duration **31 seconds**. Final
solution build: **0 warnings / 0 errors**, **9.30 seconds**. Added max-size/all-status assertions
are included in the subsequent complete gate, not retrospectively attributed to that isolated pass.

That expanded run's independent measurement samples were noisier:

- 2,000 requests: 25 rows / 9,195 bytes; first 825.31 ms; warm 47.48, 59.43, 32.22, 44.85, 63.58 ms.
- 20 requests: 20 rows / 7,373 bytes; first 809.60 ms; warm 43.63, 47.43, 23.50, 66.73, 23.25 ms.

The warm improvement was not consistent across repeated executions. No statistical or production
capacity inference follows. One-byte response variation was observed; its exact source was not isolated. Row and payload bounds
are the reliable result, not exact byte-for-byte reproducibility.

After the 5/5 browser run, page-level confirmation retention was extended through background
503/retry. Pagination buttons remain mounted while queries load, and returned page clamps become
canonical client page state without overwriting a newer filter change. The 26-request browser case
uses a separate synthetic owner to avoid pushing later Employee workflow records off their first
page; no records are deleted to obtain green tests. Held transport has a bounded fetch and cleanup
releases only owned barriers. Final browser typecheck, frontend build and lint passed; final Vite
build time **7.42 seconds**, with the existing warnings listed above. The final complete browser
execution must cover these changes; the preceding 5/5 run alone does not.

## Complete final-source backend execution

With browser fixture writers stopped, `dotnet test tests/HRFlow.RegressionTests --no-restore -v minimal`
passed **204/204**, **0 failed / 0 skipped**, reported duration **36 minutes 43 seconds**. This single
complete execution includes the final history scope, recovery and max-size/status assertions plus
retained migration/model, transaction, recovery and authorization gates. It is separate from the
7/7, failed 7/8 and corrected isolated 1/1 executions above. No failures were suppressed or retried.

This run was materially slower than earlier dated backend runs. Numeric process inspection during
the quiet minimal logger showed fresh API fixture turnover, not an established stall. The cause of
the longer runtime was not isolated; no contention diagnosis is claimed. At completion there were
no API fixture or testhost processes. Only then was the complete browser invocation started.

## Complete-browser investigation and caller migration

The first complete default-browser invocation finished **84 passed / 4 failed**, **511.368306 seconds**.
Four existing evidence cases failed at their shared helper's request-link click (`evidence.spec.ts:61`);
raw assertions/call logs were intentionally withheld. That invocation also overlapped a test-only
canonical-fixture-type selection edit, so it is not claimed as a frozen final-source execution.
No product source changed during it. The five new history cases passed in that run, including real
filtered final-page cancellation and confirmation recovery through a controlled background error.

A separately executed diagnostic isolation of one failed evidence case passed **1/1**, **59.956903
seconds**; three bounded diagnostic repeats passed **3/3**, **43.026396 seconds**. All showed safe
numeric state `matches=0 headings=0` before the old helper's link action. The original four failures
were **not reproduced** and their precise cause remains uncertain. Do not label later green as proof
of their original root cause or assume resource contention.

Source inspection and those observations confirm a separate readiness gap: the helper clicked My
Leave, then searched all hrefs without awaiting history rendering or its data. Overview legitimately
contains more than one link to the same request. The helper now registers a wait for the specific
successful history GET **before navigation**, waits for history heading/idle state, then applies the
request fixture's bounded UTC-date period through the real UI and waits for that response/idle state.
The original requested-ID click and all binding/download/privacy assertions remain intact. No direct
URL reload, unbounded API fallback, timeout increase, retry or skip was added.

A new retained controlled module barrier holds the My Leave feature while Overview contains multiple
request links; the helper must stay unsettled until release and then reach the authorised evidence
editor. This proves the corrected synchronization, not reconstruction of the earlier four failures.
Diagnostics retain only fixed source coordinates, allowlisted numeric match/heading counts and a fixed
ambiguous-target category; no raw errors, URLs, IDs, page content or messages are printed.

After correction, the two affected browser files passed **15/15**, **90.145066 seconds**, zero retries.
Every evidence helper diagnostic then showed `matches=1 headings=1`. Final complete browser verification
is a separate frozen-source execution, recorded below. Backend source/tests were unchanged after the
204/204 run; no further backend rerun was justified by this browser-helper-only correction.

## Final frozen complete-browser execution and review checks

From `tests/browser`, `$env:HRFLOW_BROWSER_CHANNEL='msedge'; npm.cmd test` passed **89/89**,
**0 failed / 0 skipped / 0 retries**, **323.432737 seconds**, in one complete execution. No executable
file changed during this run. It is separate from the preceding 84/4 complete invocation, 15/15
focused correction, 3/3 diagnostic repeat and earlier 5/5 run. The final backend and browser gates ran
sequentially, not as a single aggregate execution.

The final run included real Employee submission → Manager decision → owner history, HR monitoring,
submission-recovery navigation, supplemental evidence binding/history/download, applied filters,
final-page cancellation, background-error draft recovery, delayed query/module/session barriers,
mobile/theme/keyboard checks, real bearer refresh and non-replayable writes. Scanner-unavailable
rejection and controlled clean metadata are not successful real scanning. At completion numeric
process inspection found **0 API fixture processes / 0 browser fixture hosts**.

Final browser typecheck passed before the affected execution; final frontend build/lint passed after
all product changes. No further product changes followed. The full backend run includes retained
`HasPendingModelChanges` and migration-preservation assertions; no schema/migration/dependency/CI
files changed. Final UTF-8, formatting/whitespace, relative link/anchor, sensitive-pattern and byte-
identical instruction checks passed. The review diff includes new files; private fixtures, downloaded
blobs, credentials and generated outputs are excluded. At that implementation checkpoint no commit, push, PR or tracker change had occurred.

The comparison runner's historical-array/new-envelope discrimination is test-only measurement support,
not a production compatibility fallback. External consumers of the old array must migrate with the
API/client rollout. Remaining verification and original-failure uncertainty remain as listed above.

## Independent-review finalisation

Complete tracked caller inspection includes scripts and pitch tooling, not only default `rg` results.
The SPA hook and HTTP tests consume the envelope; existing navigation-only consumers use the migrated
UI. The pitch and opt-in real-scanner tools do not decode the history array, and their fresh owners
have one target request on the first page. They were inspected, not rerun. See the ADR caller inventory.
No executable correction was required during this review; the 204-backend and 89-browser runs above
remain separate earlier local executions, not finalisation reruns. Documentation records publication
authorisation and the coordinated breaking API/client rollout. Hosted results will be recorded only
after this branch is published and its own workflow completes.
