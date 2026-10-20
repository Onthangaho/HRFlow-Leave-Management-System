# Removed profile-image hints — 20 October 2026

## Scope and source review

PR [#123](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/123) was confirmed merged
at 13:41:48 UTC. The working tree was clean; `fix/removed-profile-image-hints` was created from
fetched `origin/main`, `e5e4b55623223313a4bd5056ebae7b581c560ac3`. Unrelated ignored work remains untouched.

The shared `EmployeeDirectoryReader.ProjectAsync` supplied both directory and detail hints from
any Current image. Removal deliberately retains an empty Current row with a new version to prevent
stale operations from overwriting a later absence generation. That row is not downloadable content.
The projection now requires **Current and Size > 0**. Own-account metadata/version APIs, storage,
removal, scanner, authorization and publication protocols are unchanged. No absence row is deleted
or version reset. There is no schema, dependency or frontend production-source change.

See [ADR 0027](../adr/0027-bounded-employee-directory.md) for the existing shared projection and API contract.

## Retained checks

`EmployeeDirectoryTests.ImageHintsDistinguishContentFromRetainedAbsenceVersions` uses a fresh migrated
file-backed SQLite store, synthetic Identity accounts and real authenticated HTTP directory/detail
reads. It verifies:

- Never-uploaded owner: null hints.
- A Current row with real server-processed raster size/checksum: its version in both hints.
- Real own-account DELETE: previous row becomes Removed; a retained empty Current row has a new
  version. Directory/detail hints are null, while own-account metadata keeps that version and
  `hasImage=false`. An old-version DELETE still returns 409.
- A fixture replacement supersedes the absence row: both hints carry the new nonempty version;
  all three persisted lifecycle rows remain, including the Removed absence generation.
- Every directory/detail read leaves serialized image rows (including IDs, versions, statuses,
  sizes, checksums and timestamps), request counts and audit counts unchanged.

Populated/replacement rows are seeded projection fixtures using actual processed raster facts;
they do **not** assert successful ClamAV scanning, upload acceptance or blob download. They do not
introduce a scanner substitute. Existing image tests separately exercise processing, fail-closed
scanning, publication/removal races, restart compensation and encrypted recovery.

`HTTP directory: retained removed-image generation has no hint or thumbnail request` uses a fresh
real API/browser store. The synthetic owner removes an absent image through the real API, creating
the retained generation; own metadata confirms the rotated version. HR then searches the owner,
brings the row into view and refreshes. Initials remain visible and **zero** requests are made to
that employee's image-content endpoint. This is real HTTP, not intercepted hint/image transport.
The existing controlled viewport/URL and session-delay tests remain separately labelled.

## Executed commands and results

```powershell
dotnet build HRFlow.sln --no-restore
dotnet test tests/HRFlow.RegressionTests --no-build --filter 'FullyQualifiedName~EmployeeDirectoryTests|FullyQualifiedName~ProfileImage' --logger 'console;verbosity=normal'
npm.cmd --prefix tests/browser run typecheck
$env:HRFLOW_BROWSER_CHANNEL='chrome'
npm.cmd --prefix tests/browser test
dotnet ef migrations has-pending-model-changes --project src/HRFlow.Infrastructure --startup-project src/HRFlow.Api --no-build
dotnet list HRFlow.sln package --vulnerable --include-transitive
npm.cmd audit --prefix src/HRFlow.Client
npm.cmd audit --prefix tests/browser
npm.cmd run build --prefix src/HRFlow.Client
npm.cmd run lint --prefix src/HRFlow.Client
git diff --check
```

The model check used a new disposable connection setting. Browser measurement/screenshot/scanner
overrides were cleared. Run backend recovery gates **sequentially**, with browser/API fixtures stopped:
an initial concurrent invocation passed 22/23 but correctly refused the recovery test while the browser
fixture was alive (`WriterQuiescence.RequireStopped`). No assertion or production guard was changed.
After browser teardown, the complete affected backend invocation was rerun sequentially:
**23 passed / 0 failed / 0 skipped**, 51.789s, including encrypted image recovery. The initial
22-pass/1-refusal run is not presented as a passing execution or combined with the rerun.

Solution build: **0 warnings / 0 errors**, 12.04s. Browser typecheck passed. Complete browser invocation:
**18 passed**, 82.085s, no retries/skips (nine real-HTTP and nine controlled-transport cases).
Frontend build/lint passed; existing >500 kB Vite and `useAuth.tsx:223` Fast Refresh warnings remain.
Main bundle is unchanged at 610.88 kB / 179.21 kB gzip. Model check found no pending changes.
Solution transitive audit found no vulnerable packages across six projects; both npm audits found zero.

## Limits and evidence boundaries

These are implementation-phase local Windows/.NET 8/Chrome executions, not finalisation reruns. The earlier PR #123 hosted 87-backend/17-Chromium
pass is historical evidence, not this fix. At the local verification cutoff, no hosted run, positive real-ClamAV update/rehearsal, Linux
or full 88-case backend run is claimed here. No new screenshot is needed for this nonvisual predicate
correction. Existing synthetic screenshots remain historical. No load/capacity or production claim.
Finalisation separately checks the staged diff, sensitive content and identical instructions, then publishes the authorised PR. Its actual hosted outcome will be recorded in the PR body; no deployment or merge is authorised.
