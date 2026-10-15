# Offline first-administrator verification — 15 October 2026

## Baseline and scope

Fetched main; PR #120 was confirmed MERGED at 2026-10-15 19:12:48 UTC. A clean tree was branched
as `feat/offline-first-admin` from `ac329818322267080a2683eb4ae90f1501b6cce2`.
Existing unrelated worktrees/private artifacts were preserved. During initial implementation no issue edits, commits, push, hosting or external delivery occurred. #81 remains a partial deployment issue.

This report records this slice's Windows local runs, not older hosted results. Retained fixtures
create fresh file-backed SQLite stores with real migrations and generated private credentials;
they stop owned API/process trees before deleting their own temporary roots. Password files,
certificates, stores and process output are not committed or uploaded. No diagnostic HTTP endpoint
or production authorization bypass exists.

## Retained provisioning evidence

`FirstAdministratorTests` contains 27 cases. Actual CLI and persisted-row assertions cover:

- New explicit department and reuse by ID; one linked active, activated account/profile, exactly
  one HR Administrator membership, three canonical roles, normalized employment number and leap
  date. Manager, schedule, activation token and leave audits remain absent. Repetition leaves
  counts unchanged. Existing passworded identities cannot be overwritten through recovery.
- Weak password, invalid email/name/number/length/date, invalid leap date, out-of-range date and
  missing/contradictory department selection: no user/profile/member/role/department changes.
- Identity-only, pending, profile-only and inactive unlinked stores refuse without overwriting.
- Wrong-case/misnormalized/duplicate canonical roles and department name/ID conflicts preserve
  original rows rather than repairing them.
- A SQLite BEFORE INSERT trigger injects late profile failure after Identity saves; users,
  profiles, memberships, roles and newly created department all roll back.
- Missing, relative and unmigrated database paths plus an insecure password file refuse; no
  missing database is created. Fixture storage assertions use the real strict permissions check
  and assert exact 0700/0600 modes when running on Unix.
- Two separate OS processes wait at explicit stdin barriers, then compete against the same store.
  Exactly one succeeds, with one persisted user/profile/membership/department. No arbitrary sleep
  or in-memory one-time flag establishes this claim.
- A real Production-mode HTTPS API starts with the provisioned store, random signing/refresh
  secrets and a generated test certificate pinned by the HTTP client. Login and HR directory
  return 200; personal balances and approve/reject return 403. No Development seed/pickup setting
  is supplied. Ordinary employee onboarding returns 400 without an approved delivery provider,
  creates no second account, and leaves requests/audits empty. A running API blocks offline use.

## Commands and results executed here

```text
dotnet build HRFlow.sln --no-restore
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~FirstAdministrator --logger 'console;verbosity=minimal'
dotnet test tests/HRFlow.RegressionTests --no-build --logger 'console;verbosity=minimal'
npm.cmd run build --prefix src/HRFlow.Client
npm.cmd run lint --prefix src/HRFlow.Client
npm.cmd --prefix tests/browser run typecheck
HRFLOW_BROWSER_CHANNEL=chrome npm.cmd --prefix tests/browser test
dotnet ef migrations has-pending-model-changes --project src/HRFlow.Infrastructure --startup-project src/HRFlow.Api --no-build
dotnet list HRFlow.sln package --vulnerable --include-transitive
npm.cmd audit --prefix src/HRFlow.Client
npm.cmd audit --prefix tests/browser
git diff --check
```

The model command used a fresh temporary connection setting, not an employee database. The browser
environment assignment above is notation: in PowerShell set `$env:HRFLOW_BROWSER_CHANNEL='chrome'`.

Initial focused run: **27 passed / 0 failed / 0 skipped**, 47 seconds. Initial complete run:
**82 passed / 0 failed / 0 skipped**, 4m30s. The final build after stronger fixture permission,
recovery-separation and Production-delivery assertions passed with **0 warnings / 0 errors**, 2.93s.
Combined backend rerun after those stronger assertions: **82 passed / 0 failed / 0 skipped**, 3m51s. A subsequent assertion-output-only change prevents failure messages from echoing generated passwords/hashes; the final build passed **0 warnings / 0 errors**, 6.21s. Final focused rerun after this message-safety change: **27 passed / 0 failed / 0 skipped**, 43s. The full suite was not rerun after this assertion-message-only change.

Retained default browser invocation: **9 passed**, 33.266s, local Chrome. Three are real HTTP
integration workflows (Employee submission/Manager decision/owner history, HR read-only monitoring,
stale employee form); six use controlled transport for session/refresh/non-replayable boundaries.
This was a separate invocation, not Production-mode first-admin browser verification.

Frontend build, lint and browser typecheck exited 0. Existing warnings: main JavaScript 605.66 kB
(177.94 kB gzip), Vite >500 kB chunk warning and build plugin timing notice; `useAuth.tsx:223`
Fast Refresh export warning. No frontend/dependency/schema change was made.
Model consistency reported no changes since the last migration. Solution-wide transitive audit
reported no vulnerable packages for all six projects; client and browser npm audits each reported
zero vulnerabilities. These are current feed results, not a security certification.

## Limits and remaining gates

At completion of the local implementation no hosted run existed. Finalisation inspects the new PR workflow separately; consult its actual head checks for the hosted outcome. Earlier #121/#120 Ubuntu passes remain dated historical evidence, not proof for this command. Local Windows runs do not exercise Linux provisioning/permission branches. Target-host service accounts, proxy/TLS/storage deployment and realistic load remain unexecuted here. Production
login was actual HTTPS HTTP with certificate pinning; no new Production browser UI walkthrough or
screenshots are claimed because this slice adds no UI. Default browser/backend gates exercise
existing workflows and fail-closed scanner cases; positive real-ClamAV and comprehensive private-file
recovery are separate prior evidence, not newly rerun positive scanning.

The two-process race proves at most one bootstrap on the tested local filesystem; it does not
certify arbitrary network storage (unsupported). The trigger tests transaction rollback, not actual
power-loss durability. Exact prior-schema mismatch, symlink variants and every Windows ACL/Linux
mount permutation are not separately exercised by the new tests; existing validators remain intact.
Operator input custody and secure credential handover remain human responsibilities. File deletion
does not prove media erasure. Production invitations, target-host recovery rehearsal and qualified
legal/calculation gates remain outstanding; bootstrap does not complete #81.

See [runbook](../operations/offline-first-administrator.md), [decision](../adr/0026-offline-first-administrator.md)
and [retained cases](../../tests/HRFlow.RegressionTests/FirstAdministratorTests.cs).

## Final review checks

Relative-link validation checked 154 local targets with no missing files. Tracked and new-file whitespace checks passed; instruction copies were byte-identical. Reviewed source/documentation and scanned intended files for private-key/token-shaped literals, machine-specific paths and unexpected artifact extensions; none were found. These checks are bounded review aids, not proof that no conceivable secret exists. No fixture stores, private credentials, logs or generated output are included. A complete patch including new files is available locally for review, outside the intended source/documentation changes.

## Independent-review finalisation

Reviewed the complete source/test/documentation diff and confirmed no API/controller or frontend change. The exclusive maintenance lease and immediate SQLite transaction precede schema/empty-store validation; Identity saves share that transaction. Production seeding and ordinary invitation gates are unchanged. Found and fixed a fixture-only credential-custody issue: its TLS certificate now uses an independently generated password, so the initial account credential is no longer reused in the child process environment. No product behavior changed. The 82-test combined, later 27-test focused and separate 9-browser results above remain separate earlier runs. New finalisation build/focused results follow; hosted results are reported on the PR after publication.

Finalisation rerun: solution build **0 warnings / 0 errors**, 7.38s; focused provisioning suite **27 passed / 0 failed / 0 skipped**, 44s. No complete local backend/browser rerun was made during finalisation because only this fixture credential separation changed. The prior combined and browser runs are identified above; the hosted workflow executes its own complete gates.
