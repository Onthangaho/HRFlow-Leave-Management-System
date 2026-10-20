# Directory browser regression investigation — 20 October 2026

## Isolation and original evidence

PR #125 remains draft and unchanged at `c435c9dc2cbb5cfa5df3b4003619d9b186cd0b1d`.
A separate clean worktree/branch `fix/directory-browser-regression` was created from fetched main
`95a681fc082090b36b6ede35c2988e54f47c6b41` (merged PR #124). The worktree's source/instructions,
planning, directory pagination/edit state, shared browser fixture and safe reporter were inspected.
No #75 legal/calculation conclusions, production source, schema, dependencies or CI protection changed.
Both instruction copies remain unchanged and byte-identical.
Final comparison also confirms PR #125's production/test/workflow tree matches this branch's base;
the documentation PR did not introduce the failing test's executable code.

Original [PR #125 hosted run 39442610037](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/39442610037):
88 backend passed; Chromium 17 passed / 1 failed. Failing test:
`HTTP directory: applied pages, drafts, attention links and desktop/mobile layouts` (1,529 ms).
Its reporter deliberately omitted assertion/call-log details. Those details were **not available**;
there is no evidence identifying the original failed assertion or proving the failure's root cause.

## Findings and cause classification

**The original hosted failure was not reproduced in the local baseline runs below.** Passing later
runs do not establish flakiness, an environment fault, a product fix or comprehensive Linux success.

- Product defect: not established; production source remains byte-for-byte equal to branch base.
- Test synchronization: a narrower, independently confirmed gap. The former Refresh check asserted
  the already-present draft immediately after clicking, without proving its HTTP response/render
  completed. Negative row-text matching was also not a complete page-readiness condition.
- Explicit barrier evidence: a real upstream filtered directory response is held after fetch. The
  old draft-value assertion succeeds while `aria-busy=true`. The new request/render wait must remain
  incomplete until explicit release, then return only with idle rendered results and the draft intact.
  This is labelled **controlled transport**, not an independent demonstration of the hosted cause.
- Shared fixture/order: one newly migrated synthetic store is shared per invocation; contexts are new
  per test. Before the target, measurement reads and own-image removal do not change employment facts,
  schedule attention or employee count. Normal-order full baseline and bounded target repeats passed.
  This does not exclude unobserved host/order effects; it does not justify a flakiness label.
- Responsive layout: retained desktop/mobile, page-width and keyboard assertions passed locally.
  At the mobile checkpoint, observed document/viewport widths were both 390; no CSS/layout fix justified.
- Environment: local Windows/Chrome differs from original Ubuntu/bundled Chromium. No confirmed
  environment failure. A future hosted run with retained diagnostics is needed to narrow recurrence.

## Small justified correction and diagnostics

[Test readiness helper](../../tests/browser/directory-readiness.ts) registers the exact collection
request predicate before the action, verifies HTTP 200 and waits for the results region's rendered
`aria-busy=false`. The target uses it for page changes, applying search, Refresh and attention-link
navigation. Existing page-count/content, unchanged draft, filter, mobile overflow and focus assertions
remain; no sleeps, timeout increases, retries, skips or relaxed assertions. No product behavior changes.

[Safe diagnostics](../../tests/browser/directory-diagnostics.ts) use fixed step labels, the relative
`directory.spec.ts` test-definition line, viewport dimensions, numeric row/document-width counts and
boolean loading/failure state. Location is **test definition plus step**, not a recovered original
assertion stack. Diagnostics run even when an action fails, without printing its error.

The [reporter](../../tests/browser/safe-reporter.ts) previously suppressed worker stdout entirely.
It now emits only validated diagnostic records rebuilt from allowlisted fields. Unknown labels,
source files, invalid numeric/boolean fields and unstructured/malformed output are discarded; extra
fields are removed. A retained pure contract test verifies this. Raw errors/call logs, page HTML,
request/response bodies, personnel values, credentials and activation links are never forwarded.
Screenshots/traces/video remain disabled, opt-in capture/measurement/scanner variables were cleared,
and default attachment/private-fixture cleanup stays intact. No CI artifact upload is introduced.

## Commands and every local execution

From the separate worktree, Windows x64, .NET 8.0.23 with SDK 9.0.305, Node 24.12.0 and installed
Chrome. Existing locked dependencies only; fresh migrated file-backed SQLite/Identity fixtures,
generated private credentials and private ephemeral ports/storage. No scanner substitute.

```powershell
dotnet restore HRFlow.sln --locked-mode
dotnet build HRFlow.sln --no-restore
npm.cmd ci --prefix src/HRFlow.Client
npm.cmd ci --prefix tests/browser
npm.cmd --prefix tests/browser run typecheck
$env:HRFLOW_BROWSER_CHANNEL='chrome'
Remove-Item Env:HRFLOW_DIRECTORY_SCREENSHOTS,Env:HRFLOW_DIRECTORY_FIXTURE_ROWS,Env:HRFLOW_DIRECTORY_MEASURE,Env:HRFLOW_IMAGE_SCANNER_TESTS -ErrorAction SilentlyContinue
npm.cmd --prefix tests/browser test -- --grep 'HTTP directory: applied pages'
npm.cmd --prefix tests/browser test
npm.cmd --prefix tests/browser test -- --grep 'HTTP directory: applied pages' --repeat-each=3
npm.cmd --prefix tests/browser test -- --grep 'diagnostic contract|refresh waits for response'
npm.cmd --prefix tests/browser test -- --grep 'HTTP directory: applied pages|refresh waits for response' --repeat-each=3
npm.cmd --prefix tests/browser test
npm.cmd run build --prefix src/HRFlow.Client
npm.cmd run lint --prefix src/HRFlow.Client
npm.cmd --prefix tests/browser run typecheck
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~EmployeeDirectoryTests
```

Runs are separate, not an invented aggregate. Retries remain **0**, workers **1** and original
60-second test /10-second assertion timeouts unchanged. Repetition was bounded in advance to three.

1. Initial diagnostic typecheck **failed**: NodeNext required `.js` helper import extension. Corrected;
   no browser test ran in that failed invocation. Subsequent typechecks passed.
2. Original assertions, instrumented isolated target: **1 passed**, 11.665s test /47.863s suite.
   Its checkpoint stdout was still suppressed by the old reporter; this was corrected explicitly.
3. Original assertions, normal-order complete suite with safe reporter: **18 passed**, 127.376s.
4. Original assertions, bounded isolated repeats: **3 passed**; target times **10.199/9.318/6.261s**,
   complete invocation **65.034s**. No local reproduction of original failure.
5. New diagnostic allowlist and held-response checks: **2 passed**, 34.768s suite; pure contract 30ms,
   barrier 10.150s. This confirms the distinct readiness gap, not the lost hosted assertion.
6. Corrected target plus barrier, each repeated three times: **6 passed**, 90.655s. In execution order:
   barrier **15.044s**, target **18.346s**; barrier **2.963s**, target **5.424s**; barrier **2.728s**,
   target **4.446s**. No failure hidden by retry; no assertion changed during these executions.
7. Final corrected complete invocation: **20 passed**, **83.926s**, no retries/skips. Nine real-HTTP
   cases, ten controlled-transport cases and one pure diagnostic-contract case. The target passed in
   5.672s and the barrier in 3.270s. This is one combined run, distinct from the earlier 18-case run.
8. After browser teardown, affected backend directory invocation: **4 passed /0 failed/skipped**,
   **57.127s**. Migrated SQLite authorization, deterministic paging, attention/Manager/detail and
   retained-image version/hint checks remain green; this is not a complete backend invocation.

Locked restore and initial solution build passed (**0 warnings / 0 errors**, 45.64s). The backend
source remained unchanged afterward. Final frontend build/lint and browser typecheck passed;
main bundle remains 610.88 kB /179.21 kB gzip. Existing Vite >500 kB and `useAuth.tsx:223` Fast Refresh
warnings remain. Both locked npm installations reported zero vulnerabilities; no dependencies changed.

Final review checks passed for **six scoped test/documentation files**, **11 relative links**, strict
UTF-8, formatting/whitespace and bounded sensitive-literal/private-path scans. `git diff --check`
and reverse-application validation of the complete patch (including new files) passed. Production,
tooling, schema/dependency/CI and both instruction files have no diff from base. At the implementation-review cutoff, no files were staged.
Owned API process count after teardown was **0**. The original documentation worktree remained clean
on its unchanged PR #125 head, still Draft. Local fixture/build output and the review patch are ignored.

## Limits and follow-up

Original assertion details remain unavailable; no claim the first hosted failure is conclusively
fixed. This branch adds safe recurrence evidence and closes the confirmed readiness gap. No new hosted
Linux run can be claimed for this unpublished branch. Positive real-ClamAV, comprehensive accessibility,
mobile hardware, load and deployment checks were not run. The 88-test complete backend gate is not
claimed as a new local run for this test-only change. No screenshots or traces were captured/published.
Implementation evidence above predates publication. Finalisation authorises only the focused commit/push and follow-up PR; PR #125 remains unchanged. No merge, issue/reviewer contact or unrelated feature is authorised.

The instrumented baseline retains original assertions but adds diagnostic browser roundtrips. It
does not reproduce the original uninstrumented timing exactly; those passes cannot rule out an
unobserved timing race. Screenshots, raw errors and payload-bearing artifacts were not used to
infer the missing original assertion. No original-failure assertion details have been reconstructed.
