# Owner-scoped leave submission recovery — local verification

27 October 2026, Windows x64 / Africa-Johannesburg; .NET SDK 9.0.305 targeting .NET 8,
Node 24.12.0, Playwright 1.64.1 / Edge. Branch `feat/leave-submission-recovery` from
`49f3373477e9052f2d4f68d1317f3e6ce8dbe81d`. PR #138 was confirmed MERGED before clean branch creation.
Existing-issue search found related scope, not a dedicated recovery issue; no tracker changes.

[API, canonicalization, lifecycle and retention contract](../adr/0034-owner-leave-submission-recovery.md).

## Executions so far (not an aggregate final pass)

- Initial `dotnet build HRFlow.sln --no-restore -v quiet`: passed, 0 warnings/errors, 14.24s.
- Initial `dotnet ef migrations add AddLeaveSubmissionOutcomes --project src/HRFlow.Infrastructure
  --startup-project src/HRFlow.Api --output-dir Migrations` failed during build. Its implicit restore
  was denied network access to NuGet (NU1301). The following no-restore build reported four stored
  NU1301 failures. No product error was diagnosed from these restore failures.
- Authorised `dotnet restore HRFlow.sln --locked-mode` succeeded, with unchanged dependency files.
  The migration command then succeeded using an explicit in-memory design-time connection (no real
  store). API service-provider construction rejected an on-disk-store requirement and EF continued
  with the existing design-time factory, as its output states. Generated migration/designer/snapshot
  are additive; no database reset or deployment occurred.
- Subsequent solution build passed, 0 warnings/errors, 9.19s.
- Initial focused `dotnet test tests/HRFlow.RegressionTests --no-build --filter
  FullyQualifiedName~LeaveSubmissionRecoveryTests -v minimal`: **12/12 passed**, 0 skipped, 1m55s.
  Elapsed-date fixture coverage and the legacy-migration assertion were added afterward.
- Rebuild passed, 0 warnings/errors, 5.05s. Expanded focused command with filter
  `FullyQualifiedName~LeaveSubmissionRecoveryTests|FullyQualifiedName~MigrationTests`:
  **14/14 passed**, 0 skipped, 1m28s. This includes 13 recovery cases and one earlier-schema upgrade.
- Safe submission-outcome logging/constant cleanup followed; rebuild passed, 0 warnings/errors, 5.68s.
- Client build/lint passed twice (2.47s and 2.15s production Vite build timings); existing
  `useAuth.tsx:223` fast-refresh lint warning remains. Browser typecheck passed separately.
  A later success-focus improvement requires its own final check below.
- First `HRFLOW_BROWSER_CHANNEL=msedge; npm.cmd test -- submission-recovery.spec.ts`:
  **2 passed / 1 failed / 1 timed out**, 100.037066s. Lost committed response and different-account
  recovery passed. Retry failed at the disabled submit-control lookup: its label changes from
  “Submit request” to “Submitting…” while pending. The assertion now selects the submit control
  and still requires disabled state. Same-account replacement timed out without a source location;
  its cause is not established. Executable edits during that run also prevent clean attribution.
  Fixed allowlisted stage diagnostics were added; no raw Playwright errors, HTML or payloads printed.
  No retry, timeout increase or weaker authorization/submission assertion was introduced.
- Expanded corrected focused browser invocation, with executable files unchanged during execution:
  **5/5 passed**, zero retries/skips, **39.545293s**. Both replacement-session cases reached fixed
  `check-entered`, `signed-out`, `signed-in`, `released`, `settled` barriers. The earlier same-account
  timeout was not reproduced; a passing rerun does not establish its original cause.
- Final frontend callback review adds session checks between awaited invalidations; this does not
  change backend source. Client build/lint passed again (**1.63s** build). Existing warning remains.
- `dotnet list HRFlow.sln package --vulnerable --include-transitive`: all six projects reported no
  vulnerable packages given current sources. Full and `--omit=dev` audits, separately in client
  and browser projects, all reported **zero vulnerabilities**. No dependencies/lockfiles changed.
- First complete backend invocation (`dotnet test tests/HRFlow.RegressionTests --no-build
  --logger 'console;verbosity=normal'`, output restricted to fixed class/method names and numeric
  summaries): **196/196 passed**, zero failures/skips, **14.4583 minutes**. This is not the final
  corrected-source execution: review while it ran restored FluentValidation's existing 400 category
  for new-attempt dates and retained unkeyed callers' original early date-validation behaviour, with
  fresh writer-protected date checks still mandatory. Two retained assertions now check that category.
  No permission, overlap, balance, document or recovery check was weakened.
- After that complete run stopped, the corrected-source rebuild passed with 0 warnings/errors
  (**12.18s**). A separate focused filter for
  `LeaveSubmissionRecoveryTests.ElapsedDatesRecoverButCannotCreateNewAttempt` and
  `LeaveSubmissionRecoveryTests.LegacyCallsRemainCompatibleAndModelMatches` passed **2/2**,
  zero skipped/failed, **14s**, including both retained HTTP-400 category assertions.
- Final corrected-source complete backend invocation, same command and privacy-safe output filtering:
  **196/196 passed**, zero failures/skips, **13.6791 minutes**. This is a separate execution from
  the earlier 196-case run, not a combined 392-case pass. All 13 new recovery cases and the existing
  authorization, transaction, migration/model, activation/recovery, document/image, employee/onboarding,
  balance/overlap, audit, directory and reporting gates passed. Browser fixtures were stopped throughout.
- After backend completion, final complete default browser command from `tests/browser`:
  `HRFLOW_BROWSER_CHANNEL=msedge; npm.cmd test` (production-built flag removed): **83/83 passed**,
  zero retries/skips, **240.426826s**. This is separate from the five-case focused pass. All five
  new recovery checks and the existing 78 browser cases passed, including actual HTTP workflows,
  non-replayable submission, real bearer refresh, current scopes, stale forms, navigation and session
  isolation. The following `npm.cmd run typecheck` passed. Existing Node experimental SQLite warning
  remains; it was not suppressed. No executable files changed during this invocation.

## Final commands and persisted-state results

The final sequence was a stopped-fixture build, the two affected date/legacy cases, one full backend
invocation, then one full browser invocation/typecheck. Earlier runs above remain separate evidence.
The full backend source did not change after its final build; frontend source did not change after
its final 1.63s build/lint. Documentation-only updates followed the complete gates.

```powershell
dotnet build HRFlow.sln --no-restore -v quiet
dotnet test tests/HRFlow.RegressionTests --no-build `
  --filter 'FullyQualifiedName~LeaveSubmissionRecoveryTests.ElapsedDatesRecoverButCannotCreateNewAttempt|FullyQualifiedName~LeaveSubmissionRecoveryTests.LegacyCallsRemainCompatibleAndModelMatches' -v minimal
dotnet test tests/HRFlow.RegressionTests --no-build --logger 'console;verbosity=normal'
# After backend fixture processes stop; from tests/browser:
$env:HRFLOW_BROWSER_CHANNEL='msedge'
Remove-Item Env:HRFLOW_BROWSER_PRODUCTION -ErrorAction SilentlyContinue
npm.cmd test
npm.cmd run typecheck
```

The two-process identical-attempt case returned the same ID from both API instances and persisted
exactly **one request / one outcome / one Submit audit / one outbox event / one binding**. Canonical
document reordering and description trimming preserved that outcome; seven changed business-field
variants conflicted. Another key created a separate request/outcome, preserving Pending semantics.
After real approve/reject/cancel and later configuration changes, recovery retained the original
request/snapshot, one binding and exactly two transition audits/events (Submit plus the real change).
Injected late outcome insertion left **zero requests/audits/outcomes/outbox events**, with the draft
still unbound; explicit retry then produced one successful set. Read-only missing/wrong-owner checks
disclosed no original ID or new request, and current revoked/inactive/pending/HR-only access was denied.
Encrypted restore preserved the original owner/key/request and blob association, retained its audit,
cleared passwords under existing invalidation and denied the old bearer on actual HTTP outcome access.

Final UTF-8, formatting/fences, **52 relative links/anchors**, whitespace, sensitive-pattern,
instruction-byte and complete-new-file diff checks passed across **23 intended files**. No dependency/CI
changes, private fixture data or generated build output are included. Final safe process inventory
reported **zero** matching task-window API/operator/fixture processes. The complete review patch includes
new files and remains ignored; the index was empty at implementation completion. No publication or
tracker action was performed during that phase.

EF-generated UTF-8 BOMs were normalised without source-semantic changes. Static review then passed
across 23 intended files / 52 relative links, matching instruction bytes and no sensitive-pattern
findings. Helpers and the review patch remain ignored.

## Publication review — 9 November 2026

Complete source, new-file, migration, test and documentation inspection found no additional product
correction. Publication is now authorised for independent PR review. The **196-case backend** and
**83-case browser** results above are separate 27 October local executions, not reruns today; their
earlier failures and unreproduced same-account timeout remain recorded. Only documentation wording
and formatting changed during this finalisation. Staged whitespace, UTF-8, sensitive-pattern,
relative-link/anchor and instruction-byte checks passed across 23 intended files and 52 relative
links/anchors before committing. No private fixtures, actual credentials/descriptions or generated
output were included. Hosted results
will be inspected on the published commit and reported separately; local results are not hosted evidence.

## Retained evidence boundaries

[`LeaveSubmissionRecoveryTests`](../../tests/HRFlow.RegressionTests/LeaveSubmissionRecoveryTests.cs)
uses fresh migrated file-backed SQLite and two actual API processes. A held writer plus endpoint-entry
barrier coordinates identical concurrent attempts; assertions inspect request/outcome, Submit audit,
bound documents and notification outbox rows. Late outcome-insert trigger failure checks full rollback,
then explicit same-key success. Valid changed fields conflict; reordered document IDs and trimmed
description recover; another key remains independent. Current role/lifecycle/credential denial,
HR-only denial, wrong-owner non-disclosure and read-only missing checks are retained.

Transition recovery calls actual approve/reject/cancel APIs, changes policy/type afterward and verifies
the original immutable snapshot, association and audit counts. The elapsed-date case seeds a labelled
synthetic historical keyed fixture: it proves handler ordering without claiming naturally elapsed time
or changing the production clock. Legacy callers still use unkeyed submission; no outcomes are invented
for their records. The existing legacy-migration comparison additionally asserts an empty new table.

Encrypted recovery uses actual stopped-writer tooling, a new restore destination and mandatory security
invalidation. It inspects receipt/request/document associations, audit preservation and cleared password
hashes; a normal restored host rejects the old bearer on the outcome endpoint. No parallel application
lineages or restored submission execution. This is local recovery evidence, not target-host readiness.

[`submission-recovery.spec.ts`](../../tests/browser/submission-recovery.spec.ts) uses actual HTTP with
controlled response loss **after** server commit, then a real read-only outcome check. Another case
aborts before dispatch, checks the real missing outcome and explicitly retries the original payload/key,
while a held real response checks duplicate-click controls. Session cases hold real outcome responses
across same-/different-account replacement. A separately labelled metadata-only document fixture has no
blob, never binds/submits it to the server and is not successful real scanning evidence.

All new fixture credentials are generated privately. Existing strict permissions and owned-process
cleanup are reused. Screenshots/traces/video remain disabled. Safe diagnostics expose fixed stages
and test-source coordinates only. No business descriptions or request bodies are logged by the feature;
the outcome table contains a digest, not a second description copy.

## Remaining limitations

At implementation completion, hosted CI had not run; see the dated publication review above.
Real ClamAV, production-built browser,
screen readers, physical devices, realistic load/target-host deployment and qualified legal checks
are not implied by default local gates. Controlled clean document fixtures verify storage/binding,
not malware detection. Existing calendar-day charging, UTC submission-date lower bound and Medical
privacy are unchanged. Unkeyed legacy callers are intentionally not idempotent. Local attempt bodies
are not persisted across route exit/refresh/logout; history review is required before starting anew.
A restored snapshot cannot recover or deduplicate missing post-cutoff lineage history. No executed
downgrade is claimed; losing the new table would lose key guarantees and must be operationally gated.

## Main integration and fresh hosted verification — 10 November 2026

The original [PR #139 run 42660224388](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/42660224388)
passed **196/196 backend** tests in **14.0291 minutes**, but its browser job failed the client audit
for inherited follow-redirects 1.16.1 / GHSA-mrwp-f3q8-3g9p. Browser setup and tests **did not execute**;
the aggregate remained failed. This result is preserved, not superseded retroactively.

Merged [PR #140](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/140), main commit
`1bccecfd8b56f6921a979ba40f3ecf5cf43d9c09`, supplies the narrowly scoped compatible 1.16.2 lockfile
correction. Main was merged into this existing recovery branch without conflicts. Submission source,
assertions, audit gates and permissions were not changed. Both instruction files remain byte-identical.
See [the dependency report](follow-redirects-security.md) for exposure, local audits and historical
resolution attempts. PR #140's own results are **not** verification of submission recovery.

The updated PR #139 commit will receive its own complete hosted backend/browser execution; its actual
run URL, counts and outcome will be recorded in the PR after completion. No local product tests were
rerun for this main integration. Historical local executions and unresolved limitations above remain
separate. No merge of PR #139, calculation change or external service enablement is authorised.
