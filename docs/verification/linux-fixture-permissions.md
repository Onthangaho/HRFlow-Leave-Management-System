# Linux fixture-permission correction — PR #120 follow-up

## Original hosted result

[Run 38781690040, job 116583234240](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/38781690040/job/116583234240)
actually ran on Linux: **54 backend cases, 50 passed / 4 failed**, 3.1939 minutes.
The browser step did not run after backend failure. This is not the historical billing blocker:
earlier billing-failed jobs never started; this run completed setup/build/lint and reached assertions.

Separate fixture-only branch `fix/linux-fixture-permissions`, base current main
`1c660c10a9dda3f8870b78292fd3d8ffe0433008`; the documentation PR remains open and untouched.
No production source, path/ACL checks, exception mappings, schema, dependencies or assertions relaxed.

## Confirmed failure paths and limits

1. **RestartCompensatesInterruptedPublicationWithoutMakingItCurrent:** original hosted stack
   reaches `PrivateOperationsPaths.Check` through `PrivateProfileImageStorage.PublishAsync`.
   `ApiFixture.InitializeAsync` restricted its root, but created `private/` with default permissions.
   A 0700 ancestor does not make the child itself 0700; validation correctly rejects group/other bits.
2. **EncryptedRecoveryPreservesImagesAndRejectsMissingRequiredContent:** original hosted stack
   fails at `BackupRestoreService.Backup`, line 39, the database-path check. This fixture already
   restricted its root/private directory, but SQLite created the database using default file mode.
   The file itself needs 0600. This is distinct from the first fixture's directory omission.
3. **ScannerOutageLeavesNoCurrentReplacement:** independently reproduced in a disposable Windows
   fixture by deliberately making only its private directory permissive (root stayed private).
   Original 409 assertion failed with 500. Safe allowlisted exception-type/frame diagnostics showed
   `InvalidDataException`, `RequireRestricted`, image `Remove`, and `ProfileImageService.ReplaceAsync`.
   Scanner-unavailable compensation reaches storage.Remove; unsafe fixture storage raises a validation
   exception, replacing the intended conflict. No blanket catch was added to production.
4. **ImageHttpHonoursExplicitScannerConfiguration:** independently ran the same targeted unsafe-directory
   experiment for this test; its unchanged 409 assertion also failed with 500 and the same safe
   diagnostic frames. This was the scanner-free branch, not a clean-scanner assertion. Original
   hosted API logs were discarded by design; the hosted 500 payload alone did not identify its type.
   The controlled local reproduction confirms this mechanism, not a newly executed Linux stack trace.

Both temporary negative controls were removed; no raw HTTP logs, fixture credentials or paths
are retained here. A subsequent incremental build reused a negative-control assembly because copied
safe source timestamps were older; the repeated 2-fail result was not accepted as a final run.
A forced rebuild was used before the clean affected and full runs below.

## Narrow correction

Fixture directories now use explicit Unix owner rwx at creation plus existing Windows restricted
ACLs; database files are precreated CreateNew with owner rw before SQLite opens them. Recovery uses
the same helpers. A retained permission test checks root/private/database before and after actual
API/worker restart and SQLite sidecars while a write transaction is open. Recovery additionally
asserts root/private/database/key/package/restored-root/restored-private/restored-database restrictions.
Production validation itself is reused by assertions; on Unix exact 0700/0600 modes are asserted.

Existing DisposeAsync still attempts to stop every owned API tree before deleting its own generated
store/pickup paths. Recovery runs offline and clears connection pools before deleting its own root.
No skips, changed expected statuses, relaxed permission checks or automatic retries were introduced.

## Execution status

New local/hosted results are recorded after execution below. Earlier #119 54-case and 12-browser
results are historical and are not substituted for these runs. Positive real-scanner image/browser
paths require actual ClamAV and remain separate from scanner-free hosted gates.

## Initial investigation publication boundary (historical)

This Windows machine has no installed WSL distribution (`wsl --list --verbose` reports none), and
no Docker command is available. An uncommitted local patch cannot be tested by GitHub Actions;
rerunning the original run would execute its old commit again. Commit/push were prohibited during that initial investigation. **A new hosted Linux execution of the proposed patch is therefore outstanding.**
Do not call the Linux failure fixed until an authorised publication lets the unchanged hosted gates
run against this patch and its actual outcome is inspected. No billing/protection/workflow changes,
Linux-test skips, purchases or first-admin implementation were made.

## New execution results — Windows, 15 October 2026

- Clean forced rebuild: `dotnet build HRFlow.sln --no-restore -t:Rebuild`:
  **0 warnings / 0 errors**, 12.42 s.
- Affected run: `dotnet test tests/HRFlow.RegressionTests --no-build --filter
  'FullyQualifiedName~ProfileImage|FullyQualifiedName~FixturePermission' --logger 'console;verbosity=minimal'`:
  **20 passed / 0 failed / 0 skipped**, 31 s. Scanner-free fail-closed path; no positive ClamAV claim.
- Complete retained backend run: `dotnet test tests/HRFlow.RegressionTests --no-build
  --logger 'console;verbosity=minimal'`: **55 passed / 0 failed / 0 skipped**, 3 m 8 s.
  This is one complete run including the added permission case, not a sum of earlier runs.
- Sequentially afterward: `npm.cmd --prefix tests/browser run typecheck`: passed;
  `HRFLOW_BROWSER_CHANNEL=chrome`; `npm.cmd --prefix tests/browser test`:
  **9 passed, no retries**, 40.455 s. Real HTTP flows and controlled transport cases retain their
  existing labels. The three positive image-scanner browser checks were not rerun; the hosted
  scanner-free nine-case gate was the target. No traces/screenshots/private state published.
- Isolated `npm ci` for client/browser manifests succeeded and reported zero vulnerabilities in
  each install audit. No package/lockfile changes. Frontend production build/lint were not rerun:
  product/frontend source is unchanged; the original hosted run passed those steps.
- Review checks: production source and CI workflow diff empty; original ProfileImageTests diff empty;
  no temporary negative controls/diagnostics remain. Both instruction files are byte-identical.
  Whitespace, relative links and sensitive/artifact checks passed; nothing staged or published.

The local suite is green; **the Linux correction is not yet established by a new hosted run**.
Current original hosted result remains 50/4. Once publication is authorised, run the unchanged
workflow on this branch, inspect all 55 backend cases and the browser step, and investigate any
remaining Linux failure before calling it fixed. Rerunning PR #120's existing old commit does not
validate this uncommitted patch. First-admin work has not started.

## Publication for hosted validation

Publication is now authorised. The fixture-only patch is prepared for a separate PR referencing #76; hosted Linux status is initially pending. Windows runs above remain separate earlier executions. No production source, 409 assertions, cleanup ordering or workflow configuration changed.

## First hosted correction attempt and newly exposed creation omissions

[Run 38785153430](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/38785153430)
ran commit 24f023d: **54 passed / 1 failed**, 55 backend cases, 2.5216 minutes.
Both expected-409 tests, restart compensation and the new fixture permission test passed.
Recovery failed later in `DeleteStage -> EnumerateFiles -> Check`, not at its original source
DB input validation. Browser was skipped. Build and frontend checks passed.

Inspection exposed additional generated-object omissions in production code: backup SQLite
snapshots were created with default file permissions; profile-images' intermediate namespace
directory was not explicitly restricted; restore restricted leaf directories but not every
recursively created ancestor. A private outer directory limits exposure but does not satisfy
per-object validation. These cannot correctly be fixed with fixture chmod or a relaxed validator.

The follow-up precreates the backup snapshot 0600, restricts the generated image namespace at
creation, and restricts every restored archive ancestor before publishing. Production validation,
409 mappings and cleanup ordering remain unchanged. This narrowly extends the original fixture-only
scope to correct file creation, with recursive recovery permission assertions retained. The final
patch therefore does contain these two small Infrastructure source corrections; the earlier
production-unchanged statement applies only to commit 24f023d and its separate Windows runs.

A complete hosted job including browser checks remains required after this follow-up.
