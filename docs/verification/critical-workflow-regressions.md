# Critical workflow regression phase: executed evidence

14 October 2026. Branch test/critical-workflow-regressions, base
3bff3f25ff3b5316077527d7d7fe733381ef85f6. Initial working tree was clean. Fetched origin/main;
PR #116 was confirmed merged at 16:55:16 UTC. #76 remains open as a first focused slice.
Explicit user authorisation narrows the prior automated-test deferral; instruction copies remain identical.

## Implementation-stage local execution (prior evidence)

Actual Windows environment: SDK 9.0.305, .NET runtime 8.0.23, Node 24.12.0; installed Chrome controlled
by Playwright Test 1.64.1. Disposable file-backed SQLite, real migrations, synthetic Identity fixtures,
two independently running API processes and bounded lifecycle/network barriers. No copied employee
store, real recipient, diagnostic API, EF InMemory, scanner substitute or production weakening.

Implementation-stage sequential commands and results after restoring the negative controls:

- `dotnet build HRFlow.sln --no-restore`: **passed, 0 warnings/errors, 3.60s**.
- `dotnet test tests/HRFlow.RegressionTests --no-build --logger 'console;verbosity=normal'`:
  **35 passed, 0 failed/skipped, 3.7255 minutes (223.53s)**. All fixtures were newly created.
- `$env:HRFLOW_BROWSER_CHANNEL='chrome'; npm.cmd --prefix tests/browser test`:
  **9 passed, 0 retries, 112.141s**. Fresh API/SQLite/client setup after backend execution.
- Clean browser `npm.cmd --prefix tests/browser ci --ignore-scripts --no-fund --no-audit`:
  **3 packages installed**, followed by another fresh suite run: **9 passed, 0 retries, 58.972s**.
- `npm.cmd --prefix src/HRFlow.Client run build`: **passed, 831 modules, 3.08s**;
  main 597.95 kB / 175.88 kB gzip. Existing Vite >500 kB warning remains.
- `npm.cmd --prefix src/HRFlow.Client run lint`: **exit 0**, existing
  useAuth.tsx:223 Fast Refresh export warning remains.
- `dotnet restore HRFlow.sln --locked-mode -p:NuGetAuditMode=all -warnaserror:NU1901,NU1902,NU1903,NU1904`:
  **passed** with the retained test lockfile. No forced/production dependency upgrades.
- `dotnet list HRFlow.sln package --vulnerable --include-transitive`: **all six projects report no
  vulnerable packages** in configured feeds. Client npm audit: **0 findings, 193 dependencies**;
  separate browser npm audit: **0 findings, 3 dependencies**, including after clean install.
- `dotnet ef migrations has-pending-model-changes --project src/HRFlow.Infrastructure --startup-project
  src/HRFlow.Api --no-build`, with a generated disposable explicit connection: **no changes since last migration**.
- Complete working/new-file whitespace, scope, sensitive-content and instruction checks are recorded below.

Exact supported setup commands and the coverage matrix are in [tests/README.md](../../tests/README.md).
All framework/package/action pins are reviewable. Implementation-stage results are local evidence,
not hosted CI evidence. Finalisation and hosted status are recorded separately below.

## Backend persisted-outcome coverage

Four capability combinations; current Employee/Manager/HR role removal, inactive and pending-account
denial; combined HR removal preserves independent personal/Manager capability. Self, unrelated,
cross-department and HR decisions are denied with Pending state and no terminal audit.

Approvals revalidate balance and same-type overlap after earlier submissions. Approve/reject/owner
cancel each persist one matching actor/state audit; repeated calls conflict without duplication.
A disposable AFTER INSERT audit failure trigger rolls back status, terminal audit and notification
outbox count. No production failure flag/endpoint exists.

Original employee/policy expectedVersion conflicts preserve newer data and reporting links. Normalized
email/employee-number creates race across two real API processes; one Identity/profile is persisted,
not orphan accounts. Repeated concurrent CSV confirmation uses server-held rows and the same durable
key, inspecting one Identity/profile and one Created outcome in the batch.

Cross-process balance, overlap, same-request approve/reject, duplicate creation and CSV confirmation
use an independent BEGIN IMMEDIATE connection plus counted endpoint-entry barriers before release.
Manager/HR permission loss is committed while a protected operation is entering behind the writer;
its later protected validation denies it without writes. Readiness polling does not select race winners.
These are real separate connections/processes, not Task.WhenAll-only or in-memory lock claims.

The migration gate generates the schema at 20261002094357 from real migration code, inserts synthetic
original Identity/profile/Approved request/decision audit rows, then upgrades. Original IDs, names,
password hash, role link, dates/status/actor/timestamp survive; newly introduced number/start date,
schedules, description/requirements/correlation/note fields remain honestly Unknown/null. It asserts
no invented submission event. No committed database or statutory calculation fixture.

## Browser coverage: real HTTP versus controlled transport

Real HTTP interactions: Employee submits the actual form, assigned Manager confirms approval,
Employee sees Approved history; the authorised persisted timeline has one Submit and Approve and
correct actor. HR monitors an actual Pending row without decision/personal controls. A real competing
HR edit produces HTTP 409; the opened form retains its draft until explicit Reload latest values.

Controlled route barriers: held real dashboard responses across different/same-account relogin; stale
refresh success (actual server rotation) or injected refresh failure cannot replace/clear a newer
same-account session. A subsequent actual request's Authorization equals the new login token,
compared as a boolean to avoid printing tokens. Non-replayable submission 401 makes one POST/no
refresh; delayed old mutation 401 cannot end a replacement HR session. These are controlled
transport gates, not actual token-expiry, packet-loss or all network failure claims.

The safe reporter prints only test identity/status/duration. Screenshots, video and traces are off;
automatic error-context attachments are deleted, no artifacts uploaded. API process output is not
retained; only safe endpoint-entry signals are consumed. Runtime credentials/state are restricted
and deleted on normal teardown. No medical files/content are present in this suite.

## Negative controls: gates demonstrably reject defects

Temporary private verification tooling removed one stale-version condition at a time, rebuilt the
API, and ran the retained relevant test. It required failure at the exact conflict assertion, not
an unrelated startup failure:

- StalePolicyEdit: **failed as expected, exit 1, 14.740s**, Expected Conflict / Actual OK.
- StaleEmployeeEdit: **failed as expected, exit 1, 12.585s**, Expected Conflict / Actual OK.

Original production file bytes were restored in finally blocks and rebuilt (4.135s). Final
`git diff -- src` is empty; the subsequent complete 35/9 suites above use the correct implementation.
No intentional defect, authorization bypass, migration, schema or product-source change remains.

## Initial corrections and repeatability limits

The initial foundation passed 26 backend cases (2.4164 minutes) and 8 browser cases (31.949s).
An expanded fixture build then caught an out-of-scope cancellation-token reference; it was fixed.
The mistakenly chained --no-build run used the earlier 26-case binary (4.1437 minutes); that output
is not evidence for the final expanded suite. Later successful expanded runs contained 35 cases,
including one run at 5.2370 minutes before final fixture publication/cleanup refinements.

A nine-case browser development run had one failing harness expectation (82.237s total): HR login
returned to the guarded personal URL rather than immediately displaying HR Overview. Explicit
Overview navigation corrected the harness without changing product permissions or weakening assertions.
It is not reported as a passing run. Review also removed a potential partial readiness-file read by
publishing restricted runtime state atomically, and eliminated released/re-picked API ports using
Kestrel port 0. Initial test-project entry/analyzer warnings were corrected; final build has none.

After these corrections, the final 35/9 sequential run had no assertion failures or automatic retries.
Setup/runtime durations varied, especially when suites ran concurrently; use sequential commands.
A small number of local runs is not proof of zero future flakiness or of hosted Linux equivalence.

## Outstanding scope

Hosted GitHub CI/Linux and bundled Chromium installation/execution are unverified. Medical/scanner/
quarantine/recovery, comprehensive accessibility/mobile/theme, load/deployment, full JWT expiry/
not-before/skew/version/refresh boundary matrices, all activation/password/recovery failures, other
same-request race pairs, full notification and CSV cancellation/delivery-failure matrices, report/export
metrics and other read-endpoint ownership matrices remain uncovered by this retained slice.
Prior manual verification remains prior evidence, not new retained coverage. #75/#22-qualified statutory
calculations stay unimplemented/unverified. #76 stays open. No production-readiness or compliance claim.

[Strategy ADR](../adr/0023-focused-regression-gates.md). No merge/deploy/issue closure.

Final review strengthened exact original Identity/profile/role-link ID assertions in the migration gate. Only that affected gate was rerun after rebuilding; its actual result is recorded below, rather than claiming another full 35-case run.

Affected migration rerun: **1 passed, 0 failed/skipped, 6.6481s**; preceding solution build **0 warnings/errors, 20.11s**. Clean-install browser repeat: **9 passed, 58.972s**, again with zero retries. Working/new-file checks and instruction equality passed; no product-source diff or staged changes.

Privacy refinement: browser output was moved under the restricted owned fixture root so automatic failure context is private even before deletion. The affected full browser suite rerun passed **9/9, 0 retries, 93.240s**. `npm.cmd --prefix tests/browser run typecheck` passed using the existing client TypeScript compiler/types, with no extra dependency. Runtime state publication is atomic; API process inspection found no remaining fixture API instances after teardown. Hosted CI remains unexecuted.

## Independent-review finalisation (14 October 2026)

Concrete review fixes are limited to test tooling: validate the owned browser temp root before
permission changes; attempt cleanup of every owned API process even if another fails, retaining
private stores if a writer cannot be stopped; give browser shutdown a bounded owned-tree/group
fallback after graceful stdin cancellation. No production source or migration changes.

The **final combined suite was rerun sequentially after these fixes** (not an aggregate of historical runs):

- `dotnet build HRFlow.sln --no-restore`: **0 warnings/errors, 8.36s**.
- `dotnet test tests/HRFlow.RegressionTests --no-build --logger 'console;verbosity=normal'`:
  **35 passed, 0 failed/skipped, 2.9353 minutes (176.118s)**, fresh migrated synthetic stores.
- `npm.cmd --prefix tests/browser run typecheck`: **passed**.
- `$env:HRFLOW_BROWSER_CHANNEL='chrome'; npm.cmd --prefix tests/browser test`:
  **9 passed, 0 failed, 0 retries, 51.052s**, fresh real API/client fixture.
- A separate temporary stdin-EOF cancellation probe: **passed**, bounded fixture exit, no new
  fixture store left and no credential descriptor published. No temporary probe is committed.
- Solution-wide transitive vulnerability check: **all six projects report no vulnerable packages**.
  Client and browser npm audits: **0 vulnerabilities** each.
- Official checkout/setup-dotnet/setup-node v5 refs reconfirmed against the pinned commits.
- Production source equals base 3bff3f25ff3b5316077527d7d7fe733381ef85f6 after mutation experiments.
  Intended-file whitespace, token/path/artifact scans and instruction byte equality passed.

The combined test execution took about **3.8 minutes**, plus build/typecheck/setup command overhead;
this is a local measurement, not a hosted runtime guarantee. Earlier deliberate stale-policy and
stale-employee mutations failed as recorded above and were restored before this final run.
Frontend build/lint and model checks were **not rerun during finalisation**: their prior executed
results remain above; no production/client/model files changed. No skips or automatic retries.

Normal teardown left no owned fixture API processes. Forced termination fallback, abrupt OS kill
and all listed broader coverage gaps are not claimed verified by the stdin-cancellation probe.
Hosted workflow status must be inspected after PR creation; local Windows/Chrome results do not
prove Linux/bundled Chromium. Screenshots/traces/video and artifact uploads remain disabled for
credential/privacy safety. No merge, deployment, issue closure or production/compliance claim.
