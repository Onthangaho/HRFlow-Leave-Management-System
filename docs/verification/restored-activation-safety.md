# Restored-store activation safety: retained local drill

21 October 2026. Branch test/restored-activation-safety from fetched origin/main
0daa9ec5659d953d282c1350e99687eff2a30ec3. PR #132 merged at 19:30:26 UTC, confirmed before
branching; initial tree clean. Existing #79/#82/#76 remain open and unedited. The implementation-phase
record below predates publication. No provider connection, external sending, deployment or statutory change.

## Scope and test architecture

[ActivationRecoveryTests](../../tests/HRFlow.RegressionTests/ActivationRecoveryTests.cs) extends the
existing backup/recovery and activation regressions rather than implementing another recovery path.
The actual HRFlow.Operations executable runs keygen, backup, restore --invalidate-security and
recover-password --confirm-current-permissions. Packages use the existing authenticated AES-GCM
implementation and exact-schema validation. Destination is newly generated and isolated; custody
keys/password files are separate from the backed-up private root and package inventory.

Twelve synthetic accounts include the seven existing fixture actors and five actual HTTP-created
accounts: Accepted, DeliveryUnknown (disconnect), PendingDelivery (injected lost acknowledgement),
an explicitly redeemed Activated account, and one durable Created CSV outcome. The acknowledgement
trigger is removed before backup. One existing synthetic employee is inactive. A real submission and
assigned-Manager approval create two real matching audits; no audit is fabricated for recovery.

The normal HRFlow.Api executable, Program and DI run in independent owned child processes. No
WebApplicationFactory replacement pipeline, diagnostic HTTP endpoint, authentication bypass or
production seam is added. A test-assembly-only DOTNET_STARTUP_HOOKS class observes HostBuilding to
replace ONLY IActivationDelivery in Development with the existing private loopback SMTP/MIME fixture.
The hook validates its generated restricted temporary root and refuses Production. Production tests
run without the hook and preserve the normal unavailable-delivery gate. This privileged test runtime
hook is not a production transport configuration and must not be deployed/configured on a real host.

The existing sink now permits a separate API process to query an in-memory hashed acceptance marker
on its same loopback endpoint, so EOF is not mistaken for positive acceptance. MAIL FROM counts
observed attempts; CHECK is not another delivery. No forwarding, remote host, provider credentials or
external recipient is supported. Tokens, messages and canaries stay in memory; no recoverable token
queue or private invitation artifact is created.

## Ordering and deterministic barriers

1. Bootstrap the original synthetic lineage with the existing migrated SQLite/Identity fixture;
   capture real, still-unexpired access/refresh pairs and the signing/refresh configuration privately.
2. Stop/await both bootstrap API writers. Run the original normal API with the test SMTP registration,
   create states/CSV outcomes, then stop/await it. No recovered application exists concurrently.
3. Add one Pending notification sentinel while stopped, referencing an existing synthetic request.
   It is outbox test data using the real Submit category, not a fabricated leave-state audit.
4. Run actual encrypted backup/restore commands. Existing process inventory, exclusive maintenance
   lease and SQLite reservation remain active. Recovery invalidation is the product implementation.
5. Reconcile synthetic roles/lifecycle against the preserved fingerprint before starting recovered
   workers; there are no post-cutoff writes in this controlled store. Start the recovered normal
   Development API while the sink is already listening. EF diagnostic
   events signal a real worker read and the committed transaction for the EXACT sentinel ID. No
   arbitrary delay, manually invoked substitute worker or diagnostic endpoint establishes ordering.
6. Inspect persisted rows and HTTP denials, stop again, then run the existing offline recover-password
   command for the reviewed active/activated HR identity. First-admin is never used for recovery.
7. Exercise real local Production-mode TLS login with the reviewed password, but no SMTP hook or
   provider. Manual creation, resend and CSV confirmation remain denied; PrivatePickup configuration
   refuses Production startup. Stop before returning to the Development-only SMTP rehearsal.
8. Read authorized status/directory/Created CSV outcomes, attempt repeated confirmation, then explicitly
   resend with the current employee version. Redeem only the fresh invitation once and restart again.

Every start asserts previous owned hosts are stopped. Commands/startup/HTTP/transport waits have
bounded deadlines; no automatic retries or business callback replay. Startup waits use listening and
real health/ready, not sleeps. The shared bootstrap fixture retains readiness-only polling; it does
not order races. Test cleanup attempts every owned process, stops the sink and removes only its
validated generated root after proven process shutdown. If any owned writer cannot stop, it fails and
retains private files. Constructor/start failures and assertion failures do not silently become passes.
Unix 0700/0600 and Windows owner-only helpers/production path validators are retained; required roots,
restored database, custody/package files and descendants are inspected. These local runs are Windows
only; any later hosted Linux result must be inspected separately on the review PR.

## Persisted/HTTP/transport evidence

- Logical fingerprints reconcile employee facts/versions/reporting/lifecycle, account identity and
  activation facts, roles/role definitions, requests, audits, preferences, durable Created outcomes and
  immutable notification event identities/recipients. Security fields deliberately differ after restore.
  The original store's credential versions/password remain unchanged; it is never restarted alongside
  recovered data. Fingerprints and private entities are not serialized into reports.
- All twelve restored accounts have password hash and invitation hash/expiry/recipient/dispatch fields
  cleared; credential versions rotate and all persisted refresh rows are revoked. Pending accounts
  remain Pending with Expired feedback; inactive stays inactive. No role or lifecycle repair occurs.
- Still-unexpired old access JWTs (same signing material/issuer/audience), old refresh tokens, old
  passwords and old invitations are denied through actual HTTP. Identity validators and API gates
  are unchanged. Existing activated and inactive actors are included in the old-credential checks.
- Sink attempts/submissions are five before backup and remain five through recovered startup,
  real worker activity, status reads, repeated Created CSV confirmation and permission denials.
  Restored batches are cancelled/expired; repeated confirmation conflicts without recreating users.
- Existing HR recovery uses restricted password-file input, then actual login works. HR-only role
  membership remains exact; personal dashboard/decisions remain forbidden. The temporary password
  file is deleted; this does not claim secure media erasure.
- One explicit current-version resend creates attempt/submission six and Accepted feedback. Its fresh
  token differs from every old token; old invitations still fail, the fresh one redeems once, repeat
  redemption fails and normal login works. The target remains Employee-only. User count stays twelve,
  the request retains exactly two audits, and the sentinel has exactly one notification after restart.
- Captured child/operator output is checked privately for credential/token/path canaries, JWT patterns
  and MIME-content markers. Assertions never print passwords, invitation values or password hashes.
  No message, database, key, certificate, log or private path belongs in the review artifacts.

## Executions (separate runs)

Windows x64, .NET SDK 9.0.305/runtime 8.0.23, existing xUnit foundation and file-backed SQLite with real
migrations. No EF InMemory, dependency addition, framework replacement, browser or scanner substitute.

- Initial build: failed CS7036 because the new fixture called Deactivate without its actor/reason.
  Corrected to the existing domain contract; no production change. Corrected build passed with zero
  warnings/errors (13.96s).
- Initial recovery-only execution: 0 passed / 1 failed, 73.82s. The sentinel used Submission instead of
  the actual Submit category. The existing preference worker correctly failed closed/suppressed it,
  so no inbox item existed. Corrected the synthetic category and made the commit barrier specific to
  the sentinel. No worker policy or assertion was relaxed.
- Next focused execution: 16 passed / 1 failed (17 cases), 360.28s. Recovery completed all stages.
  Existing LateAcknowledgementUsesCurrentGenerationAndEligibility(expired) failed with TaskCanceledException
  during ApiFixture/Harness startup, before its SMTP scenario. Listener readiness timed out; available
  evidence does NOT establish why that process was slow/unready. No timeout was increased and this
  result is not discarded as flakiness.
- Separate focused rerun: 6 passed / 0 failed, 133.41s, covering the recovery drill and all five existing
  late-acknowledgement variations, including expired. This passing rerun does not explain the earlier
  startup failure. Secret-safe assertions and unexpired-token checks were present in this run.
- Build after cleanup/privacy and persisted/read assertions: zero warnings/errors, 6.19s.
- Complete retained backend execution: 111 passed / 1 failed, 112 total, 23.7954 minutes (1427.72s).
  The new recovery drill timed out at the deliberate Production/PrivatePickup refusal while waiting
  for process exit. Earlier phases completed encrypted restore, persisted invalidation, HTTP denials,
  worker completion and recovered HR Production login. The run did not retain a guard-observed
  diagnostic, so it does not establish whether that message appeared or why exit exceeded 20 seconds.
- Follow-up adds fixed boolean refusal diagnostics and assertions for the exact Production guard and
  no listening endpoint, retaining nonzero-exit and the 20-second bound. Build passed with zero
  warnings/errors (16.59s). The separate final recovery-only execution passed 1/1 in 1.2657 minutes
  (75.94s), including all five safe phase markers, explicit resend, single-use redemption and restart.
  The complete run remains failed and was not rerun after these diagnostic/assertion additions.
  No automatic retry hid that failure.

## Reproducible local commands

Run all fixture-sensitive commands sequentially; stop other API/browser fixtures and disable service
auto-restarts first. Never point these tests at an employee store. No operator variables, passwords or
keys need to be supplied by the developer: the fixture generates and privately cleans them.

~~~powershell
dotnet build HRFlow.sln --no-restore
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~ActivationRecoveryTests --logger 'console;verbosity=normal'
dotnet test tests/HRFlow.RegressionTests --no-build --filter 'FullyQualifiedName~ActivationRecoveryTests|FullyQualifiedName~ActivationDeliveryTests' --logger 'console;verbosity=normal'
dotnet test tests/HRFlow.RegressionTests --no-build --filter 'FullyQualifiedName~ActivationRecoveryTests|FullyQualifiedName~LateAcknowledgementUsesCurrentGenerationAndEligibility' --logger 'console;verbosity=normal'
dotnet test tests/HRFlow.RegressionTests --no-build --logger 'console;verbosity=normal'
~~~

The last command is the complete suite, not a sum of focused results. Formatting touched only the new
fixture files. Build outputs and raw local logs are excluded from source review.

## Limits

This is local Windows, synthetic loopback recovery evidence, not target-host readiness, guaranteed
RPO/RTO, provider TLS/deliverability, Linux permissions/process inventory, physical storage durability,
load, all-role recovered browser interaction or a new scanner/document recovery drill. Test runtime is
not operator recovery time. No browser/product behavior changed, so browser gates/frontend checks were
not rerun. No schema/dependency change: no migration or package audit claim is added. Existing dated
hosted/Linux results belong to earlier PRs. At the local recording point this branch was unpublished;
subsequent hosted results are tracked separately on its review PR.
Both the earlier existing-fixture startup timeout and the complete-run refusal exit timeout remain
unresolved verification limitations. Passing isolated reruns do not identify either cause. D1-D6 and legal/deployment/privacy gates remain unapproved; #79/#82/#76 stay open.

## Review checks

The intended diff contains seven test-source files and ten Markdown files. Production src/, tools/,
schema, dependency manifests/locks and CI have no changes. Relative-link existence, UTF-8 decoding,
replacement-character/trailing-whitespace and token/private-key patterns were checked; 150 relative
links resolve and no changed Markdown links contain local anchor fragments. Both instruction files are
byte-identical. git diff --check passes. No runtime artifacts are included. The index was empty during
implementation; independent-review finalisation stages only the intended tests and documentation.
No screenshots were generated because this is a backend/operator drill. These checks are scoped
checks, not a comprehensive secret-scanner or security-audit certification.

## Follow-up: investigate the failed complete gate (21 October 2026)

The original 111/112 complete result and all earlier executions above remain evidence, not replaced
by subsequent passes. The exact failing test was
RestoredStoreInvalidatesOldAuthorityAndOnlyExplicitResendDispatches. Its fourth owned normal API child
was the deliberately forbidden Production/PrivatePickup launch, after successful recovered-HR TLS
login and delivery-gate HTTP checks. The failure was at the 20-second WaitForExitAsync/WaitAsync
deadline in Host.StartAsync, before nonzero-exit/guard assertions; it was not during backup, restore,
password recovery, SMTP dispatch or shutdown cleanup. No PID, command line or private path is needed
to identify that stage.

The historical log lacks native-exit/EOF observations, so the timeout cannot establish whether the
process was still running or redirected output was still draining. The retrieved official
[.NET 8.0.23 implementation](https://github.com/dotnet/runtime/blob/v8.0.23/src/libraries/System.Diagnostics.Process/src/System/Diagnostics/Process.cs#L1365-L1467)
awaits stream EOF as well as exit. Reviewed 21 October 2026; this explains diagnostic ambiguity, not
the historical failure's cause. Application refusal, runtime exit delay, blocked output, interference
and resource contention remain alternatives; no passing rerun proves any of them.

### Narrow changes and cleanup inspection

- [FixtureProcessDiagnostics](../../tests/HRFlow.RegressionTests/FixtureProcessDiagnostics.cs) observes
  native exit, separate stdout/stderr EOF and line counts, a fixed
  unhandled-exception prefix flag, elapsed/CPU milliseconds, working-set MiB and thread count. Fixed
  stage enums identify listening, readiness, refusal or cleanup. Unavailable metrics use -1. Raw
  output, exception messages, arguments, PIDs, addresses, paths and secrets are never emitted.
- Refusal retains its exact guard, no-listener and nonzero-exit assertions and its existing 20-second
  deadline. Startup (30 seconds), owned stop (10 seconds recovery / 5 seconds bootstrap) and SMTP
  cleanup (5 seconds) are unchanged. No retries, skips or timeout increases were introduced.
- Source inspection found both fixtures skipped waiting for output when HasExited was already true;
  recovery also called an unbounded synchronous WaitForExit. Cleanup now always awaits exit/draining
  under the existing deadline, checks EOF, then disposes handles. These are source-proven cleanup gaps,
  not a demonstrated cause of the earlier refusal deadline. Existing bootstrap readiness/stop failures
  now get the same privacy-safe stage diagnostics.
- Recovery cleanup awaits every owned host and the sink's accept/connection tasks before clearing
  SQLite pools and removing its guarded root. A live/unproven writer retains the store and fails.
  Added assertions report only owned host/stopped counts, active SMTP task count and removal boolean.
- [RecoveryLifecycleTests](../../tests/HRFlow.RegressionTests/RecoveryLifecycleTests.cs) isolates
  Production refusal and injects caller failure before/after normal
  Development startup. It checks completed cleanup, zero active connections, connection refusal on
  the closed loopback listener and a new SQLite writer reservation after host shutdown. These checks
  verify failure cleanup without weakening the real API or manually invoking substitute workers.
- Initial process inventory was denied by the sandbox and is not counted as a zero result. An
  authorised aggregate-only inventory then found zero API and zero operator processes (seven dotnet
  processes overall). None were terminated by name. The complete gate's own preflight again required
  zero API/operator processes. This is not proof about machine-wide contention at the historical fail.

### Follow-up executions, each separate and sequential

1. Initial diagnostic build failed CS1061: the new listener assertion incorrectly called AsTask on a
   Task-returning ConnectAsync overload. Removed AsTask; no production change.
2. Corrected build: zero warnings/errors, 4.37s. Isolated Production refusal: 1/1 passed, 16.7883s.
   Child lifetime 5400ms, CPU 187ms; expected guard asserted, native exit and both EOFs observed.
3. Final diagnostic build: zero warnings/errors, 3.64s. Second isolated refusal: 1/1 passed, 12.5477s.
   Child lifetime 4810ms, CPU 218ms; native exit and both EOFs observed. This is a bounded second
   execution, not an automatic test retry.
4. Combined recovery/lifecycle filter: 4/4 passed, 56.9560s. Both refusal launches exited with both
   EOFs (4901ms/156ms CPU and 4662ms/125ms CPU). Full-drill cleanup: six hosts stopped, zero active SMTP
   tasks, private store removed. Injected failure cleanup passed both before and after startup.
   Negative listener checks and SQLite writer acquisition passed. No CPU/wall figures establish
   resource contention or explain the historical timeout.
5. One complete backend invocation: 115/115 passed, 7.5554 minutes (453.32s), after a preflight found
   zero API/operator fixture processes. No executable files changed while it ran. Both refusal
   launches observed native exit and both EOFs (4852ms/171ms CPU and 4883ms/140ms CPU). The full drill
   again reported six stopped hosts, zero active SMTP tasks and removed store; injected cleanup cases
   passed. Post-run aggregate inventory found zero API, operator and testhost processes. The earlier
   111/112 invocation remains a separate failed execution, not rewritten as a pass.

~~~powershell
dotnet build HRFlow.sln --no-restore
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~ProductionPickupRefusalExitsAndDrainsOutput --logger 'console;verbosity=normal'
# Run the same isolated command once more, recorded separately; retries remain disabled.
dotnet test tests/HRFlow.RegressionTests --no-build --filter 'FullyQualifiedName~RecoveryLifecycleTests|FullyQualifiedName~ActivationRecoveryTests' --logger 'console;verbosity=normal'
dotnet test tests/HRFlow.RegressionTests --no-build --logger 'console;verbosity=normal'
~~~

The original refusal timeout has not been reproduced by these focused executions. The earlier
LateAcknowledgement(expired) bootstrap timeout also remains unexplained. Safe diagnostics are retained
for future failures; neither issue is claimed fixed by rerun success. Production source/delivery,
schema, dependencies, permissions and CI remain unchanged. No external transport is available.

No controlled blocked-pipe, OS-denied kill or resource-stress experiment was run. The retained fields
can distinguish native-exit/EOF stages if another failure occurs, but do not retrospectively provide
missing observations. The isolated refusal uses the original synthetic migrated store; its guard runs
before storage/maintenance initialization. The full drill separately exercises the restored store.

## Publication follow-up: remaining cleanup bounds

Independent review after publishing 39651c7 found that ToolAsync bounded native exit but subsequently
awaited ReadToEndAsync without a bound. Exit plus both streams now share the existing 45-second
cancellation deadline. SMTP accept-task and connection-task shutdown likewise share its existing
five seconds. Neither deadline was increased. Operator timeout diagnostics contain only exit/task
completion booleans. Private-store deletion now also requires proven SMTP cleanup, not only stopped
API hosts; otherwise the store is retained and cleanup fails. No automatic command replay was added.
These source-review corrections do not explain the original refusal timeout.

Separate additional Windows executions:

- Output/accept-bound build: zero warnings/errors, 13.79s; affected recovery/lifecycle/activation SMTP
  filter: 20/20 passed, 2.6053 minutes (156.32s).
- After adding the pre-deletion SMTP guard: build zero warnings/errors, 4.62s; the same affected filter
  passed 20/20 in 2.4360 minutes (146.16s). This is a separate run on the final test-source changes.
- The local 115/115 complete invocation above predates these publication follow-ups. It is not
  relabelled as a new full local execution. Final-head hosted gates are inspected separately.

Initial published-head [hosted run 39674879918](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/39674879918)
passed on Ubuntu at 39651c7: 115 backend tests in 6.6970 minutes and 51 Chromium tests (121430.451ms
browser duration), plus build/lint/audit/typecheck steps. Its log was inspected privately for totals;
no raw logs or sensitive artifacts are committed. This evidence belongs to the initial head before
the follow-up bounds, not a later head. Hosted success neither reconstructs the historical timeout
nor proves target-host storage/TLS/proxy/provider/scanner readiness. Positive real-ClamAV coverage
was not rerun. Subsequent PR-head results belong in the PR's separately inspected workflow record.
