# Bounded Manager queue — local verification, 10 November 2026

> 11 November: publication of the combined queue/editor patch is authorised. The pause and all
> executions below are historical; see the [follow-up finalisation](employee-editor-investigation.md#finalisation-11-november-2026)
> for review corrections and new results. No merge, issue closure or deployment is authorised.

> Publication paused at the user's request. Queue changes are preserved; the subsequent
> [employee Edit investigation](employee-editor-investigation.md) records the separate UI/image/logging
> correction and new executions. Results below remain the original queue evidence.

Branch `feat/paginated-manager-approval-queue`, fetched base
`5f3d4966f672f88c852c3bf985ff877625db1adf`, after confirmed merged PR #141.
[API/authorization/caller contract](../adr/0036-bounded-manager-approval-queue.md).
No commit, push, hosted CI, issue edit or deployment is authorized in this implementation phase.

## Repeatable measurements

Command from repository root, with other fixture writers stopped:

```powershell
dotnet test tests/HRFlow.RegressionTests --no-restore --filter FullyQualifiedName~ManagerQueueMeasurementTests --logger "console;verbosity=normal"
```

Disposable migrated file-backed SQLite, one assigned synthetic owner, 20 or 2,000 Pending requests,
one genuine Submit audit each, tied dates across 400 days. Setup/migration/seeding/login excluded from
the HTTP timer. Read all response bytes, first endpoint invocation plus five serial warm calls. First
is not a cold OS/database-cache guarantee. Windows x64, i5-1155G7 2.50 GHz, 8 logical CPUs, 8 GiB RAM,
Node 24.12.0. No CPU isolation, load simulation or production-capacity claim.

Baseline, separate **2/2 passed**, **16.6974 seconds**, before changing the API binary:

- 20 rows: 20 returned, **7,001 bytes**; first **115.33 ms**; warm **10.35, 10.46, 5.37, 5.37, 4.79 ms**.
- 2,000 rows: 2,000 returned, **700,001 bytes**; first **154.23 ms**; warm **25.72, 27.40, 20.64, 30.66, 21.92 ms**.

First post-change samples, in the restricted execution environment like the baseline, from the separate
4/8 focused execution (both measurement cases passed; the overall run failed as recorded below):

- 20 rows: 20 returned, **7,117 bytes**; first **140.67 ms**; warm **9.73, 11.67, 5.85, 7.21, 4.96 ms**.
- 2,000 rows: 25 returned, **8,870 bytes**; first **205.07 ms**; warm **18.84, 30.13, 13.36, 16.04, 15.84 ms**.

Additional post-change samples from the separate unrestricted 6/8 focused execution below
(measurement cases passed; this environment difference precludes a strict timing comparison):

- 20 rows: 20 returned, **7,117 bytes**; first **142.19 ms**; warm **13.95, 12.78, 5.26, 5.84, 5.64 ms**.
- 2,000 rows: 25 returned, **8,870 bytes**; first **236.16 ms**; warm **14.76, 15.73, 8.46, 11.17, 8.00 ms**.

Independent later samples in the expanded 11/11 execution:

- 20 rows: 20 returned, **7,117 bytes**; first **186.45 ms**; warm **12.69, 12.46, 6.19, 23.01, 7.21 ms**.
- 2,000 rows: 25 returned, **8,870 bytes**; first **179.27 ms**; warm **13.34, 12.60, 7.28, 8.43, 8.02 ms**.

Larger response bounds improved; **first-call and small-store timings regressed**. Larger warm samples
improved locally; these few noisy samples do not establish universal speed improvement. Count/envelope
overhead, JIT/cache and scheduling effects were not isolated. Handler interception at both sizes found
**six SELECTs, all using the same DbTransaction reference**. This includes credential/current-role
checks, filtered count and bounded rows, excluding the separate bearer gate and workers. Baseline SQL
count was not instrumented; no before/after SQL-reduction claim. Other full-history reads remain.

## Earlier failures, corrections and separate focused runs

- Initial focused backend **4 passed / 4 failed / 8 total**, **44.0845 seconds**. Four HTTP cases ended
  prematurely during expected 400/403/409 paths; handler/measurement cases passed.
- Two independent safe diagnostic isolations each failed **0/1**, **7.8223** and **7.0442 seconds**.
  Allowlisted framework types/frames identified `EventLogInternal.OpenForWrite` and
  `EventLogLogger.WriteMessage` throwing while logging expected denials under the restricted Windows
  process sandbox. DataProtection/DPAPI warning frames were also observed; no key material was read.
  Temporary diagnostics were removed; fixture/production logging and permission checks were unchanged.
- Unrestricted focused run **6 passed / 2 failed / 8 total**, **48.2266 seconds**. Premature-response
  failures disappeared. Two new tests incorrectly expected zero audits after constructing real
  requests, whose factory correctly creates Submit audits. Tests now compare preserved original audit
  rows or assert the real Submit count, rather than deleting history or changing production behavior.
- Expanded focused backend **11/11 passed**, **59.9434 seconds**, with selector >25 owners, actual HR
  deactivation, current lifecycle, credential/role/reassignment denial, two-process competing decisions,
  all filters/bounds/order, independent attention count and constant protected SQL count.
- Initial focused browser **4/4 passed**, **28.705694 seconds**, Windows Edge. Real HTTP paging/reject,
  controlled refresh with retained request/note and actual stale-decision denial, same-/different-account
  held-response barriers. This preceded extra approve and out-of-order/error cases; it is not final
  coverage of those later additions.

No skips, automatic retries, timeout increases or relaxed authorization/expected-status assertions. Private
fixtures use generated credentials, real migrations, owned-process cleanup and existing strict modes.
Safe reporter additions print only fixed source coordinates. Screenshots/traces/video are disabled.
No notes, descriptions, raw errors, payloads, tokens or private paths are reported.

## Retained coverage

Backend verifies owner/reporting scope and denied self/non-report/cross-department IDs, Employee/HR-only
denial and combined roles, current revocation/lifecycle/credential checks, reassignment, inclusive leap
bounds, invalid/extreme inputs, 55-row ties/multiple pages, unavailable IDs and zero-result clamps.
Selector tests search a later owner beyond the first 25 choices, then perform real HR deactivation.
Real competing decisions run through two API processes and persist one terminal audit, preserving
the original Submit. Reads compare original audit facts and do not create submission outcomes.
Existing full regression gates retain balance/overlap, late rollback, Medical privacy/outbox and
recovery checks; no new engine or scanner substitute was introduced.

Browser tests exercise real synthetic requests and both final-page decision actions, applied/draft
filters, note/selection retention and authoritative errors, mobile/dark/reduced-motion/keyboard,
out-of-order filters, error/retry drafts and same-account/combined-account replacement. Controlled
transport barriers are labelled separately from real server decision/invalidation workflows.

## Complete gates and limitations

Complete final backend/browser executions and final checks are recorded below only after completion.
The four npm full/production-only audits reported zero vulnerabilities; no dependencies were changed.
First solution build passed zero warnings/errors (10.54 seconds). Frontend build, lint and browser
typecheck passed; existing auth fast-refresh warning remains. Final build/check evidence follows.

No hosted/Linux, positive real-ClamAV, production-built browser, physical-device/screen-reader, or
realistic-load execution is claimed. Normal tests remain independent of a locally installed scanner.
No schema/migration/index, provider, statutory/payment, evidence-retention or production change.
Rollback concerns coordinated response compatibility, not record loss. Historical reports are retained.

## Complete backend execution and final build

With browser writers stopped, `dotnet test tests/HRFlow.RegressionTests --no-build --logger "console;verbosity=normal"`
passed **215/215**, **0 failed / 0 skipped**, **18.8803 minutes**, one complete execution. This includes
all nine new queue test cases plus two measurement cases, retained model/migration, current-role,
writer/rollback, document/recovery, refresh and authorization gates. It is separate from every earlier
focused run; no failures were silently rerun. C# executable code/tests were unchanged during this run
(a controller XML comment was added). Final `dotnet build HRFlow.sln --no-restore` passed **0 warnings /
0 errors**, **10.96 seconds**. Numeric process inspection found **0 fixture hosts** before browser startup.

This full run's additional measurement samples (not replacements for earlier samples):

- 20 rows / 7,117 bytes: first 172.55 ms; warm 12.27, 16.13, 9.25, 9.21, 25.25 ms.
- 2,000 rows / 25 returned / 8,870 bytes: first 160.83 ms; warm 9.80, 13.95, 6.95, 6.01, 6.13 ms.

All six .NET projects had no vulnerable packages in `dotnet list HRFlow.sln package --vulnerable --include-transitive`.
The four npm checks were `npm audit` and `npm audit --omit=dev` separately for `src/HRFlow.Client` and
`tests/browser`; each reported zero vulnerabilities. The client build before final text adjustment (including TypeScript) passed,
Vite **2.85 seconds**; lint passed with the existing auth fast-refresh warning. Final browser
`npm run typecheck --prefix tests/browser` passed after all seven new browser cases were present.

## Browser fixture isolation

After the initial 4/4 browser run, expanded cases add final-page approval, out-of-order filters/error
recovery and non-replayable decision 401. Source inspection identified a shared-fixture risk: creating
future requests for the Employee actor would push later personal-history workflow requests off page
one. The final cases instead use the existing unrelated-owner/senior-Manager pair in the same isolated
fixture. This is test isolation, not deletion, a production rule change or an observed full-suite failure.
Normal same-account replacement uses that same senior Manager; combined-role replacement is separate.
Complete browser runs follow the backend execution/build, with frozen executable sources within each run.
Their distinct outcomes and the final text/keyboard adjustment are recorded below.

## Separate complete browser executions

From `tests/browser`, with `HRFLOW_BROWSER_CHANNEL=msedge`, the first complete `npm.cmd test` reported
**96/96 passed**, no failed/skipped/retried tests, **536.658306 seconds**. Its enclosing PowerShell
process nevertheless returned **exit 1**. The private log contains a `NativeCommandError` conversion
of Node's experimental SQLite warning, no reported test failure or safe-reporter runner error. The
underlying npm exit code was not captured for that invocation; it is not presented as an unqualified
command success. Numeric process inspection afterward found zero fixture hosts.

After that run, newly introduced loading-text question marks were corrected to plain statement
punctuation, and both page/decision cases gained a real Shift+Tab assertion from Reset to Apply.
No authorization, paging, decisions or calculation behavior changed. Build/lint/typecheck passed.
Instead of treating the earlier run as final-source proof, a second complete execution explicitly
captured the native npm exit code (warnings remained enabled):

```powershell
$env:HRFLOW_BROWSER_CHANNEL="msedge"
& npm.cmd test > ../../.local-verification/manager-queue-browser-final.log 2>&1
$queueBrowserExit=$LASTEXITCODE
Write-Output "browser-command-exit=$queueBrowserExit"
exit $queueBrowserExit
```

Final complete result: **96/96 passed**, **0 failed / 0 skipped / 0 retries**, **304.881201 seconds**,
**explicit command exit 0**. This fresh-store execution includes all seven queue cases and the retained
personal history, submission/outcome recovery, Manager/HR capabilities, team calendar, evidence,
actual bearer refresh/session and non-replayable workflow regressions. It is separate from the
215-backend run, earlier 4-browser focus and first complete 96-test report. No executable files changed
during this final run. Normal CI remains scanner-independent; no positive real-ClamAV proof follows.

## Review checks and remaining boundaries

UTF-8/formatting, complete-diff whitespace, relative links/anchors, sensitive-pattern review and
byte-identical instructions are checked across all changed/new source, tests and documentation.
No production fixture data, screenshots, credentials, downloaded files, logs, generated outputs or
local harnesses belong to the diff. No commit/push/PR/tracker changes.

The payload timings cover only the queue endpoint, not end-to-end UI time. The UI also performs a
bounded report search and uses the existing full type catalogue. HR monitoring, balance/report/type
reads remain outside this pagination slice. No production capacity, statutory calculation, real
scanner, screen-reader/device or hosted/Linux verification claim is made. Full source/model/schema
compatibility is unchanged except the deliberate queue response contract; deploy API/client together.

Final post-browser client build, with explicit npm exit capture, passed **exit 0**, Vite **5.52 seconds**,
including TypeScript. The existing Vite plugin-timing advisory and auth fast-refresh warning remain;
Node's experimental SQLite warning was not suppressed. Final lint/browser typecheck also passed after
the text/keyboard adjustment. Final numeric inspection found **0 fixture API/browser hosts**.

Review checks passed on all **18 changed/new files**, including **56 relative links/anchors**, strict
UTF-8, balanced Markdown fences, new-file and tracked-diff whitespace, sensitive-pattern/manual
source review and byte-identical instructions. No files are staged. The existing CI workflow/budgets
are unchanged; this unpublished branch has no new hosted result. Local timings do not prove the
complete hosted gate will fit its budget, or establish target-host readiness.
