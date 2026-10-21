# HR workspace query efficiency: local verification

21 October 2026, branch perf/hr-workspace-attention-queries from fetched main
b72a5274c1d7365a350dfa9fb5c11095b94ffd28 (PR #130 confirmed merged). Clean initial tree;
unrelated work preserved. At implementation handoff no commit, push, PR or issue edit had occurred. Related scope is #92/readiness findings,
not a new or completed product issue. [Design and contract](../adr/0030-hr-workforce-attention-aggregation.md).

## Measurement method

Windows x64, Intel Core i5-1155G7 2.50GHz, eight logical CPUs, 7.8GiB RAM; SDK 9.0.305,
.NET runtime 8.0.23, Node 24.12.0, installed Chrome for browser checks. CIM hardware inspection was
permission-denied; CPU/memory above were obtained from Node os, without device/user identifiers.
No throttling, isolated-machine guarantee, load test or production-capacity claim.

Each trial creates a NEW migrated private file-backed SQLite store and two real API processes. Same
logical generator/relationships/cardinalities before/after, with fresh random IDs/credentials rather
than byte-identical physical files. Stores contain 32 or 1,007 employees, two departments and 14 or 500
schedule revisions. Seven real synthetic Identity fixture actors support login; bulk projection fixtures
use explicit RequiresActivation/delivery flags, not actual mailbox delivery or claims all bulk accounts
can log in. Eight repeating cases cover current/future schedules, unknown/future starts, activation,
Accepted/PickupReady/failed/unknown/pending outcomes, expiry and inactive employment. No leave/import
rows are added to the timing fixtures: these measurements isolate workforce attention, not worst-case
report volume. Additional populated-report regression verifies preserved leave behaviour separately.

Setup/migrations/seeding/login are outside the timed section. Six serial HTTP requests per size/trial:
first endpoint invocation in its fresh process, then five warm invocations, including response read.
First invocation includes application/JIT/query/serialization costs; it is NOT an OS-cold-cache measure.
HTTP includes the normal bearer gate. SQL commands are measured separately using the production handler,
real scoped authorization/credential services and an EF interceptor on the same context. Counts include
handler credential/HR checks and projection reads, exclude the separate HTTP bearer gate, setup,
reference reconciliation and provider-internal connection pragmas. No diagnostic HTTP endpoint added.

Corrected baseline commands: 60 for 32 employees; 1,645 for 1,007. Optimized commands: 12 for BOTH sizes,
retained as executable assertions. Response shape unchanged; comparison samples have only tiny
timestamp-fraction byte variation: approximately 3,396-3,397 bytes small and 3,412-3,413 large, not a payload-reduction claim.
[All numeric samples](hr-workspace-query-measurements.json) include the initial invalid-counter run and
later validation samples; no personnel data/IDs/credentials. Main comparison uses three baseline and
the first three optimized measurement executions. Measurement cases passed even when a later separate
snapshot fixture failed; no measurement was discarded for its timing. Later all-green focused/full-run
measurements are also preserved separately.

- 32 employees: commands 60 -> 12. First calls before 401.98/474.38/505.13ms; after 459.47/532.00/503.62ms.
  Median warm call 34.91 -> 20.87ms (15 calls per side). Warm ranges 22.36-188.67ms -> 12.57-68.05ms.
  Median first call REGRESSED 474.38 -> 503.62ms; do not imply every small-store/cold call improved.
- 1,007 employees: commands 1,645 ->12. First calls before 1085.97/1025.92/1022.45ms; after 436.57/461.86/406.03ms.
  Median warm call 519.36 -> 30.77ms (15 calls per side). Warm ranges 388.58-751.49ms -> 22.03-183.06ms.
  The 183ms optimized outlier remains included. These repeated serial local calls are not load capacity.

The final complete backend run independently measured first calls 421.01/384.65ms (small/large), with
12 commands each; all five warm samples for each are in the numeric file. They are not silently mixed
into the initial three-trial comparison. Variation illustrates local timing limits, not a guaranteed SLA.

Two additional baseline trials temporarily used ONLY the original main handler, under a finally guard
restoring the exact optimized bytes. Both completed; final source references the new reader. No
intentional defect, reset or unrelated changes are retained.

## Executed evidence, including failed runs

- Initial baseline tests returned valid HTTP results but reported SQL 0: repeated AddDbContext did not
  attach the interceptor. Instrumentation was fixed with an explicit test-only scoped context and an
  active-counter assertion. SQL 0 is invalid, not evidence of zero queries. Those samples remain in the
  numeric evidence separately. Corrected baseline and two repeats each passed both cardinalities.
- Initial optimized measurement: two passes, commands 12/12. Production build: zero warnings/errors.
- First focused regression run: 5 passed / 1 failed. Snapshot fixture wrongly assumed WAL; actual default
  was DELETE. Retained assertions now explicitly exercise both supported behaviours, not a product
  journal-mode change. Next run: 5 passed / 2 failed because test Guid.NewGuid was inside an untranslatable
  EF ExecuteUpdate expression. The GUID is now a bound parameter. Both setup failures are preserved. The initial xUnit2031 build warning was corrected with
  Assert.Single's predicate overload; final builds have zero warnings/errors.
- Corrected focused run: 7 passed, 38.08s. Covers aggregate reconciliation, empty migrated store,
  DELETE/WAL snapshot barriers, HTTP authorization and preserved report/decision/audit data.
- Latest build after test cleanup hardening: zero warnings/errors, 2.05s. The held read is awaited even
  on writer failure, so its store cannot be deleted while still in use. The subsequent complete backend run
  passed 111/111, zero failures/skips, 7m26.58s. This is one combined execution after that cleanup change, distinct from the focused run.
- Frontend TypeScript/production build passed (Vite reported 2.09s), lint passed with the existing useAuth.tsx:223 Fast Refresh
  export warning, browser typecheck passed. Full client/browser audits returned zero findings.
- Offline model consistency passed: no pending model changes. No schema, dependency or lockfile changes.

## Retained checks

[Backend](../../tests/HRFlow.RegressionTests/HrWorkspaceQueryTests.cs) independently reconstructs the
former scalar algorithm from persisted employees, users and schedule revisions. It compares all eight
workforce counts and every department, including null facts, current/future schedules, future starters,
expiry equality/one-tick boundaries/null expiry, active/inactive, activated/Pending/Accepted/unknown/failed/
prepared states, invalid/missing/noncanonical Identity links and department changes. Scalar fixture
materialization is test-only and excluded from measurements. Private in-memory fingerprints and empty
tracking verify no read changes to profiles/Identity/roles/schedules/requests/audits; no private data is
printed. Real HTTP denies Employee/Manager, revoked HR, Pending and inactive accounts; combined roles
retain personal access after HR removal. Actual submission/approval followed by inactive reassignment
reconciles the unchanged report endpoint and trend sums; audits remain unchanged by reporting.

Snapshot assertions use independent actual SQLite connections and explicit barriers, not sleeps or an
in-memory database. They are NOT a claim of separate-process contention (existing retained suites
cover cross-process write protection). The barrier checks an existing transaction; both modes allow a
second writer reservation, and WAL additionally commits before read completion. Next read with a stale
credential is denied. TimeProvider is read once and SAST/UTC boundary semantics are explicit.

[Browser](../../tests/browser/hr-workspace.spec.ts) retains a real HR Overview/filter/attention-directory
workflow, mobile dark/reduced-motion/keyboard checks and labelled controlled 503/403 and late-response
transport. Same-/different-account replacement cannot expose the numeric old-response canary. These
controlled statuses are not substituted for actual backend current-role denial tests. Screenshots,
traces and video remain off; no message contents, credentials or private fixture output are retained.

## Reproducible commands

~~~powershell
dotnet build HRFlow.sln --no-restore
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~HrWorkspaceQueryTests --logger 'console;verbosity=normal'
dotnet test tests/HRFlow.RegressionTests --no-build
npm.cmd --prefix src/HRFlow.Client run build
npm.cmd --prefix src/HRFlow.Client run lint
npm.cmd --prefix tests/browser run typecheck
$env:HRFLOW_BROWSER_CHANNEL='chrome'
npm.cmd --prefix tests/browser test
~~~

Run fixture-sensitive suites sequentially: offline operations intentionally refuse other API writers.
Explicitly configure a fresh disposable connection for EF has-pending-model-changes; never use an
existing employee store. Final default browser run passed 41/41 with retries disabled in 126.77s, including all four new HR
cases. Frontend product source was unchanged; the browser uses the normal development client and
installed Chrome, not a separate production-built browser invocation. Client/browser full and
production-only audits returned zero findings; solution transitive scan found none across six projects.
Final diff/documentation checks are recorded below, separately from prior dated reports. No hosted/Linux, production-built browser, target-host
load, real-provider/ClamAV, comprehensive accessibility or legal-compliance result is claimed here.
Remaining report/trend/import materialization is documented in the ADR; no arbitrary truncation or
cache masks it. Production invitation delivery stays disabled and #75/#22 gates remain open.

## Final review checks

14 intended changed/new files; 117 relative documentation link targets checked. UTF-8/JSON parsing,
replacement-character/trailing-whitespace and sensitive-literal checks passed. git diff --check passed.
Both instruction files are byte-identical. No product frontend, dependency/lock, schema/migration or CI
changes at implementation handoff. Complete review patch includes all new files and is validated against the
working tree. Historical verification reports remain untouched.

## PR finalisation review

The complete source/test/documentation diff was reviewed again before staging. Authorization still
runs first inside the existing deferred snapshot; static SQL and EF-bound values introduce no
per-employee round trips, input concatenation, large IN list or truncation. Frontend product source,
API DTOs, calculations, schema, dependencies and CI remain unchanged. Measurement samples are numeric
synthetic evidence, without account identifiers, credentials or private paths.

Finalisation reruns documentation links, UTF-8/JSON, whitespace, sensitive-literal and instruction-byte
checks against the staged content. The separate 7-focused, 111-complete-backend and 41-browser runs
above are implementation evidence, not newly rerun finalisation tests. Hosted results are recorded
on the PR after publication; they are not inferred from these local Windows/Chrome results.
