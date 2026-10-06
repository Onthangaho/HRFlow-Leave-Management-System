# Leave type and shared-policy management on SQLite

## Scope and contract

Issue #23 is the backend API for User Story 6. HR management UI is issue #24 and is not
included. GitHub issue #23 was verified open with title "Leave type/policy CRUD endpoints".

Management routes are `GET/POST /api/v1/management/leave-types` and
`GET/POST /api/v1/management/leave-policies`, with `GET/PUT/DELETE /{id}` for each resource.
They require the existing HR Administrator JWT policy. Every write additionally checks
current Identity HR Administrator membership **inside the writer reservation**, so role
revocation blocks an old JWT. Controllers delegate to the Application configuration
service; actor identity is derived from authentication, never from JSON. Route IDs are
the only target identifiers; input DTOs have neither target nor actor ID fields.

The authenticated employee selector remains `GET /api/v1/leave-types`. It now returns
explicit `{ id, name }` DTOs, without EF navigation properties or management information.
The existing personal balance/history and manager decision contracts are unchanged.

Policy creation requires `name`, `allowOverlap` and integer `defaultBalance`. Type
creation requires `name` and nonempty `leavePolicyId` referring to an existing policy.
PUT replaces those same fields and requires `expectedVersion` from that resource's
management DTO. DELETE requires `?expectedVersion=<version>`. Versions are opaque,
nonempty GUIDs; every successful edit rotates the edited resource's version, including
an edit whose fields are unchanged. A type edit does not modify the policy or its version.

Example policy creation body:

```json
{ "name": "Standard entitlement", "allowOverlap": false, "defaultBalance": 20 }
```

Type creation links the returned policy ID explicitly. A policy may be shared by several
types; there is no one-policy-per-type restriction. Policy names need not be unique.
Both names are trimmed, must contain 1–100 UTF-16 characters, and cannot contain control
characters. Type uniqueness uses the trimmed name's .NET invariant uppercase value,
persisted as `NormalizedName` with a unique database index. There is no accent folding
or Unicode composition normalization. Entitlement must be a nonnegative integer.
Zero means **no positive-day leave may pass submission or approval**, not unlimited
entitlement or a special unpaid-leave bypass. The existing zero-entitlement Unpaid seed
keeps this behavior.

Management policy DTOs contain `id`, `name`, `version`, `allowOverlap`, `defaultBalance`,
`linkedLeaveTypes` (`id`, `name`, `requestCount`) and `canDelete`. Type DTOs contain
`id`, `name`, `version`, `leavePolicyId`, `policyName`, `policyVersion`, `allowOverlap`,
`defaultBalance`, `requestCount` and `canDelete`. Counts include **all request statuses**.
These are advisory read snapshots: writes recheck versions and references under protection.
No employee payload, credential, password hash, or token is exposed.

Creation returns 201 with a management-detail Location and complete DTO. GET/PUT return
200; successful DELETE returns 204. Invalid input, missing/empty edit versions and
policy-rule failures return 400; missing resources/policy references return 404. Missing
authentication returns 401 and denied HR access returns 403. Stale versions, normalized
name collisions, referenced deletions and expected lock contention return 409. Responses
use ProblemDetails with useful messages; the management prefix also formats authorization
denials before MVC runs. Existing structured logging and correlation headers remain in
use, without logging names, rule payloads, or credentials.

## Current-policy consequences and history

Updating a policy affects **every linked type** for future submissions, pending approvals,
and current balance reads. Reassigning a type to another policy has the same consequence
for that type. Entitlement remains per employee **and type**, not pooled across types
that share a policy. Inclusive calendar days and overlap checks within the same type
remain unchanged. Pending requests reserve nothing; Rejected/Cancelled requests consume
nothing. No annual reset, accrual, carry-over, working-day or effective-date system is added.

Approved status, decision maker/time and audit rows are never rewritten. Because balances
use current rules and approved history, reducing entitlement below already approved use
can yield a negative remaining balance. Historical approval is retained; subsequent
positive-day submissions/approvals fail. An approval serialized before a policy reduction
may therefore remain approved under the earlier rule. If the reduction serializes first,
approval validates the new rule and fails without changing Pending status or adding an
approval audit. Type names displayed in history remain current names, as before; no
historical policy/name snapshot is introduced.

A type referenced by **any** request, including Rejected or Cancelled, cannot be deleted.
A policy referenced by any type cannot be deleted. There is no cascading deletion or
implicit reassignment. Unused records can be deleted with matching versions. Existing
request-to-type restriction is retained; policy-to-type deletion now also uses RESTRICT.

## Writer protection and limitations

`ILeaveConfigurationTransaction` is the minimal Application callback interface implemented
by Infrastructure's existing `SqliteWriteTransaction`. Management writes, submissions,
and development configuration seeding use the same reservation as decisions and employee
management: `BeginTransaction(deferred: false)` (`BEGIN IMMEDIATE`) before authoritative
reads, then `UseTransactionAsync` on the scoped EF context. Submission validators now
check only shape/dates; employee/reporting/type/policy/history reads occur after reservation.
If deletion wins, a submission's missing type returns a safe 409 asking to reload. If
submission wins, deletion sees the request and returns 409. Foreign keys also protect
references at persistence time. Narrow handling maps the normalized-name unique constraint
and configuration/request FK failures to 409; other unexpected constraints/errors retain
the sanitized 500 path. No broad database-exception-to-conflict mapping is added.

The reservation serializes separate DbContexts, connections and API processes sharing the
same SQLite file. Expected versions are compared after reservation and are EF concurrency
tokens as a second safeguard. Intermediate saves used to project response DTOs, final saves,
and commit are in the same transaction; failure rolls back and clears tracking. No new audit
mechanism is introduced. The provider waits up to three seconds per command, with **no
application callback replay**. A fresh HTTP attempt must reload and revalidate after a conflict.

SQLite still has one writer for the entire database, limiting throughput. Native waits are
synchronous, and the command timeout is not an overall request deadline. All writers must
use this protocol on a filesystem supporting SQLite locks. Direct SQL and older binaries
can bypass business validation; uniqueness/FKs remain final relational safeguards. Another
provider, network filesystems and production load need separate evaluation. Apply migrations
with writers stopped and a backup before starting upgraded API instances.

Development defaults are seeded only when the application store has no employees or policies.
Startup therefore preserves HR's type deletions and renames rather than resurrecting them.
The active startup seed operation participates in writer protection. Development Identity
account seeding retains its existing behavior and must not be used as production provisioning.

## Migration and legacy review

`20261006201033_AddLeaveConfigurationManagement` adds type normalized names and type/policy
versions, adds policy labels, enforces nonnegative entitlement/name lengths and normalized
type uniqueness, and changes policy-to-type deletion to RESTRICT. Existing IDs initialize
edit versions. Previously policies had **no name field**; their new labels are explicitly
`Policy <existing ID>`, which HR can edit. No existing type name, entitlement, overlap flag,
relationship, request, employee/Identity record or audit is changed.

Preflight stops the migration before schema/data changes for blank/overlong/control-containing
legacy type names, normalized duplicates, negative entitlement, or orphaned type/policy/request
references. SQLite's built-in uppercase cannot generate .NET invariant Unicode keys, so legacy
non-ASCII names also stop for explicit review. New management names support Unicode after
migration. Do not silently delete/rename legacy records to make migration pass: back up the
database, report affected IDs and agree on explicit remediation (or a reviewed .NET backfill
migration for valid Unicode names) before retrying. This Unicode backfill is a known limitation
of this migration, not a declaration that such existing names are invalid business data.

Preflight inspection for an existing database (read-only connection):

```sql
SELECT Id, Name FROM LeaveTypes
WHERE Name IS NULL OR length(trim(Name)) NOT BETWEEN 1 AND 100
   OR Name GLOB '*[^ -~]*' OR instr(Name, char(0)) > 0;
SELECT upper(trim(Name)), count(*) FROM LeaveTypes
GROUP BY upper(trim(Name)) HAVING count(*) > 1;
SELECT Id FROM LeavePolicies WHERE DefaultBalance < 0;
SELECT t.Id FROM LeaveTypes t LEFT JOIN LeavePolicies p ON p.Id = t.LeavePolicyId
WHERE p.Id IS NULL;
SELECT r.Id FROM LeaveRequests r LEFT JOIN LeaveTypes t ON t.Id = r.LeaveTypeId
WHERE t.Id IS NULL;
```

For non-ASCII rows, also compare `.Trim().ToUpperInvariant()` in a .NET inspection tool;
SQL alone is not a Unicode-equivalent duplicate check. Existing application databases were
not migrated during this task. A read-only backup of earlier disposable data was upgraded
and compared; verification evidence is recorded separately.

## Official references

- [Microsoft.Data.Sqlite transactions and deferred-transaction warning](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions)
- [Microsoft.Data.Sqlite 8.0.23 transaction implementation](https://github.com/dotnet/efcore/blob/v8.0.23/src/Microsoft.Data.Sqlite.Core/SqliteTransaction.cs)
- [EF Core application-managed concurrency tokens](https://learn.microsoft.com/en-us/ef/core/saving/concurrency#application-managed-concurrency-tokens)
- [SQLite foreign-key restrictions](https://www.sqlite.org/foreignkeys.html)
- [Verification record](../verification/leave-policy-management.md)
