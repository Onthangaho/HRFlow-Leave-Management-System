# Owner leave preview: local verification

26–27 October 2026 (SAST); Windows x64, Node 24.12.0, .NET 8 target (SDK 9.0.305), Edge/Playwright 1.64.1.
Branch `feat/leave-request-preview`; base `c9780f87767b28dfe287243cb37f1cbdd0c4e235`.
`gh pr view 137 --json state,mergedAt,mergeCommit` confirmed MERGED at 20:50:29 UTC; fetched origin/main
and created the branch from clean main. [API/design](../adr/0033-owner-leave-request-preview.md).

## Separate executions

Fresh migrated private SQLite, generated Identity credentials and owned loopback API processes.
No real employee store or external recipient. Historical PR #137 hosted 164 backend / 70 Chromium
is not a rerun of this patch. No hosted execution is claimed for this unpublished branch.

- `dotnet build HRFlow.sln --no-restore -v quiet`: passed, zero warnings/errors, 19.57s.
- `dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~LeavePreviewTests -v minimal`:
  initial 15/15 passed, zero skipped, 2m57s. Four focused cases were added afterward; this initial
  result does not verify those later cases.
- Client `npm.cmd run build` then `npm.cmd run lint`: passed. Initial production build 13.41s;
  existing auth-provider `react(only-export-components)` warning and Vite plugin-timing diagnostic.
  Requirements-refresh follow-up was added after this build.
- Browser `npm.cmd run typecheck`: passed.
- Browser `$env:HRFLOW_BROWSER_CHANNEL='msedge'; npm.cmd test -- leave-preview.spec.ts`: initial
  6/6 passed, zero retries, 39.244226s. Later out-of-order strengthening and policy-review case need
  their own subsequent evidence.
- A solution rebuild overlapped that browser fixture and failed MSB3021/MSB3027: 10 copy warnings /
  2 errors, 20.50s. Windows blocked copying the live `HRFlow.RegressionTests.dll`. This execution-order
  failure is preserved, not an authorization/permission defect. After the fixture stopped, the same
  build passed with zero warnings/errors in 3.13s. Subsequent fixture executions are sequential.
- `dotnet list HRFlow.sln package --vulnerable --include-transitive`: all six projects reported no
  vulnerable packages given current sources.
- `npm.cmd audit` and `npm.cmd audit --omit=dev`, separately in client and browser: all four reported
  zero vulnerabilities. No dependency/manifest/lockfile changes.

- Expanded focused backend execution: **19/19 passed**, zero skipped, 3m02s. This was separate from
  the initial 15-case run. The overlap-write assertion was subsequently tightened from any failed
  response to the exact expected HTTP 400; a later rebuilt execution is required for that assertion.
- Follow-up client build/lint passed (2.10s), and the read-only-refresh follow-up build/lint passed
  (2.85s). Browser typecheck passed again after the additional cases. A final focus-outline change
  requires the final build recorded below.
- First complete backend command (`dotnet test tests/HRFlow.RegressionTests --no-build -v minimal`):
  **ABORTED**, not passed. It reported 159 completed passes, zero reported assertion failures/skips,
  duration 23m24s after the operator stopped the verified owned testhost. At about 22 minutes the
  testhost/console were alive with no API/operator children; minimal logging did not identify the
  active test. Later privileged metadata showed a fixture created at **21:35:10.810 UTC**, near
  the interruption, with 26 schema tables and zero users/employees/requests/audits. The interruption
  was premature: no hang, failing test, contention or preview defect was established. This remains
  an operator-aborted slow execution, not a proved timeout/root cause. No assertion, product timeout,
  CI limit or permission was weakened. The identified interrupted store was subsequently removed
  after checked absolute temp-root containment, exact creation window/generated name, no reparse
  points, three owner-restricted entries and exclusive database-file access; no other store was removed.
- After that stopped run, solution rebuild passed with zero warnings/errors in 15.99s, including
  the exact HTTP-400 assertion. The next complete command uses normal per-test progress filtered
  to fixed class/method labels, safe counts/source locations only, and a **local diagnostic**
  `--blame-hang-timeout 3min --blame-hang-dump-type none`. It collects no memory dump; private
  diagnostic output is excluded from the diff. This is a separate execution, not an automatic retry.

## Rebuilt complete backend result

The separate diagnostic execution completed: **183/183 passed**, zero skipped/failed,
**28.1621 minutes**. All 19 preview cases, including the exact HTTP-400 assertion, passed in this
rebuilt run. The long successful duration reinforces why the earlier operator interruption must not
be presented as a demonstrated hang. No product/fixture correction for a backend timeout is claimed.

```powershell
dotnet test tests/HRFlow.RegressionTests --no-build --logger 'console;verbosity=normal' `
  --blame-hang-timeout 3min --blame-hang-dump-type none --results-directory $taskResults
```

`$taskResults` was a newly created owner-restricted ignored local diagnostic directory. Output was
filtered to fixed test class/method names, numeric summaries and safe source coordinates, not raw
errors/arguments. No memory dump was produced. The full run includes
`EvidenceAdditionTests.ModelMatchesMigrationAndLaterTypeChangesDoNotDemandEvidence`
(`HasPendingModelChanges()` is false), real legacy migration preservation, writer/role revocation,
balance/overlap revalidation, exactly-one audits, late rollback, CSV idempotency, activation/recovery,
first-admin, image and directory regressions. No schema or migration changes.

## Browser failures, diagnosis and separate reruns

All commands below ran in `tests/browser` with `HRFLOW_BROWSER_CHANNEL=msedge`, Playwright retries=0.

- Expanded `npm.cmd test -- leave-preview.spec.ts`: **7 passed / 1 failed**, **40.824837s**.
  The then-current line 63 could not locate the populated description with exact `getByLabel`.
- Unchanged failing assertion plus safe diagnostic, `npm.cmd test -- leave-preview.spec.ts --grep
  'policy change requires explicit review'`: **0 passed / 1 failed**, **23.270057s**. It reported
  `count=1 valueMatches=true labelCount=0`: the draft was retained, not lost. No raw field value,
  HTML, credential or Playwright call log was printed.
- After switching to the exact textbox role/accessibility name, the same isolated command:
  **1/1 passed**, **13.474626s**, `count=1 valueMatches=true labelCount=0 roleCount=1`. The exact-value
  assertion remained; this was a selector correction, not a product draft-loss fix. Real 409,
  explicit review, retained fields, successful submission and exactly one Submit event all passed.
- First complete `npm.cmd test`: **76 passed / 2 failed**, **220.699333s**. Both new preview tests
  failed at their draft-checkbox preconditions (then-current lines 51 and 78). The earlier evidence
  file commits five additions against the five shared Clean drafts; assuming leftovers made these
  new tests order-dependent. All original 70 cases and the other six new cases passed.
- Correction: the real policy-conflict/review/submission test now uses optional evidence without
  consuming that shared pool. A separately labelled UI test supplies controlled document/configuration
  metadata, calls the **real preview API**, preserves a checked selection through explicit review,
  and asserts **zero Submit/document mutations** plus exactly the three date/type preview body keys.
  The fake metadata has no blob and is never submitted, downloaded or called a successful scan.
  No production seam, database Clean-status rewrite, additional seed pool, skip or weaker existing
  evidence assertion was introduced.
- Relevant-order command `npm.cmd test -- evidence.spec.ts leave-preview.spec.ts`:
  **17/17 passed**, **57.254587s**, proving the preview checks work after all five original drafts
  are consumed. The final UI fixture subsequently fixes its generated metadata IDs across refetches
  and makes safe diagnostics tolerate an absent textarea; no product logic changed.
- Final client `npm.cmd run build` and `npm.cmd run lint` passed after the read-refresh/focus changes,
  production build **4.99s**. Existing auth-provider fast-refresh warning and Vite plugin-timing
  diagnostic remain. Final browser `npm.cmd run typecheck` passed. Node's existing experimental
  `node:sqlite` warning appears in complete browser runs; it is not suppressed or a dependency change.

- Corrected complete default browser execution: **78/78 passed**, **200.648432s**. This was separate
  from the failed 76/78 execution and the 17-case relevant-order execution.
- Final semantic/focus review then made the result a named group and explicitly focused the retained
  requirements-review notice when its triggering control disappears. Retained browser assertions
  now require that group and notice focus. After these frontend-only changes, client build/lint
  passed again (production build **1.53s**), with the same existing auth-provider warning; browser
  typecheck passed. The intermediate group-only build/lint also passed (3.49s).

Final complete browser execution after those focus changes: **78/78 passed**, **212.307883s**,
zero retries/skips. This is a separate complete run, not an aggregate with the earlier 78-case pass.
It includes all eight preview cases and the existing 70 browser gates: real Employee submission,
assigned Manager decision/owner history, HR monitoring, onboarding/directory/edit conflicts,
reports/export navigation, calendar, lazy routes, authentication/session boundaries and non-replayable
mutations. Backend product source was unchanged after its successful 183-case run; the complete
backend suite was not repeated for these frontend-only edits.

```powershell
# From tests/browser, after other fixture writers stopped:
$env:HRFLOW_BROWSER_CHANNEL='msedge'
Remove-Item Env:HRFLOW_BROWSER_PRODUCTION -ErrorAction SilentlyContinue
npm.cmd test
npm.cmd run typecheck
```

### Final test-only readiness correction

Source review identified a possible independent writer during the reconciliation case's all-table
fingerprints: real submission/approval queues notification events, and the existing worker polls
every five seconds. No failure was attributed to this race. The test now asserts two durable events
and awaits their persisted delivery acknowledgements before capturing the comparison baseline.
This bounded 15-second condition uses 25ms readiness polling, not a guessed sleep or business retry.
All original hash, balance, status and audit assertions remain intact; no worker is disabled.

After browser fixtures stopped, `dotnet build HRFlow.sln --no-restore -v quiet` passed again with
zero warnings/errors (8.06s). The exact affected command:

```powershell
dotnet test tests/HRFlow.RegressionTests --no-build `
  --filter FullyQualifiedName~LeavePreviewTests.ReconcilesAndSubmissionStillRevalidates -v minimal
```

passed **1/1**, zero skipped/failed, **11s**. This followed the complete 183-case run; the full
backend suite was not rerun after this one test-only readiness adjustment. The other 182 backend
cases and all backend product source are unchanged. Do not present these executions as one final
184-case run. The final 78-case browser run was before this C# test-only edit; no browser product or
test source changed afterward.

## Final diff and documentation review

The complete source/new-file review covered **16 intended files**. UTF-8/BOM/replacement-character,
final-newline/trailing-whitespace, Markdown fence, **39 relative-link/anchor** and sensitive-pattern
checks passed. `git diff --check` passed; the identical instruction files were compared byte-for-byte.
Git's existing LF-to-CRLF worktree notices are not whitespace failures. Manual diff inspection and
the pattern checks found no credentials, JWTs, activation links, private fixture paths, databases,
uploaded files or generated output. A complete local review patch includes new files; verification
helpers/progress and restricted diagnostics remain ignored and are not part of that diff.

`git diff --cached --name-only` was empty. Infrastructure/schema/migration, dependency/lockfile and
CI files are unchanged. A safe process inventory after completed gates found **zero** matching new
API/operator/test-fixture processes in this task window. No commit, push, PR, tracker change or
external-service enablement was performed. Static checks are rerun after this report update.

## Coverage and evidence boundaries

### Publication review — 27 October 2026

Publication is now authorised for independent PR review. The earlier empty-index/no-publication
statement describes the implementation phase, not this finalisation. Complete source/new-file
inspection found no further product correction. Only publication wording changed during finalisation;
the 183-case backend run, subsequent test-only readiness adjustment with its isolated 1/1 pass,
and final 78-case browser run remain separate earlier local executions. They were not rerun as a
combined finalisation gate. Staged UTF-8, links, whitespace, sensitive-pattern and instruction-byte
checks passed across all 16 intended staged files and 39 relative links/anchors before committing.
No private fixture, generated output or sensitive-pattern finding was included. Hosted results will be reported separately after publication;
no hosted pass is inferred from these local results.

[`LeavePreviewTests`](../../tests/HRFlow.RegressionTests/LeavePreviewTests.cs) checks real bearer HTTP,
owner isolation even with an overposted identity, role composition, calendar dates/leap/weekend units,
requirements/versions, missing manager, live role/lifecycle/credential denial, safe bounded input,
balance and actual submission/approval reconciliation, history/policy changes after preview, zero/
exhausted balances and overlap-enabled policy. All-table fingerprints compare persisted values
without printing them. A separate connection holds writer protection while a real API-process
preview finishes, then committed revocation denies submission. This is reuse of the existing
deferred transaction, not a newly instrumented mid-read revocation race.

[`leave-preview.spec.ts`](../../tests/browser/leave-preview.spec.ts) retains actual form/API preview,
Manager personal/HR navigation separation, explicit stale-policy review with real 409 then submission,
retained dates/description, focused result, mobile/theme overflow, controlled 503/retry, held real
response/out-of-order inputs and same-/different-account replacement. The read-only refresh case
replaces only the first bearer with the existing signed expired probe; actual server validation
returns 401 and the real refresh/retry succeeds once. This is not a naturally aged login-issued token.
The document-selection retention test is controlled **metadata only**, with no blob or writes.
Existing separate evidence tests exercise actual binding/download over controlled clean bytes;
neither is claimed successful real ClamAV scanning. No new scanner, bypass, skip or automatic test retry.

## Limitations

Positive real ClamAV, physical devices, screen readers, comprehensive accessibility, target-host,
load and legal review are not exercised by this date/balance feature. Default browser execution uses
the development server; production-built browser execution is not claimed unless separately listed.
The local model/UTC submission lower bound is not statutory entitlement or a pay promise.
