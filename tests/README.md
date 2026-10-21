# Critical workflow regression gates (#76 first slice)

## Deferred client-feature gates

`loading.spec.ts` adds all seven nonempty role compositions plus real direct-route/history checks and labelled controlled
module failure/delay tests. Default CI runs these against the development server. Production-built
verification is opt-in and uses the same private fixture with a private Vite build/preview destination:

```powershell
$env:HRFLOW_BROWSER_PRODUCTION='1'
$env:HRFLOW_BROWSER_CHANNEL='chrome' # omit to use installed Playwright Chromium
npm.cmd --prefix tests/browser test
Remove-Item Env:HRFLOW_BROWSER_PRODUCTION -ErrorAction SilentlyContinue
```

For role byte/timing measurement, also set `HRFLOW_LOADING_MEASURE=1` and `HRFLOW_LOADING_OUTPUT` to
an absolute ignored `.local-verification/client-loading-<label>.json` path in an existing directory. This selects only `loading.measure.ts`; clear
both variables before regression gates. Output contains only asset basenames, byte counts and timings,
never credentials or API payloads. One fresh context per role measures cold Overview, a completed
secondary route, logout and warm re-entry in the same cache. No throttling or capacity claim.
See [design](../docs/adr/0028-deferred-client-features.md) and [all executions/limits](../docs/verification/client-route-loading.md).

## Directory browser readiness and safe diagnostics

Follow-up to PR #125 run 39442610037: the original assertion was withheld by the safe reporter and
has not been reproduced locally. A separate held-response regression demonstrates why draft values
alone cannot prove Refresh completion. `readDirectory` waits for the applied collection request and
rendered idle state; production code is unchanged. See [all runs and limitations](../docs/verification/directory-browser-regression.md).

The reporter emits only allowlisted `directory-diagnostic` records: fixed step, relative test-definition
location, viewport/row/width counts and booleans. It never prints raw errors, HTML, payloads or arbitrary
worker stdout. A pure contract test retains that privacy boundary. Keep screenshot/trace/video off and
clear `HRFLOW_DIRECTORY_SCREENSHOTS`, `HRFLOW_DIRECTORY_MEASURE`, `HRFLOW_DIRECTORY_FIXTURE_ROWS` and
`HRFLOW_IMAGE_SCANNER_TESTS` overrides for diagnosis. Bounded repetition is not a configured retry:

```powershell
npm.cmd --prefix tests/browser test -- --grep 'HTTP directory: applied pages' --repeat-each=3
npm.cmd --prefix tests/browser test -- --grep 'diagnostic contract|refresh waits for response'
npm.cmd --prefix tests/browser test
```

The default suite adds one pure diagnostic-contract test and one controlled-response barrier case.
Real HTTP workflows and controlled transport remain labelled separately. Existing timeouts/retries,
security assertions and private fixture cleanup are unchanged. Early foundation/hosted notes below
are historical evidence; consult dated verification reports for each run rather than inferring CI.

## Bounded directory regression slice

`EmployeeDirectoryTests` retains SQL command-count, stable paging/ties, search/attention/Manager lookup
and current-authority HTTP checks using migrated SQLite. `DirectoryMeasurementTests` recreates
32/1,007-profile stores and prints safe count/byte/timing measurements, not request payloads.
The browser fixture includes 75 extra synthetic profiles by default, so directory/form callers must
search/page rather than assuming every employee is mounted. The new `directory.spec.ts` distinguishes
real API/forms/onboarding from controlled image/session transport; the existing leave/session cases
remain enabled. `directory-measure.spec.ts` verifies no absent-image probes and at most 25 rows.

After a full Debug solution build:

```powershell
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~EmployeeDirectoryTests
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~DirectoryMeasurement --logger 'console;verbosity=normal'
$env:HRFLOW_BROWSER_CHANNEL='chrome'
npm.cmd --prefix tests/browser test
```

For optional safe count-only local measurements set `HRFLOW_DIRECTORY_FIXTURE_ROWS=25` or `1000`
and `HRFLOW_DIRECTORY_MEASURE=after`, then run only `directory-measure.spec.ts` with an existing
ignored `.local-verification` folder. Remove overrides before the default suite. Opt-in
`HRFLOW_DIRECTORY_SCREENSHOTS=1` captures the synthetic directory only; normal CI screenshots,
traces, video and artifact uploads remain disabled. No new framework/dependencies or scanner fixture.
See [contracts](../docs/adr/0027-bounded-employee-directory.md) and
[executed evidence/limitations](../docs/verification/bounded-employee-directory.md).

## Offline first-administrator coverage (#81)

`FirstAdministratorTests` uses disposable migrated private SQLite stores, generated credentials,
real operator CLI invocations, a two-process stdin barrier and a real local Production HTTPS API.
It covers exact HR-only access, nonempty-store refusal, explicit departments/canonical roles,
Identity/employment validation, rollback, repeated invocation and unsafe storage. The normal
Production invitation gate remains enforced. No HTTP bootstrap bypass or production test endpoint
is added. Run `dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~FirstAdministrator`
after a full Debug solution build; run operator/API suites sequentially because stopped-writer
validation intentionally refuses a concurrent API. See [runbook](../docs/operations/offline-first-administrator.md)
and [actual evidence](../docs/verification/offline-first-administrator.md). Hosted results for this
new slice must be observed after publication; older hosted passes do not cover it.

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

## Profile-image extension (#95)

`ProfileImageTests` retains bounded JPEG/PNG/EXIF processing, unsafe input/decode limits,
real scanner outage, labelled controlled adverse scanner replies, independent storage integrity,
two-process paused publication/removal and actual API/worker restart compensation.
`ProfileImageRecoveryTests` verifies encrypted image/lifecycle/marker recovery, security invalidation
and missing-current-blob refusal. Existing live-access cases now check the image endpoint too.

With no `HRFLOW_TEST_CLAMAV_PORT`, the retained HTTP lifecycle test asserts fail-closed behavior.
Explicitly supplying a real scanner enables its owner/HR/Manager, stale/two-process replacement,
injected late database rollback and versioned removal path. This is not a skipped test or a
clean-verdict substitute. Scanner-free CI cannot establish the positive path.

The separate `profile-images.scanner.ts` browser suite requires real ClamAV and is selected with
`HRFLOW_IMAGE_SCANNER_TESTS=1`; see [exact commands](../docs/operations/private-profile-images.md).
Its main test uses real HTTP/ClamAV/browser interactions. Two labelled response-barrier tests use
actual upstream writes and controlled response delays across replacement sessions. Credentials and
responses stay in private fixtures; routine traces/video/failure screenshots remain disabled.

See [executed evidence and outstanding image checks](../docs/verification/private-profile-images.md).

## Hosted verification status (unchanged)

PR #117's first workflow attempt was blocked before any job steps ran: GitHub reports the account
is locked due to a billing issue. Resolve that account restriction and rerun the workflow; neither
Linux nor bundled Chromium is verified by this blocked run. See the committed verification report.

With real ClamAV configured, `HRFLOW_IMAGE_SCANNER_TESTS=all` runs both existing and image
browser gates in one invocation; `1` remains the image-only selection.

## Linux fixture storage gate

Fixture roots/private directories must be explicitly owner-only (0700), and databases precreated with 0600 before SQLite opens them; parent privacy and runner umask do not replace per-path validation. FixturePermissionTests and image recovery assertions retain this boundary using production path checks. See [actual hosted failure and correction evidence](../docs/verification/linux-fixture-permissions.md).

## Activation coordination (#79 local slice)

ActivationDeliveryTests adds 16 cases with real SQLite/Identity coordination, in-memory private MIME,
loopback SMTP, explicit network barriers and separate-process versioned resend. The SMTP fixture is
test-assembly-only; no provider or external sending is configured. activation.spec.ts adds four browser
cases: actual HR create/resend with controlled lost/delayed responses, mobile retained draft, replacement
sessions and harmless activation GET. No message artifacts, screenshots, traces or video are retained.
See [contract](../docs/operations/activation-delivery-coordination.md) and
[exact executions](../docs/verification/activation-delivery-coordination.md).
