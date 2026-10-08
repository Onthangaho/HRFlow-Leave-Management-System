# JWT expiry and current-permission verification

Issue #74; executed 8 October 2026. Security branch `fix/jwt-current-permissions` is an isolated
worktree based on fetched `origin/main`, `f47e20c4245ae71384a4a7b0fcbc80a9274f750f`.
PR #73 was verified MERGED (8 October 2026, 17:21:34 UTC). During implementation verification, no commit, push, PR or merge was made.

The original `docs/hrflow-south-africa-product-roadmap` branch retains its uncommitted work.
Authorised issue #74 wording and issue #98 UTF-8 repairs, including their local planning sources,
remain separate from this security diff. No existing issue was closed. #74 now explicitly allows
valid same-session expiry refresh/one retry, rejects invalid refresh, distinguishes permission
loss and inactive accounts, and protects new sessions/non-replayable operations.

## Setup and evidence categories

Temporary verification tooling is ignored/local, not committed automated test files. It starts
two real API processes with independent scoped DbContexts/connections sharing a newly created
disposable SQLite file. Existing databases were not altered or removed. Generated signing key,
pepper, provisioning passwords and tokens remain outside published documentation. A fresh
database was used after correcting verifier assumptions about the existing refresh hash column
and byte order; those were tooling errors, not application defects.

Access token issuance was temporarily configured to one minute, with the application's explicit
30-second skew unchanged. WAL was enabled only on the disposable file for controlled read/write
interleavings. Production/database journal configuration is not changed by this patch.

Evidence is deliberately separated:

- **HTTP/SQLite:** real login/protected/refresh HTTP calls, separate API processes, persisted
  request/status/profile/version/audit queries and forced writer waits.
- **Application/SQLite read interleavings:** a temporary .NET console composes the real handlers,
  current-role service and read wrapper, pausing a role lookup after reading it. A separate SQLite
  connection commits revocation/data changes before the handler continues. Not an HTTP test.
- **Real browser:** isolated installed Chromium launched through Playwright, real React/Vite and
  API. Route interception is explicitly identified below when used to control timing/failures.
- **Axios adapter:** the actual auth-interceptor module is transpiled temporarily and run with
  controllable adapters/refresh promises. This verifies narrow dispatch timing, not HTTP or UI.

No static rendering is claimed as interaction verification, and no screenshot is needed for this
security-only change. All fixture accounts and lifecycle reasons are synthetic.

## Executed HTTP and persisted-state checks

The completed API runner reported **19 passing groups**:

- Valid JWT: 200. Expired 40 seconds beyond expiry: 401. Expired 10 seconds: 200 within skew.
  These signed boundary fixtures use a compatible earlier `nbf`, isolating the expiry check.
- Missing `exp`: 401. `nbf` 40 seconds ahead: 401; 10 seconds ahead: 200. Wrong issuer,
  audience and signature: each 401. No raw token is included in published errors/results.
- HR-only, Manager self-decision, unassigned Manager and cross-department Manager approval
  and rejection: 403, Pending preserved and no request/audit count change.
- Real submit/approve/reject/owner-cancel and a Manager's personal request decided by their
  assigned senior Manager: successful persisted transitions, one existing audit per transition.
- Current valid access to personal history/balances, manager queue/team summary, HR monitoring,
  department reports, owner/manager/HR timelines, employee list/detail, management type/policy
  reads, roles and department/type selectors: 200.
- Old Employee JWT after current Employee membership removal: personal reads, timeline,
  selectors, submission and owner cancellation return 403 without request/audit changes.
- Old Manager JWT after Manager removal: queue/team/personal/selector/timeline/decisions denied.
- Old HR JWT after HR removal: employee list/detail, roles, type/policy list/detail, monitoring,
  report, timeline and selectors denied. Employee create/update/deactivate and configuration
  writes denied. HR-only losing its sole role does not retain authenticated-selector access.
- For a combined Employee/Manager/HR account, removing each role separately retains other
  current capabilities. Removing Employee still permits personal access via Manager; removing
  Manager denies its queue but retains personal/HR access; removing HR denies employee management
  but retains Manager/personal access. Membership restoration is fixture cleanup only.
- Five forced cross-process races: a separate connection holds `BEGIN IMMEDIATE` and removes
  the relevant role uncommitted. API process two authenticates against the old committed state
  and remains waiting. After the role removal commits, protected **submission, owner cancel,
  approval, policy creation and deactivation** each return 403. The Pending request, employee
  version and request/audit counts remain unchanged; no forbidden policy is inserted.
- Inactive owner login, refresh and old-JWT API access denied; deactivation cancels the owned
  Pending request with its matching audit.
- Refresh rotation succeeds once; replay, invalid proof and a persisted expired refresh token
  return 401. Existing `TokenHash` maps to database column `Token`; hash bytes are token then pepper.

Supplementary HTTP/SQLite checks also passed:

- Inactive Manager and HR old JWTs deny applicable reads/writes with no request/audit changes.
- An active-owner submission waits behind a separate writer; that writer commits inactive
  status before releasing. Submission returns 403 and persists nothing. This controlled
  fixture changes status directly to isolate the post-reservation guard; it is not a claim
  about executing a complete competing deactivation HTTP transaction in this particular check.
- Current HR policy create/read/versioned update/delete succeed. Employee profile-only edit
  preserves manager and combined roles. Existing active-account regressions remain usable.
- After the final build, current Identity role-catalog projection was verified using an unused
  disposable role (then removed), and revoked personal cancellation again returned 403 with
  unchanged Pending/audit state. The five controlled read interleavings were rerun successfully.
- Disposable API logs contain none of the generated passwords, signing key, pepper or raw JWTs.

Final persisted inspection after browser regressions: **8 requests, 15 AuditEntries**.
Each new request has exactly one Submit; each terminal request has exactly one corresponding
terminal audit, while Pending requests have none. Failed authorizations added no persisted
transitions. No extra audit mechanism or schema migration was introduced.

## Executed consistent-read checks

Five real handler interleavings passed: HR employee directory, Manager Pending queue, personal
balances, personal history and authenticated type selector. After live roles were read, a separate
connection acquired the writer and committed role removal plus a relevant data change (display
name, reporting line, entitlement or type name). The paused handler returned the **earlier coherent
authorized data**; a new handler/context then returned Forbidden. The writer could commit while
the read remained open, demonstrating that these deferred snapshots did not reserve the writer.
Fixture roles/data were restored afterward, without changing historical requests/audits.

The newer team/report/timeline snapshot implementations are retained and exercised through HTTP;
their internal statement boundaries were not newly forced here. Configuration/reference reads use
the same wrapper, but were not individually paused mid-projection. Rollback-journal interleavings
were not executed; WAL is the disposable evidence configuration.

## Executed real-browser checks

The completed primary browser runner reported seven passing scenarios:

- A **naturally issued** one-minute token was allowed to expire beyond the real 30-second skew.
  Opening My leave produced history responses `[401, 200]` with exactly one real refresh HTTP call.
  The refreshed session rendered the real personal history/balances successfully.
- A real successful refresh response was held with Playwright routing while the user signed out
  and signed into another account, then repeated with the same account. Release did not replace
  or clear the new session, retry the old request or display its history.
- Delayed successful 200 and injected 401 history responses released after account switching
  showed no prior history, caused no new refresh and did not sign out the new account.
- Removing current Employee membership behind an existing login produced 403 feedback on
  My leave without refresh or automatic sign-out solely because of the 403.
- An injected 401 on the explicitly non-replayable deactivation produced **one PATCH, zero
  refresh calls**, returned to login and left the target active in persisted data.
- A supplementary completed runner held real invalid-refresh 401 responses until after logout,
  account switching and same-account relogin. Release could not clear/restore the newer session
  or retry the old history request; each history operation dispatched once.

Supplementary browser scenarios executed successfully, with observed HTTP/persisted outcomes:

- Invalid, replayed and persisted expired refresh proof each caused a real API 401, one refresh
  attempt, no protected history replay and login with protected content removed. A controlled
  initial history 401 triggers refresh in these cases; this is not another natural-expiry wait.
- The actual Employee request form submitted a synthetic request, displayed success and updated
  personal history. The actual assigned Manager queue approved it; persisted status and exactly
  one terminal audit were checked. Team leave opened successfully.
- The actual HR profile-only edit saved successfully, rotated its persisted version and kept
  its manager. HR reporting, read-only Pending monitoring and timeline entry opened successfully;
  HR monitoring had no Approve controls.

The supplemental runner initially stopped on incorrect verifier assumptions about existing button
labels/navigation, after the refresh/submission/approval checks had passed. Corrected focused HR
interaction tooling completed separately. These results do not claim an unrelated UI redesign,
complete accessibility review or a newly executed full historical feature suite.

## Executed Axios-adapter checks

Thirteen narrow adapter scenarios passed: a second 401 ends the initiating session after exactly
one refresh/retry; 403 does neither; explicit non-replayable writes dispatch once; failed refresh
causes no replay; delayed refresh after logout/different-account/same-account relogin cannot clear
or retry the new epoch; old 200/401/403 responses reject as cancellations; stale retry configuration
fails before dispatch; epoch stamping happens synchronously; parallel same-session 401s share one
refresh flight. These checks supplement, not replace, the real browser/HTTP evidence above.

## Reproduce without publishing secrets

1. Build the branch; start the API in Development against a **new** file under a disposable directory.
   Supply generated signing-key/refresh-pepper/provisioning-password environment values privately;
   set `Authentication__Jwt__AccessTokenMinutes=1` only for the expiry exercise. Start a second API
   process against that same disposable file. Never point verification at an existing database.
2. Log in as each seeded synthetic role; create a combined-role account and additional synthetic
   reports using HR management. Submit distinct one-day requests and inspect LeaveRequests/AuditEntries.
3. Use the disposable signing key only to produce the boundary JWT variants described above.
   Keep `nbf <= exp`; remove `exp` separately. Use the live API, checking 200/401/403 plus persisted rows.
4. Remove each relevant AspNetUserRoles membership on the disposable file, call every applicable
   endpoint with its already-issued JWT and compare rows. For forced writer waits, remove membership
   inside a separate uncommitted immediate transaction, send the valid-body operation to API process
   two, confirm it is still waiting, then commit and verify denial/no persisted changes.
5. Start the real client with `VITE_API_BASE_URL` pointing at that API. Use a real browser. For natural
   expiry, wait until `accessTokenExpiresAtUtc + 31 seconds`, then open My leave and count history/
   refresh calls. For timing cases, hold real responses until after UI logout/login and then release.
6. Independently inspect persisted status, employee version, roles and audit rows. Inspect logs for
   generated secret values without printing them. Stop only the disposable processes you started.

No permanent automated test files are added under the current repository convention. The ignored
temporary tools and private fixtures are not intended source/PR files.

## Build, lint, inventory and limits

Actual outputs (rerun after final source changes before completion):

- `dotnet build HRFlow.sln`: **Build succeeded. 0 Warning(s), 0 Error(s).**
- `npm ci --ignore-scripts`: 130 packages added; 131 audited; **0 vulnerabilities**. No package changes.
- `npm run build`: TypeScript/Vite passed; 812 modules transformed. No dependency upgrade.
- `npm run lint`: exit 0, existing `react(only-export-components)` Fast Refresh warning in useAuth.
- Live Swagger: **29 protected operations, two anonymous auth operations and one health endpoint**;
  compared with all controller route/method attributes and the endpoint inventory.
- `git diff --check`: exit 0; only local LF-to-CRLF conversion notices, no whitespace errors.
  Both instruction copies are identical in each worktree. New-file whitespace/privacy scans and
  reverse-apply checking of the complete diff passed. No generated secrets/raw JWTs, machine paths
  in new documentation, disposable databases/logs or generated output enter the intended changes.

Remaining limitations/unexecuted checks:

- Exact wall-clock equality at the +/-30-second instant was not asserted; tested points are +/-10
  and +/-40 seconds, plus natural expiry beyond skew. No distributed clock-drift/load test.
- No new forced interleaving for every HR update/delete/reject variant, configuration projection,
  newer team/report/timeline statement boundary or complete login/deactivation race. Existing shared
  safeguards are inspected; do not confuse that with new execution of every historical race suite.
- No direct browser pending-login/logout race; the new login guard is source/build-reviewed.
  No multi-tab permission push or server-side logout denylist.
- No new forced unexpected database rollback/migration exercise: this patch adds no migration and
  retains existing transaction/audit mechanisms. No production deployment, throughput certification
  or complete browser/accessibility regression suite. SQLite is still a single database-wide writer.
