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

## Finalisation review ? 22 October 2026

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
