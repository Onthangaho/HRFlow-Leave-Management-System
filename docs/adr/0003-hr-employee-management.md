# HR employee and reporting management on SQLite

## Contract and authorization

HR uses `GET /api/v1/employees` (or `GET /api/v1/employees/{id}`), `POST` to create,
and `PUT /api/v1/employees/{id}` to replace an employee edit snapshot. Controllers map
commands through MediatR; Infrastructure coordinates the linked Identity account.
Writes require both the HR JWT policy and current Identity HR Administrator membership,
checked inside the protected operation. Revocation therefore blocks an existing JWT from
writing. Read models contain employee ID, department ID/name, manager ID/name, all roles,
and an opaque `version`; they contain no credentials, hashes, or tokens.

Creation accepts `fullName`, `email`, an initial `password`, `departmentId`, a nonempty
`roles` collection, and an optional `managerId`. Accounts without managers are allowed.
Personal leave submission still requires a valid reporting assignment.

Update accepts the profile fields, a complete replacement `roles` collection, and
`expectedVersion` from the original read. The route supplies the target; an optional
body `employeeId` must match. Password changes are not part of this workflow.

`managerAssignment` has three explicit values:

- `Preserve` (including omission): keep the existing manager, even when it is null.
- `Assign`: require a nonempty `managerId`.
- `Clear`: intentionally remove the manager.

Preserve/Clear must not carry a manager ID. The effective relationship is validated
against the resulting department, including a preserved assignment after a department
change. Managers must exist, hold the current Manager role, belong to the same department,
and create neither self-assignment nor a reporting cycle. A manager with direct reports
cannot move departments or lose Manager membership until those reports are reassigned.

Only Employee, Manager, and HR Administrator roles are supported. Case/whitespace are
normalized and duplicates removed. An update replaces all roles explicitly; the form
loads the complete collection so a profile edit preserves combined capabilities.
Removing the last Identity HR Administrator is rejected, including concurrent removals.
Changing roles does not rewrite existing JWT claims: sign in again to refresh navigation
and claimed capabilities. Live checks protect management writes and manager decisions.

## Concurrency, atomicity, and migration

`IEmployeeManagementTransaction` exposes only a protected callback to Application.
The shared Infrastructure `SqliteWriteTransaction` also implements the existing
`ILeaveDecisionTransaction`; it acquires `SqliteConnection.BeginTransaction(deferred: false)`
before any validation/authorization database reads, then enlists the scoped context with
`UseTransactionAsync`. Microsoft.Data.Sqlite issues `BEGIN IMMEDIATE`, reserving SQLite's
single writer across connections and API processes. Waiting requests validate only after
that reservation is acquired. Reciprocal assignments, role removal against report
assignment, and last-HR removals therefore cannot both validate an obsolete graph.

UserManager and the employee service use the same scoped HRFlowDbContext. Identity's
intermediate saves, employee changes, roles, login/email, and manager assignment all
remain in that outer transaction. An error disposes/rolls it back; tracking is cleared.
A new audit mechanism is not introduced. Leave history and existing decision audits
remain unchanged when reporting assignments change; Pending decisions use the current
manager relationship through the existing leave safeguard.

Each successful employee update rotates a Guid `Version`, including role-only edits.
The client submits its original `expectedVersion`; the protected service compares it
before changes and EF maps it as a concurrency token as an additional persistence check.
A stale edit returns 409 rather than silently rebasing/replacing newer fields or roles.
The form requires an explicit reload. The AddEmployeeEditVersion migration adds only
this column and initializes existing tokens from each unique employee ID; existing
profile, Identity, role, leave, and audit rows are preserved.

The provider waits for contention up to a three-second command timeout. BUSY/LOCKED and
actual EF concurrency failures return safe 409 ProblemDetails; there is no application
callback replay. A fresh HTTP request revalidates state. Invalid relationships/roles or
Identity validation return 400, duplicate emails return 409, missing employees return
404, and denied access remains 403. Unexpected database failures are not broadly treated
as conflicts and use the existing sanitized 500 path and correlation logging.

SQLite serializes writers for the whole database. Password hashing and Identity work
also hold this reservation, limiting write throughput; the timeout is per command,
not a total HTTP deadline, and native waits are synchronous. All application instances
must use this protocol and the same database file on a filesystem supporting SQLite
locking. Direct SQL or older binaries can bypass business validation. Production load,
network filesystems, and another database provider require separate evaluation.

## Verification (6 October 2026)

Temporary tooling outside the repository used a disposable SQLite database, two API
processes on separate ports, real authenticated HTTP requests, and independent DbContexts
for persisted-row inspection. No repository automated test files or existing databases
were modified.

Passed: creation/login of Employee, Manager, HR-only, and combined-role accounts;
profile-only role/manager preservation; explicit Assign/Clear/Preserve; invalid, missing,
cross-department, self, and cyclic managers; contradictory contracts; direct-report
safeguards; invalid preserved managers after department edits; stale updates; duplicate
email and Identity validation rollback; direct API permission denials; revoked HR JWT
write denials; last-HR protection; and reassignment of an existing Pending request with
historical decisions/audits unchanged.

Across two processes, reciprocal assignment races ran five times (one 200, one 400;
acyclic persisted graph). Same-version edits returned one 200 and one 409. Assignment
versus Manager-role removal races ran three times (one 200, one 400; valid persisted
reports). A held writer lock returned 409 in 3.11 seconds without changes. An injected
late employee-insert failure after Identity saves rolled back all related rows; it
returned sanitized 500 and the disposable trigger was removed. A read-only backup of
a pre-migration disposable database was upgraded and inspected: only initial version
tokens changed, while all prior profile, Identity, role, leave, and audit data survived.

Isolated headless Chromium exercised the real Vite UI against that disposable API:
creation with three roles, disabled duplicate submission, prefilled edit IDs/roles,
profile-only preservation, Clear/Assign, eligible manager selection excluding self,
invalid department-preserve feedback, stale 409 with explicit reload and a fresh save.
The directory updated without document reloads; there were no page errors or mobile
horizontal overflow at 375px. Desktop/mobile screenshots were inspected, and an
independent DbContext checked the final browser record and linked Identity/roles.
The connected interactive browser service was unavailable, so no user-browser session
or additional browsers were checked. Production load and full accessibility audits
were not run. Repository automated tests remain deliberately deferred.

Final checks: `dotnet build HRFlow.sln` succeeded with zero warnings/errors; frontend
production build succeeded (223 modules); lint exited successfully with only the
existing `useAuth.tsx:184` Fast Refresh export warning. `git diff --check` passed.
Dependency findings are recorded below; no dependency versions were changed.

## Dependency advisory investigation (6 October 2026)

`npm audit --json` reports two packages with high aggregate severity, not just two
individual advisories. `npm audit --omit=dev --json` reports only Axios. Audit exits
with code 1 because vulnerabilities remain; it is not a failed verification command.

- `hrflow-client -> axios@1.19.0`: direct production dependency, bundled into the SPA.
  The audit contains seven high and five moderate Axios advisories. High findings
  cover Node data-URL/proxy ReDoS, Node HTTP/2 controls and errors, a Node socket gadget,
  form-serialization prototype-pollution gadgets, and fetch redirect-control bypass.
  The current client uses `axios.create` JSON requests with the browser's default XHR
  adapter preference; it has no Node Axios service, multipart serializer, explicit
  fetch adapter, or `maxRedirects: 0` guard. This is a code-inspection assessment of
  current exposure, not proof that every pollution gadget is unreachable or a reason
  to suppress the production advisory. Patched Axios `1.20.0` is available and fits
  the existing `^1.19.0` range. A targeted reviewed update can fix the audit findings.
- `hrflow-client -> postcss@8.5.26 -> source-map-js@1.2.1`, and
  `hrflow-client -> @tailwindcss/postcss@4.3.3 -> @tailwindcss/node@4.3.3 -> source-map-js@1.2.1`:
  transitive development/build dependencies. Malicious indexed source maps with huge
  section offsets can block the Node event loop (GHSA-68fv-2mgg-jv7q). These paths are
  used during CSS/build work, not as an HRFlow browser/API runtime service. Patched
  `source-map-js@1.2.2` is available and satisfies both parents' `^1.2.1` range; a
  targeted lockfile refresh can select it without a major upgrade or forced audit fix.

Package.json and package-lock.json are unchanged. No `npm audit fix`, forced upgrade,
or unrelated dependency changes were performed. Remediation belongs in a separately
reviewed dependency update, followed by build/lint and browser verification. Exploit
reproductions were not run against this app.

Advisory references (all listed Axios findings are patched in 1.20.0):

- Node data URL ReDoS: [GHSA-c29m-xwm3-cm6r](https://github.com/advisories/GHSA-c29m-xwm3-cm6r).
- Node proxy ReDoS: [GHSA-mghh-pgcx-3jjj](https://github.com/advisories/GHSA-mghh-pgcx-3jjj).
- Form serialization gadget: [GHSA-x97p-jq2g-jp4f](https://github.com/advisories/GHSA-x97p-jq2g-jp4f).
- HTTP/2 control bypass: [GHSA-3pq3-5fj3-cg6v](https://github.com/advisories/GHSA-3pq3-5fj3-cg6v).
- HTTP/2 unhandled error: [GHSA-542g-h47m-68v8](https://github.com/advisories/GHSA-542g-h47m-68v8).
- Node socket gadget: [GHSA-m8m8-qj5v-23w3](https://github.com/advisories/GHSA-m8m8-qj5v-23w3).
- Fetch redirect guard bypass: [GHSA-r4gj-5m52-g5wh](https://github.com/advisories/GHSA-r4gj-5m52-g5wh).
- Moderate Axios findings: [fetch gadget](https://github.com/advisories/GHSA-vh66-26gq-q6x8),
  [default method gadget](https://github.com/advisories/GHSA-9fr6-4gfg-395g),
  [inherited headers](https://github.com/advisories/GHSA-j8rh-479h-cp32),
  [FormData headers](https://github.com/advisories/GHSA-4hqw-qxg8-jxx2), and
  [NO_PROXY CIDR](https://github.com/advisories/GHSA-44g4-m2mj-wpvx).
- Source maps: [GHSA-68fv-2mgg-jv7q](https://github.com/advisories/GHSA-68fv-2mgg-jv7q)
  and [maintainer's 1.2.2 release](https://github.com/7rulnik/source-map-js/releases/tag/v1.2.2).

## Official references

- [Microsoft.Data.Sqlite transactions](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions)
- [Provider transaction implementation (8.0.23)](https://github.com/dotnet/efcore/blob/v8.0.23/src/Microsoft.Data.Sqlite.Core/SqliteTransaction.cs)
- [EF Core external transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions#using-external-dbtransactions-relational-databases-only)
- [SQLite busy errors and provider timeouts](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/database-errors)
- [EF Core application-managed concurrency tokens](https://learn.microsoft.com/en-us/ef/core/saving/concurrency#application-managed-concurrency-tokens)
