# Activation coordination: local verification, 21 October 2026

## Scope and provenance

Branch feat/activation-delivery-coordination from fetched origin/main
454d5f7b764b6f052d0dbfdc80390fb9bdc320d6. PR #128 merge confirmed at
2026-10-21T15:20:26Z before branching; initial tree clean. No provider approved, external mail sent,
production transport enabled, issue edited or publication performed. D1-D5 remain Pending.
[Contract and migration/recovery consequences](../operations/activation-delivery-coordination.md).

## Executed runs (separate, not aggregated)

Windows local .NET/Chrome fixtures use new migrated file-backed SQLite stores and generated private
credentials. Network messages remain in memory; reporters retain no bodies, links or credentials.
No scanner is substituted; this slice does not newly verify real ClamAV.

- Initial scripted edit left an interface/signature mismatch; build failed, corrected before tests.
  First test compilation also found a missing EmployeeRoles namespace; corrected.
- First focused coordinator execution: 13 passed / 2 failed, 1m19s. One expected GET405 but actual
  existing fallback authorization returned401; the retained assertion now verifies401 and unchanged
  Pending/hash/password state. No API authorization was relaxed. The other showed SmtpClient can
  complete after EOF without a positive DATA acknowledgement; the private fixture now requires its
  sink acceptance marker. This is a test fixture correction, not proof of a production SMTP adapter.
- Second focused execution: 15 passed, 1m35s, after these corrections.
- Expanded focused execution: 16 passed, 1m33s, including two actual API-process resend competition.
- First complete execution: 103 passed / 1 failed / 0 skipped, 6m01s. The modified recovery fixture
  had changed its original account to Pending while retaining an activated-state assertion. Corrected
  by preserving that activated account and adding a separate realistic passwordless Pending account.
  No production logic or assertions were weakened to resolve that fixture inconsistency.
- Final build after fixture correction: zero warnings/errors, 2.76s.
- Final frontend build: TypeScript build and Vite 837 modules, 3.04s. Lint passed with the existing
  useAuth.tsx:223 Fast Refresh export warning. Browser TypeScript check passed.
- Clean client npm ci installed 130 packages; no manifests/lockfiles changed. Full and production-only
  client/browser audits returned zero findings. Solution transitive vulnerability scan found none in
  all six projects. These are dated package checks, not a claim of complete application security.

- Complete corrected backend rerun: 104 passed / 0 failed / 0 skipped, 7m16s. This is one combined execution,
  separate from the focused runs and earlier 103/1 result. Model consistency: no pending changes.
- Initial complete browser execution: 34 passed / 3 failed, 240.16s. Both new late-session cases tried
  clicking Sign out behind a busy native modal; the script now uses browser Back to leave that modal,
  then signs out before releasing the held response. The existing attention-link assertion also failed:
  new active synthetic employees had no schedule and contaminated its exact baseline count. The new
  file now retires only its own allowlisted accounts through authorized HR HTTP deactivation after each
  case. No assertion, permission or production dialog was weakened. Safe logs identify the attention-link
  step; raw Playwright errors/content are intentionally not retained.
- Corrected four-case browser run: 4 passed, 22.29s. Next complete browser execution: 37 passed,
  110.55s. Existing attention-count assertions passed unchanged. This run preceded additional CSV
  refresh/link assertions; the final combined execution including those assertions passed all 37 cases in 117.93s.
- Browser typecheck passed after the final CSV assertion extension. Final solution rebuild after test-only
  cleanup hardening passed with zero warnings/errors, 7.51s. Cleanup uses finally blocks so SMTP cleanup
  failure cannot prevent owned API/provider disposal; the final focused run passed 16/16 with no skips in 1m26s. The complete 104 run preceded this
  test-only cleanup change; it is not presented as a new 104 execution. Migration rollback was reviewed,
  not executed as a downgrade drill. Existing upgrade/legacy preservation and encrypted restore ran
  within the complete backend suite.

- Last test-only extension explicitly replays an old token after recovery authority removal: solution
  rebuild passed with zero warnings/errors (3.66s), then 16 focused cases passed / 0 skipped in 1m27s.
  This is a separate execution from the 104 combined run and both 37-case browser executions.

Commands used (offline backend and browser fixtures run sequentially):

~~~powershell
dotnet restore HRFlow.sln --locked-mode
dotnet build HRFlow.sln --no-restore
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~ActivationDeliveryTests
dotnet test tests/HRFlow.RegressionTests --no-build
npm.cmd --prefix src/HRFlow.Client ci --no-audit
npm.cmd --prefix src/HRFlow.Client run build
npm.cmd --prefix src/HRFlow.Client run lint
npm.cmd --prefix tests/browser run typecheck
npm.cmd --prefix tests/browser test
npm.cmd --prefix src/HRFlow.Client audit --json
npm.cmd --prefix src/HRFlow.Client audit --omit=dev --json
npm.cmd --prefix tests/browser audit --json
npm.cmd --prefix tests/browser audit --omit=dev --json
dotnet list HRFlow.sln package --include-transitive --vulnerable
~~~

## Retained assertions and evidence boundaries

[16 backend cases](../../tests/HRFlow.RegressionTests/ActivationDeliveryTests.cs) exercise real MIME,
actual anonymous HTTP redemption, single use/unchanged roles, definite rejection, timeout/disconnect,
restart without redispatch, late acknowledgement failure, concurrent rotated resend, email/activation/
deactivation/expiry/profile edits, HR-role/credential revocation, CSV durable Created outcomes,
header injection and boolean canary-log assertions. Explicit SMTP barriers allow an independent SQLite
writer to commit before releasing transport. Two actual API processes compete using the same version
behind a writer barrier; one succeeds, one conflicts. Other SMTP cases use separate DI scopes/connections,
not independent SMTP-host processes. Test SMTP is plain loopback and cannot forward externally.

[Recovery](../../tests/HRFlow.RegressionTests/ProfileImageRecoveryTests.cs) now checks activated and
Pending profiles separately: restored password/hash/recipient/dispatch authority is cleared, Pending
remains Pending with Expired feedback, lifecycle and image coherence stay intact. No delivery worker
or durable recoverable message exists to resend restored invitations. The acknowledgement/restart test
also removes exactly the recovery authority fields and proves that replaying the old in-memory token
returns Expired without another SMTP attempt; this complements actual encrypted-restore row checks. Migration legacy-field assertions
verify null new metadata rather than inferred generation history.

[Four browser cases](../../tests/browser/activation.spec.ts) combine real HR create/resend and private
pickup with controlled lost/delayed HTTP responses. They test retained mobile drafts, no duplicate
creation, explicit recovery and different-/same-account replacement-session isolation. Harmless activation
GET navigation strips the fragment without a password POST. The extended real CSV browser case refreshes
persisted Created/delivery status and follows the employee-number deep link to explicit resend. Existing broader gates retain CSV, edit,
authentication, role, leave, image and recovery workflows. Controlled transport is not provider evidence.

## Outstanding

At implementation handoff, hosted Linux and a separate production-built browser run had not executed.
PR hosted results are tracked separately from the local runs above. Restore
checks inspect cleared invitation authority; a restored-store SMTP network-start drill is not retained. No actual provider, authenticated SMTP/TLS, sender/domain,
mailbox delivery, bounce processing or external recipient tested. No durable email retry/outbox introduced;
interruption requires explicit rotated resend and may create duplicate/stale mail in a future adapter.
No exhaustive process-kill timing matrix, real-network chaos/load, comprehensive accessibility or new
scanner drill. Historical tests are separate from this dated rerun. Provider/privacy/operational approvals
and legal/calculation gates remain open; this is not production-delivery readiness.

## Implementation-handoff review checks

36 intended files (28 tracked modifications, eight new files), 123 relative link targets checked,
UTF-8/replacement-character and trailing-whitespace checks passed. git diff --check passed; instruction
copies are byte-identical. Sensitive-pattern review found no actual keys, tokens or private artifacts.
The complete local patch includes new files and passes reverse-apply validation. At that handoff, nothing was staged,
committed or published. Manifests, lockfiles and CI are unchanged.

## PR finalisation review

The complete source/new-file diff was reviewed again before staging. Production delivery remains
disabled and the loopback SMTP fixture remains test-assembly-only. The generation/recipient claim,
dispatch outside transactions, guarded acknowledgement and durable Created CSV outcome boundaries
remain intact. Model consistency was rerun: no pending model changes. Downgrade consequences were
reviewed; no downgrade was executed. Earlier local suites were not newly rerun during this review.
