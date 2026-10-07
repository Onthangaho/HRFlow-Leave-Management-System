# Employee deactivation verification (#25 and #26)

Executed on 2026-10-07 using disposable SQLite data, real HTTP requests to two independently
started API processes sharing one database, and an independent SQLite inspection connection.
Temporary tooling, databases, credentials, tokens and logs remain outside the repository.
No automated test files were added. Existing application databases were not modified.

## Executed lifecycle and security checks

- HR PATCH returned 200 and cancelled exactly two owned Pending requests. Persisted state
  was inactive, edit version changed, and lifecycle actor/time/reason were recorded.
- Each cancelled request had exactly one audit: acting HR employee, UTC timestamp,
  Pending -> Cancelled, Action=Cancel, and the deactivation cause plus full supplied reason.
- Approved, Rejected and previously Cancelled request rows and their existing audits were
  compared field-for-field and remained unchanged. Identity account, roles and reporting
  links remained stored. HR detail returned isActive=false.
- Employee/Manager direct calls returned 403. HR role removed from Identity after login:
  old JWT write returned 403. Deactivated HR old JWT write returned 401.
- Blank/501-character reasons returned 400; absent employee returned 404; stale and repeated
  original/current-version attempts returned 409 without duplicate cancellation audits.
- Last active HR deactivation and last-active-HR role removal returned 409 even with inactive
  HR memberships preserved. Active reports blocked manager deactivation with 409; reassignment
  succeeded; manager deactivation then succeeded. Assignment to inactive manager returned 400;
  inactive manager's old-token decision returned 401.
- Inactive login/refresh returned 401. Old tokens returned 401 on selector, history, balances
  and submission. PUT against an inactive record returned 409 and did not restore access.
- A temporary audit-insert trigger forced an unexpected late 500. Persisted employee state,
  version, Pending status and audit rows matched their pre-operation snapshots. Trigger removed.
- Active login, refresh, submission, approval, HR create/edit, directory and history returned
  their expected success responses. Profile updates preserved the existing API contract.

## Two-process outcomes on the final functional build

Eight simultaneous submission/deactivation pairs:
- Deactivation returned 200 in all eight.
- Submission returned 401 twice and 403 six times, reflecting denial at bearer validation
  or the authoritative protected operation respectively.
- All eight inactive employees had zero new requests. This sample did not observe submission
  winning a simultaneous race. A separate submission completed through process two before
  deactivation through process one returned 200/200 and persisted Cancelled with one HR audit.

Eight simultaneous approval/deactivation pairs:
- One approval returned 204 before deactivation's 200; its persisted Approved decision and
  one approval audit were retained.
- Seven approvals returned 409 after deactivation's 200; each persisted Cancelled with one
  HR cancellation audit. There were no duplicate terminal transitions.

Additional simultaneous operations through the two processes:
- Edit/deactivation: 409/200; persisted inactive without an overwritten version/profile.
- Owner cancellation/deactivation: 403/200; one Cancelled transition/audit.
- Rejection/deactivation: 409/200; one Cancelled transition/audit.
- Manager assignment/deactivation: 400/200; the report retained an active manager.

These are observed samples, not exhaustive scheduler or production-load proof. SQLite's
existing writer reservation, not an in-memory lock, coordinates these separate processes.

A held writer reservation on an independent SQLite connection caused deactivation to return
409 ProblemDetails after 3233 ms. The profile/version stayed unchanged; no operation replay
occurred. This checks expected contention handling, not production throughput.

## Remaining boundary checks completed before PR review

Real HTTP calls through two API processes and an independent SQLite inspection connection:
- Eight login/deactivation races: login won twice (200), lost six times (401); deactivation
  returned 200 in every pair. All old and newly issued tokens returned 401 after commit.
- Eight refresh/deactivation races: refresh won four times (200), lost four times (401);
  deactivation returned 200 in every pair. Issued-token protected reads/writes returned 401
  after commit. A login-started-first case also returned 200/200, then protected access 401.
- Both operations were released from a held independent writer reservation in the simultaneous
  samples. No credentials or token values are included in this report.
- Eight HR role-removal/deactivation races with exactly two active HR accounts: deactivation
  won (200), competing last-HR role removal returned 409, and persisted active HR count stayed
  at least one. Count was inspected at operation completions and afterward.
- A role-removal-started-first case returned 200/403: removal revoked the actor's HR capability,
  so the protected deactivation check refused its old JWT. One active HR remained.
- Inactive HR old-token creation attempts returned 401 and employee row counts were unchanged.
  Existing active/profile/role checks were inspected inside employee/policy writes, submission,
  owner cancellation, manager decisions and token issuance; no additional guard gap was found.

Client follow-up: isActive is part of the employee TypeScript contract. Source inspection
confirmed eligible choices require active status, same department and Manager membership,
exclude self, and leave the loaded edit snapshot and Preserve default intact. The directory
has visible text Active/Inactive badges. Frontend TypeScript/Vite build and lint were rerun.
No deactivation button was added. Actual browser verification remains unavailable.

## Migration and restart

A read-only backup of earlier disposable pre-change data was migrated separately. Comparison
passed for all original columns and rows: five Employees, one LeaveRequest, one AuditEntry,
five Identity accounts/memberships, and three leave types/policies. All five employees became
active with null lifecycle metadata; old audit reasons were null; foreign_key_check was clean.
The source database was untouched. Restart against the lifecycle verification database
preserved inactive state/version, denied inactive login/old tokens, retained HR read access,
and did not restore a revoked role. Final migration and restart checks were repeated successfully. Final cancellation-cause formatting was rechecked after
its small implementation adjustment; lifecycle/HTTP/concurrency checks passed again.

## Commands and actual outputs

- Initial and final cached backend builds: Build succeeded, 0 Warning(s), 0 Error(s).
- Normal dotnet build HRFlow.sln attempted online restore: failed with NU1301 because this
  restricted environment could not reach NuGet signature metadata.
- A first offline restore into the environment-selected workspace cache completed but build
  failed with NETSDK1064 missing analyzer packages. The generated cache was removed. Explicit
  restore using the existing user package cache and dotnet build HRFlow.sln --no-restore passed.
- npm run build: TypeScript/Vite production builds passed, 231 modules; rerun after the three connected client changes.
  Vite reported a plugin-timing advisory, not a compilation failure.
- npm run lint: exit 0; existing useAuth.tsx Fast Refresh only-export-components warning remains.
- EF migrations has-pending-model-changes: No changes have been made to the model since the last migration.
- git diff --check: passed (Git emitted only local LF/CRLF normalization notices).
- Startup in the restricted Windows environment initially failed because Windows Event Log
  access was denied. Disabling that provider only in disposable-process environment variables
  allowed verification; no repository logging configuration was changed.

## Limitations and unexecuted checks

- Interactive employee edit-form/browser verification was not executed: the available computer-use
  tool returned no apps or browsers. The frontend build/lint and real active edit API checks
  passed, but they are not evidence of an actual form interaction.
- No deactivation action UI is included. Directory status badges and active-only manager choices
  are connected to the new read contract; their actual browser rendering was not verified.
- Executed concurrency samples cover the previously remaining token/role boundaries, but do
  not provide exhaustive scheduling or production-load proof.
- Only Chromium-independent HTTP checks were performed for this backend task; no browser,
  cross-provider, network-filesystem, production deployment/load or exhaustive contention tests.
- SQLite retains the three-second provider wait, no application replay, and one writer for
  the database. Login/password checks now also hold that reservation. Requests already in flight
  can complete a read using an earlier authorization snapshot; subsequent requests are denied.
- No permanent automated tests were added, following repository instructions.
