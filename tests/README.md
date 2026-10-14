# Critical workflow regression gates (#76 first slice)

Use .NET 8 runtime plus a compatible SDK and Node 24. Local execution used SDK 9.0.305,
.NET runtime 8.0.23 and Node 24.12.0 on Windows. CI selects SDK 8 and Ubuntu 24.04; hosted
execution remains unverified. Build the solution in Debug before starting these fixtures.

## Repeatable local commands

From the repository root (use npm.cmd/npx.cmd in Windows PowerShell if npm.ps1 is blocked):

```sh
dotnet restore HRFlow.sln --locked-mode -p:NuGetAuditMode=all -warnaserror:NU1901,NU1902,NU1903,NU1904
dotnet build HRFlow.sln --no-restore
npm ci --prefix src/HRFlow.Client
npm run build --prefix src/HRFlow.Client
npm run lint --prefix src/HRFlow.Client
dotnet test tests/HRFlow.RegressionTests --no-build --logger 'console;verbosity=normal'
npm ci --prefix tests/browser
```

Then, in tests/browser:

```sh
npm run typecheck
npx playwright install chromium
npm test
```

Local fallback to an already installed Chrome (the actual browser used for this report):

```powershell
$env:HRFLOW_BROWSER_CHANNEL='chrome'
npm.cmd --prefix tests/browser test
Remove-Item Env:HRFLOW_BROWSER_CHANNEL
```

CI installs bundled Chromium and its OS dependencies. Local Chrome interaction does not prove
that hosted Linux/bundled-browser execution passed. Run backend and browser suites sequentially
to reduce startup contention. Retries are disabled; a failed test is not hidden by automatic retry.
The fixture CLI/shared harness requires Debug build outputs; a test-only project build is insufficient
because the real API must also be built. No scanner installation is required for these document-free gates.

Audits/diff checks:

```sh
dotnet list HRFlow.sln package --vulnerable --include-transitive
npm audit --prefix src/HRFlow.Client
npm audit --prefix tests/browser
git diff --check
```

NuGet restore treats vulnerability warnings as errors; the test project has a lockfile. Browser TypeScript checking reuses the existing client compiler/types (no additional compiler dependency). Playwright
has its own minimal manifest/lockfile, separate from production client dependencies. Existing frontend
Fast Refresh and large-chunk warnings are documented rather than hidden by test setup.

## Fixture and failure handling

Each backend case creates a new restricted private directory, file-backed SQLite database and
actual migration history. It inserts synthetic Identity records using Identity's normalizer/password
hasher, with cryptographically generated runtime passwords/security stamps. All Development seed
password settings are explicitly supplied with generated secrets; no default credentials are used.
The four canonical Development identities are pre-provisioned alongside synthetic negative/combined
actors so startup follows real existing seeding, not a new bypass. No existing databases are copied.

Two independent API processes share only the case's database/private root. Kestrel assigns ports
atomically from port 0. Browser setup temporarily reserves its selected loopback port, creates a new
C# fixture, then starts the real Vite client. HTTP is Development loopback only; this is not TLS or
Production-mode verification. No external delivery provider or real recipient is used. CSV creation
uses actual Development private pickup under a unique owned profile, removed during cleanup.

HTTP calls are bounded at 15 seconds, API readiness at 30 seconds/process, endpoint-entry barriers
at 10 seconds, browser expectations at 10 seconds, browser tests at 60 seconds, and process exits
at 5 seconds (browser graceful host shutdown at 8 seconds, with a bounded owned-tree fallback). Readiness polling is not race coordination.
The CI job is bounded to 20 minutes. Cases do not rely on fixed listening ports or sleep durations
to choose a winning transaction.

API routing lifecycle lines signal endpoint entry without retaining request/response data. An independent
SQLite BEGIN IMMEDIATE connection holds the writer while both requests enter, then explicitly releases
it. This proves separate connections/processes participate in the production transaction protocol.
It is not a simulated in-memory lock. Late failures use an AFTER INSERT audit trigger in the disposable
SQLite store only; production has no diagnostic endpoint, failure flag, auth bypass or schema change.

The C# owner stops its API process trees before cleaning only generated fixture/pickup directories;
paths are checked against owned parents. Every process cleanup is attempted; if a writer cannot be stopped,
private stores are retained rather than deleted under it. Browser host stdin shutdown also cancels startup;
a timeout uses only its owned Windows process tree or POSIX process group. Private directory validation
precedes permission changes. Abrupt
OS termination can leave private temporary directories or processes: inspect ownership before manual
cleanup; never delete a broad temp/database root or use an existing employee store as a fixture.

Screenshots/video/traces are off. The safe reporter prints only test titles/status/duration, deletes
payload-bearing Playwright attachments (including automatic error-context), keeps output under the restricted private fixture root, and does not print error
call logs or upload artifacts. API logs are discarded, not saved. Browser runtime state containing
credentials has restricted file permissions and is removed after teardown. Generated output and
node_modules are ignored. Do not override tracing or upload private fixture directories in CI.

## Coverage matrix (retained gates)

- Role capability composition: CapabilityMatrix (Employee, Manager, HR-only, combined); CombinedRoleRemoval.
- Current permissions/lifecycle: LiveAccessDenial for each role, inactive/pending denial; HR and Manager RevocationBehindWriter.
- Manager relationship/no HR decisions: DecisionScopeDenial for HR, self, non-report and cross-department; persisted Pending/audit inspection.
- Fresh policy approval: ApprovalRevalidates balance/overlap; CrossProcessBalance for concurrent balance and overlap using both API processes.
- Transition/audit integrity: ExactlyOneAudit approve/reject/owner cancel; CrossProcessSameRequest approve/reject; LateFailureRollsBack also checks outbox count.
- Stale writes: StaleEmployeeEdit and StalePolicyEdit; retained browser employee draft/explicit reload.
- Duplicate persistence: CrossProcessDuplicates for normalized email or employee number; identity/profile counts match.
- CSV idempotency: RepeatedCsvConfirmation uses an explicit two-process barrier plus repeated key; one account/profile and one durable Created row.
- Migration truth: LegacyRecordsSurviveUpgrade generates schema at 20261002094357, upgrades with real migrations, compares original Identity/profile/request/audit facts, null employment/snapshot fields and absent fabricated submission events.
- Real browser HTTP workflow: Employee form -> assigned Manager -> owner history/timeline, actual Pending HR monitoring without decisions, stale employee HTTP 409 with retained draft and explicit reload.
- Controlled transport/session gates: old dashboard completion across different/same account; stale refresh success/failure with actual fresh-login Authorization comparison; non-replayable 401; delayed mutation failure cannot clear replacement HR session.

Controlled route barriers/fault responses are labelled separately from real HTTP workflow tests.
They are not claims of real access-token expiry, packet-loss, browser refresh persistence or all JWT boundaries.
SQL row/audit assertions supplement HTTP results. The browser workflow also reads the persisted authorised
timeline and checks status, one Submit/Approve event and decision actor.

## Explicit remaining coverage

Medical/scanner/quarantine/recovery, comprehensive accessibility/mobile/theme matrix, load/deployment,
Linux execution, all JWT lifetime/not-before/skew variants, full activation/password/recovery failure
matrices, other same-request race combinations, notification delivery/retention, CSV cancellation/delivery
failure and qualified statutory calculations remain future/manual gates. No invented legal calculation
fixtures. #75/#22 and broader #76 completion stay open. See the verification report for actual run counts,
durations, initial harness failures and temporary negative-control experiments.
