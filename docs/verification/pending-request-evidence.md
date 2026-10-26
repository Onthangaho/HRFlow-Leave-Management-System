# Pending-request evidence — execution record

22 October 2026. Branch `feat/pending-request-evidence`, base `ca2cb934105d9dd97800e5b907748ded9fae3473`
from fetched main, PR #134 confirmed merged. The implementation handoff was local and uncommitted; publication is separately authorised. No deployment.
[API/concurrency/migration design](../adr/0032-pending-request-supplemental-evidence.md).

## Evidence boundaries

Fresh migrated file-backed SQLite and generated private Identity credentials are used. The focused
HTTP tests generate and validate PNG blobs through real private storage, then explicitly establish
controlled clean metadata. This is not malware-detection evidence. Production scanning/storage
fences are unchanged. No ClamAV listener was present, and earlier private scanner descriptors pointed
to unavailable installations/definitions. Positive real-ClamAV scanning is not executed in this run.
No always-clean scanner was added to production or used to claim real scanning.

## Separate executions

- Initial migration/build attempt: network-restricted NuGet access failed (`NU1301`). Locked restore
  with authorised network access succeeded; no dependency changes.
- Migration generation initially refused missing explicit connection configuration. With an unused
  explicit model-only path it succeeded via the design-time factory; application host discovery reported
  an in-use/maintenance refusal. No existing store was reset or migrated. This is not a deployment test.
- First solution build: passed, zero warnings/errors.
- First focused evidence execution: **22 passed, 0 failed, 0 skipped**, reported duration **1m58s**.
  This preceded the later encrypted-recovery and additional transition/missing-blob/model assertions.
- Second focused execution: **23/23 passed**, **2m16s**, adding actual encrypted recovery.
- Third focused execution: **29/29 passed**, **2m53s**, adding bind-first real transitions,
  missing-blob and model/snapshot checks. Its subsequent separate complete gate passed
  **144/144**, **10m47s**. These preceded the final transport hardening and expanded race cases.
- Expanded focused execution: **38 passed / 1 failed**, **3m42s**, no skips. The null-selection
  assertion passed, but an oversized body expected 413 and received 500. Inspection confirmed
  the global MVC filter treated Kestrel's request-too-large exception as an unexpected failure.
  A narrow 413-only mapping now returns fixed safe text and expected-outcome logging; limits and
  assertions are unchanged. After rebuilding, the affected HTTP test passed **1/1**, **7s**.
- Source review also found that a failed eligibility refresh could unmount a selected draft editor.
  The corrected UI keeps it mounted but hidden/disabled during errors and restores the same reviewed
  selection on explicit retry. A controlled browser barrier retains this behaviour; it is not
  presented as a reproduced production incident.
- Initial focused browser execution reported timeouts; the safe reporter withheld the original
  assertion/call details. Inspection found the helper used a full `page.goto()` after login,
  discarding HRFlow's deliberately memory-only session. Independent safe
  HTTP probes returned 200 for login/history/timeline/eligibility and counted five clean fixture
  drafts. The helper now follows actual My Leave → View History SPA links and asserts the route.
  No persistence feature, retry or timeout increase was added. This is a demonstrated helper defect,
  not a claim that each original timed-out call was captured. Complete failed-run totals and corrected
  executions are recorded separately after completion. The fresh rerun uses the corrected helper;
  editing a test file while the first runner remained active is not counted as a fresh execution.
  That first run ended **0 passed / 6 timed out**, **376214.994 ms**, retries disabled. A separate
  fresh isolated first-case execution passed using a temporary privacy-safe reporter which emitted
  only the fixed test title/status and numeric test-source locations; raw error/call details and
  attachments remained excluded. Temporary diagnostic tooling and probe data are not product files.
- Frontend build passed (839 modules); entry 360.42 kB / 112.56 kB gzip. Vite reported plugin timing
  overhead. Lint exited zero with the existing `useAuth.tsx:223` Fast Refresh export warning.
- Browser TypeScript check passed. Full client and browser dependency audits exited zero; solution
  vulnerability scan exited zero: all six .NET projects and both npm projects report no known
  vulnerabilities from the queried sources. No package/lock changes. Exact final counts and later gate executions are recorded below
  after completion; the first focused run is not presented as a final aggregate.

## Retained coverage

`EvidenceAdditionTests` exercises owner Employee/Manager/combined bindings; captured modes and
Required Medical refusal; stale/checksum/missing/nonclean/wrong-owner/already-bound/duplicate/class
denials; current Medical redaction; identical/changed-key recovery; explicit separate-API-process
entry barriers and last-slot concurrency; transition-first refusal and bind-first real lifecycle
preservation; injected late SQLite failure; model consistency and actual encrypted isolated restore.
Persisted bindings, outcomes, action rows, snapshots and transition audits are inspected.

`evidence.spec.ts` labels real HTTP binding over controlled clean fixtures separately from held/lost
HTTP response tests. No screenshots, traces, messages, tokens or raw Playwright errors are retained.
The normal safe reporter remains unchanged. Browser setup generates five clean private fixture drafts;
it does not install a scanner or bypass production authorization. Existing migration preservation
also asserts that no addition history is invented for legacy rows.

## Corrected final executions

- Final solution build after the 413 correction: **passed, zero warnings/errors**.
- Corrected complete backend gate: **154 passed / 0 failed / 0 skipped**, **10m48s**. This is one
  actual complete execution, separate from the earlier 144 pass and 38/1 focused runs. It includes
  all 39 addition cases plus the 115 existing gates, migration preservation/model consistency and
  stopped-writer encrypted recovery. No browser/API fixture suite ran alongside offline tooling.
- Fresh isolated first browser case: passed with safe numeric diagnostics; it is not added to another
  run's test count.
- Corrected focused Chrome browser invocation: **6 passed / 0 failed**, **35165.438 ms**. Real HTTP
  binding/history/download uses controlled clean metadata. Real HTTP upload with the scanner absent
  yields ScanUnavailable and no selection/download capability. Lost acknowledgement, refresh error
  and session-switch responses are labelled controlled transport cases. Desktop keyboard/focus,
  actual light/dark theme attributes and 390px mobile no-overflow assertions passed.
- Latest frontend build, lint and browser typecheck passed. The existing Fast Refresh warning remains;
  no dependency, database-provider or CI changes. The additive table migration is described in the ADR.
- Separate fresh **complete default Chrome suite: 57 passed / 0 failed**, **195161.419 ms**. This
  includes all six evidence checks and the 51 existing browser gates. No retries, skips, assertion
  weakening or timeout increases. It is not a production-built or hosted/Linux browser execution.
  Owned fixture cleanup completed; no fixture API processes remained after the runs.

Final review checks: 25 intended modified/new files; `git diff --check` and new-file whitespace/UTF-8/
sensitive-pattern checks passed; 64 relative documentation links resolve, and both instruction files
are byte-identical. Complete review patch includes all new files and passes reverse-apply checking.
No files are staged; no commit, push, PR or issue edit. Pattern checks are not a comprehensive secret
audit, and external reference URLs were not batch revalidated.

### Commands

Separate invocations from repository root; use stopped fixture writers for backend/operator gates:

```powershell
dotnet restore HRFlow.sln --locked-mode
dotnet build HRFlow.sln --no-restore -v minimal
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~EvidenceAdditionTests --logger 'console;verbosity=minimal'
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~NullSelectionAndOversizedBodyFailSafely --logger 'console;verbosity=minimal'
dotnet test tests/HRFlow.RegressionTests --no-build --logger 'console;verbosity=minimal'
npm.cmd --prefix src/HRFlow.Client run build
npm.cmd --prefix src/HRFlow.Client run lint
npm.cmd --prefix tests/browser run typecheck
npm.cmd --prefix src/HRFlow.Client audit --json
npm.cmd --prefix tests/browser audit --json
dotnet list HRFlow.sln package --vulnerable --include-transitive
$env:HRFLOW_BROWSER_CHANNEL='chrome'
npm.cmd --prefix tests/browser test -- evidence.spec.ts
npm.cmd --prefix tests/browser test
```

The isolated diagnostic invocation used the same first-case grep with a temporary private reporter;
it is excluded from the deliverable and never prints raw errors or personnel/credential payloads.
Screenshots/traces/video remain disabled. No scanner binary, upload, database, runtime descriptor,
private key, test log or temporary verification tool belongs in the change.

## Outstanding checks

Positive real-ClamAV upload/scan/download is unavailable here; real scanner outage must continue to
fail closed. Hosted CI, Linux-specific new checks, target-host volumes/recovery, real screen-reader/
physical-device review, exhaustive disk faults and retention/legal review are not claimed.
No production delivery, statutory/payment work, bound-document removal or request-date edits.
Rollback drops later-addition provenance/idempotency; downgrade execution is not claimed.

## Finalisation review - 22 October 2026

Fetched main remains the recorded base. Issues #83/#84 were read and remain open. Complete source,
migration, tests and documentation were reviewed before initial publication. Hosted recovery then
exposed the creation-mode correction described below.
Staged whitespace, UTF-8/encoding, relative documentation links, sensitive-pattern inspection and
byte-identical instruction checks passed over the 25 intended files. Private fixtures and generated
output are excluded. Product suites above are earlier implementation executions, not finalisation
reruns. Hosted results are reported separately on the PR; local results do not establish Linux or
positive real-ClamAV coverage. Downgrade execution remains unperformed.

## First hosted execution and scoped correction

PR #135 [run 39734973984](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/39734973984)
completed **153 backend passed / 1 failed**, **8.4801 minutes**. Browser gates were skipped, not passed.
`EncryptedRecoveryPreservesAdditionAndBoundBytes` failed at the backup command (exit 1, not timeout).
Safe diagnostics identify the test and source location; child output did not expose the exact refusal.
Inspection found document storage omitted explicit private creation modes for the `documents` parent
and blob/lock/tombstone files. Unlike the image pipeline, these depended on Unix defaults; strict
backup validation rejects broader modes. This concrete omission is corrected using the existing
private-image creation convention: owner-only directories and new files, unchanged Windows private
parent ACL inheritance. Existing file permissions are not silently repaired; validators remain strict.
Retained checks now inspect exact Unix modes and restricted Windows ACLs before backup and after
restore, plus private removal markers and denial of publication after removal.

The correction built with zero warnings/errors. A mistakenly restricted-context focused execution
then completed **1 passed / 39 failed / 0 skipped**, **12m56s**, with premature HTTP responses at
the fixture send method. The directly exercised storage assertion passed. This failure is retained;
restricted execution is a suspected environment cause, not proof explaining every failed call.
Cleanup left zero fixture API processes before a separate authorised-network invocation.

Separate authorised-network focused invocation against the same corrected build: **40 passed / 0 failed /
0 skipped**, **13m50s**. This includes actual encrypted recovery, explicit private-path assertions and
publication/removal checks. No assertion, timeout or retry changed between these two invocations.
The result supports environment dependence but does not reconstruct every premature-response cause.
The earlier complete local 154 pass remains earlier evidence; the corrected complete hosted gate is
reported separately. No frontend file changed for this correction, so local browser/build/lint were
not rerun. Positive real-ClamAV evidence remains unavailable.

## Hosted budget investigation - 26 October 2026

The cancelled [run 39739778872](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/39739778872)
is preserved: **155/155 backend passed**, **10.3311 minutes**; **13 browser passed**, no reported
browser failure, no complete browser result. The job exhausted its shared 20-minute budget.
The earlier PR description/summary's suggestion of a browser stall was unsupported. The next test's
position does not implicate the directory mutation test or establish a hang; the new evidence cases
were not reached. No original failure or cancelled run has been removed or counted as a pass.

Inspection on 26 October used the original timestamped installation output from 22 October, not a
new execution. All times below are UTC, not South African business dates:

- Job began **07:20:15**; backend step **07:21:15-07:31:37**.
- Combined browser setup/test step began **07:31:37**. Its npm clean-install completion output was
  at **07:36:37.468**, about **5m00s** into the step.
- Explicit npm audit's zero-vulnerability output appeared at **07:38:33.476**, about **1m56s** later.
  These output intervals identify where the budget went; they do not establish a registry/network cause.
- Browser typecheck script began **07:38:33.585**. Playwright OS dependency installation began
  **07:38:35.931**; apt progress spans **07:38:36.028-07:39:36.488** (about one minute).
- Chromium download began **07:39:39.037**; the last browser asset download completed
  **07:39:48.347**. All actual OS/browser prerequisites were installed, not assumed cached.
- **playwright test** began **07:39:48.505**, roughly **8m11.5s** after the combined step began.
  Passing results span **07:40:02.257-07:40:27.464**. The step was cancelled **07:40:29** and the job
  finished **07:40:33**. Only about **40.5 seconds** were available after test command startup.

### Focused CI correction

The existing [workflow](../../.github/workflows/critical-regressions.yml) now runs independent backend
and browser jobs, each bounded at **20 minutes** on a fresh Ubuntu 24.04 runner. Each builds its own
API/fixture executable from locked restore with vulnerability warnings treated as errors. Only the
backend job runs the entire xUnit gate; only the browser job runs the entire default Chromium gate.
The browser job retains client clean install/build/lint/audit, browser clean install/audit/typecheck
and real Playwright Chromium/system installation, with separate timed steps for setup and execution.
No test filter, assertion, per-test timeout, retry, skip or product source changed for the budget split.
The separately necessary audit correction below changes only one lockfile resolution.

A bounded two-minute final **regression** job preserves the existing check name and requires both
suite jobs to succeed; failed, cancelled or skipped suites cannot yield a green aggregate. Branch
protection, workflow triggers, read-only permissions and pinned actions are unchanged. There is no
cache, artifact upload or fixture transfer between jobs: private SQLite stores, credentials, documents,
messages and runtime descriptors remain confined to their own runner. Existing fixture shutdown,
owned-process-tree cleanup, exact private path validation and fail-closed scanning remain unchanged.

Based on the observed roughly 10m backend and 8m browser setup plus earlier complete browser timing,
independent budgets provide headroom without increasing a test timeout or assuming cached packages.
This is a justified execution-budget change, not a proven product or browser-stall correction.
Final hosted execution is recorded separately below once inspected. Positive real-ClamAV scanning
remains outstanding; controlled clean fixtures and scanner-unavailable refusal cannot substitute.

### First independent-jobs run and retained audit refusal

[Run 40310846750](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/40310846750)
on **54a9d03608e57d548e198d69a9ada57e98c93cf5** completed with **155/155 backend passed**,
**9.5655 minutes** test time (job **10m20s**). The browser job failed after one minute at the retained
client audit; browser installation/tests were not run. The aggregate correctly failed. This is a
separate failed run, not a browser assertion failure, stall or budget cancellation.

The audit reported one high finding for direct client `zod@4.4.3`,
[GHSA-mrhx-c7xc-m3w5](https://github.com/advisories/GHSA-mrhx-c7xc-m3w5)
(CVE-2023-54404; affected below 4.6.6, patched 4.6.6). The official advisory, updated 23 October,
and [maintainer 4.6.6 release](https://github.com/colinhacks/zod/releases/tag/v4.6.6) were retrieved
26 October; the release lists checking collection size bounds before parsing elements. Registry
metadata independently confirms the version, public tarball URL and SHA-512 integrity.

Dependency path: client root -> zod, also satisfying @hookform/resolvers' Zod peer. There is one
resolved copy. It supports browser employee/login form validation; this is client runtime exposure,
not a .NET server dependency. No claim is made that the current UI reproduces the advisory's resource
exhaustion. The focused compatible fix changes only the Zod lock entry's version/tarball/integrity
**4.4.3 -> 4.6.6**, within unchanged `^4.4.3`; no override, suppression, forced upgrade, new package
or unrelated resolution. The browser project's lockfile and all manifests remain unchanged.

A guarded edit first refused the lockfile's line endings; the following unpatched clean install/audit
reproduced **one high** finding and is not counted as patched verification. After the focused edit,
separate fresh client/browser `npm ci` passed. Client full and production-only audits and browser full
audit each report **zero vulnerabilities**. Installed and locked Zod both equal **4.6.6**. Local client
build, lint and browser typecheck passed; existing Fast Refresh warning remains. No local product
suite was rerun for the workflow-only split; complete suites will execute against the patched final
head in hosted CI. No private fixture artifacts were cached or uploaded.

### Complete browser result after the budget correction

[Run 40312586376](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/40312586376)
on **fa8e01b64b035fca1793a73c349407dce47df448** completed **155/155 backend passed**
(**6.3970 minutes**) and **56 browser passed / 1 failed** (**142884.173 ms**). The audit and real
Chromium/system installation passed. This is a complete failed browser invocation, not a budget
cancellation: the first real HTTP evidence case failed after **2816 ms**, not a timeout. The other
five evidence cases passed. Original raw assertion details were withheld by the privacy-safe reporter.

To identify that assertion without raw errors, the reporter now extracts only bounded line/column
coordinates for the fixed relative filename `evidence.spec.ts`; no stack text, page contents, payloads
or private paths are emitted. A retained pure contract test checks that canary text/paths and invalid
coordinates cannot enter output. Existing tests/assertions/timeouts remain intact. A first typecheck
reported TS2835 for the helper's extensionless import; adding the required `.js` extension corrected
it, and typecheck passed. Separate local Chrome diagnostic-contract plus affected workflow invocation:
**2 passed / 0 failed**, **57678.29 ms** (affected workflow **37057 ms**). That isolated local pass
does not reproduce or explain the hosted assertion failure. No retry was configured.

### Separate diagnostic executions and replacement-destination correction

Complete local Chrome execution with the source-only diagnostics: **55 passed / 3 failed**,
**347577.063 ms**. Failures were the HR replacement evidence case and the existing Employee-to-Manager
workflow and HR monitoring cases. The safe evidence coordinate was `evidence.spec.ts:117:95`: the
absence-of-evidence-panel assertion, not a leaked success notice. The original two workflow source
coordinates were not retained; they must not be reconstructed from later passes.

Source inspection shows login can restore an authorised return route. The evidence session test now
waits for logout's login route, explicitly navigates the replacement account to Overview and asserts
that destination before releasing the held old response. Both original absence assertions remain
unchanged. This establishes the tested destination instead of treating legitimate fresh HR history
as proof of stale content. It does not change production session handling or claim that return-route
behaviour explains every earlier failure. Workflow source coordinates are now allowlisted too;
only the two fixed relative test filenames and bounded numbers can be printed.

Separate local focused invocation after that correction: **5 passed / 0 failed**, **38230.229 ms**,
covering the privacy contract, both replacement cases and both earlier failing existing workflows.
No retries, skips or timeout increases. The two existing workflow failures did not reproduce in that
focused execution; a later pass alone does not establish their cause.

The browser job in [run 40314124265](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/40314124265)
on **0e460d88b61b3589ebc3f17c3c4d5702b80760c6** completed **58 passed / 0 failed**,
**142530.797 ms**, with no retry. This preceded the explicit-destination correction above; it is
separate hosted evidence, not a local aggregate or proof of a root cause for the original failed case.
Fresh Ubuntu browser job **11:18:49-11:26:36 UTC**, **7m47s**:

- API fixture restore/build **45s**; client install/build/lint/audit **14s**.
- Browser clean install/audit/typecheck **3s**.
- Actual Chromium/system installation **11:20:03-11:24:09**, **4m06s**.
- Complete browser gate **11:24:09-11:26:32**, **2m23s**, including owned fixture teardown.

The same run completed **155/155 backend passed**, **10.0829 minutes** test time, backend job
**10m49s**. Both suite jobs and the final `regression` aggregate passed; overall **10m55s**.
This is one actual complete hosted workflow, distinct from every local and earlier failed execution.
The subsequent follow-up changes only the explicit test destination, workflow-source diagnostics and
this report. Its own head must independently pass hosted gates before review is concluded.

The browser setup variation reinforces separate bounded budgets; no test stall was established.
The diagnostic contract adds one pure case (57 -> 58), not a replaced or omitted workflow. No
private artifact was cached/uploaded. Positive real-ClamAV remains unexecuted, independent of these
controlled clean fixtures and fail-closed outage checks. Final-head hosted status is reported on PR #135.

### Completed corrected-code hosted verification - 26 October 2026

[Run 40315812145](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/40315812145)
on **9ebec54846ae824c6e535007ea6f7249d7872ba6** passed both complete suites and the required aggregate:

- Backend **155 passed / 0 failed**, **9.9232 minutes** test time; job **10m39s**.
- Chromium **58 passed / 0 failed**, **133035.99 ms**; browser job **15m17s**.
- Overall workflow **15m21s**; aggregate success. No retry, skip, timeout increase or assertion removal.
- Browser fixture build **33s**, client install/build/lint/audit **11s**, browser install/audit/typecheck
  **2s**. Real OS/Chromium installation **11:33:23-11:45:24 UTC**, **12m01s**.
- Within installation, apt progress spans **11:33:24.296-11:45:16.030**; browser downloads begin
  **11:45:17.652** and finish **11:45:24.572**. The long interval is OS prerequisite work, not a
  browser test or the Chromium asset transfer. No specific mirror/network cause is established.
- Complete browser gate **11:45:24-11:47:38**, including normal private fixture teardown.
  No fixture artifact was cached/uploaded; all builds/audits/typecheck and permissions remained enabled.

The original budget cancellation, high-audit refusal, 56/1 hosted run and 55/3 local run remain above.
The first hosted assertion failure was not reproduced in the later complete invocations; neither
green execution establishes its original cause. The explicit destination correction addresses the
demonstrated local test assumption without changing product authorization/session behaviour.

This records the tested source/test/CI head before the report-only commit. That subsequent head's
required workflow is inspected separately and linked from PR #135; no prior result substitutes for
it. The report-only update does not alter executable inputs. Positive real-ClamAV remains outstanding;
controlled clean fixtures and scanner-unavailable denial do not prove successful real scanning.
Production-built browser, physical device/screen-reader, target-host, downgrade, retention/legal and
provider checks are not implied. Production delivery remains disabled. No merge or deployment.

### Later complete run identifies a layout-readiness assertion

The report-only [run 40318093850](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/40318093850)
on **720057154bbeb17124da34c986949cd25bbf22e4** completed **155 backend passed**
(**10.1891 minutes**) and **57 browser passed / 1 failed** (**142494.97 ms**). The safe diagnostic
now identifies **evidence.spec.ts:58:89**: the immediate `scrollWidth <= innerWidth` assertion after
changing the viewport/theme. That case failed in **2826 ms**, not a timeout or stalled test. The
original 56/1 run did not retain its source coordinate, so this later result must not be presented
as reconstructed original call details.

The corrected test waits with bounded `expect.poll` for the identical width condition, using the
existing configured 10-second assertion budget and unchanged 60-second test limit. Test retries remain
zero; no product CSS, overflow threshold or mutation replay changes. A controlled case explicitly
holds the document at 640px with a 390px viewport, asserts
that it is too wide and the waiting check has not completed, then releases only that owned CSS
barrier. Persistent overflow still fails. This separates responsive layout readiness from the
theme attribute becoming visible and uses the existing directory-test readiness pattern.

New barrier cleanup initially failed typecheck (TS2339: Node has no remove); using the owned node's
parent removal corrected it, then typecheck passed. Separate complete evidence-file local Chrome
invocation: **8 passed / 0 failed**, **53519.882 ms**, including the actual workflow, scanner-unavailable
denial, both replacement sessions, recoverable refresh, lost acknowledgement and privacy contract.
The explicit barrier is an additional case (58 -> 59 complete default cases), not removed coverage.
The earlier complete successes, later failure and unknown original details remain recorded.

Separate subsequent complete local Chrome invocation after the layout correction: **59 passed /
0 failed**, **220304.934 ms**, process exit zero. Owned fixture cleanup left zero API processes.
This is not combined with the 8-case invocation or any earlier complete run. The new hosted head
must run both complete suites; its result is linked from PR #135. No production source changed.
