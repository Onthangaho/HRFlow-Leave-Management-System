# ADR 0005: Employee deactivation and access revocation

## Scope

Issues #25 (Employee deactivation with cascading pending-request cancellation) and #26
(Deactivate endpoint + block new requests from inactive employees) were verified open.
PR #68 was verified merged. This lifecycle change includes the connected directory status and manager-selector contract;
no deactivation button,
reactivation, hard deletion, password reset, or new audit system is introduced.

## Endpoint and UI contract

HR uses PATCH /api/v1/employees/{id}/deactivate with JSON:

```json
{ "expectedVersion": "<version loaded from the employee DTO>", "reason": "Employment ended" }
```

The route supplies the target; actor and target command properties ignore JSON. The actor
comes from authentication. The existing HR JWT policy applies, and current Identity HR
Administrator membership plus an active employee profile are checked again inside writer
protection. HR directory/detail DTOs now include isActive; their other fields and the
create/update contract remain unchanged. Inactive profiles remain visible to authorized HR.

200 returns employeeId, version (rotated), isActive: false, and cancelledRequestCount.
A future confirmation interface must explain the irreversible-through-this-workflow
access loss and pending cancellations, show the exact employee, require a reason, and
retain the originally loaded version. Reason is required, limited to 500 UTF-16 characters
in the submitted string, and trimmed before storage. Reasons are sensitive HR context;
do not log them. Versions are opaque nonempty GUIDs.

400 means invalid version/reason shape; 404 means missing employee; 403 means insufficient
or revoked HR membership; 409 means stale version, already inactive, active direct reports,
last active HR, or expected writer contention. Version mismatch is checked before inactive
state: the original version returns a stale conflict, while a refreshed inactive version
returns already inactive. Neither attempt creates audits or rotates the version again.
Missing/inactive authentication returns 401 ProblemDetails. A request authenticated before
concurrent deactivation can instead return 403 from the protected active-account check.

Profile edits cannot reactivate or edit inactive employees and return 409. No update DTO
accepts lifecycle state. New manager assignments require an active, current Manager account
in the same department, with existing self/cycle safeguards. Managers with active reports
must have them reassigned first; inactive reports retain their existing reporting links.
Role removal also counts only active HR accounts, so preserved inactive memberships cannot
allow removal of the last functioning administrator. The existing client can still edit
active employees with the same profile/roles/manager-operation/version contract. The client
employee contract includes isActive, the directory displays text Active/Inactive badges, and
new assignment choices include only active, same-department Manager accounts other than self.
Editing retains the loaded manager snapshot and defaults to Preserve; filtering new choices
does not silently clear an existing reporting relationship. The server remains authoritative.

## Atomic history preservation

Employee.Deactivate records IsActive=false, DeactivatedAtUtc, DeactivatedById (domain HR
employee ID), and trimmed DeactivationReason, and rotates the existing edit Version.
Profile, Identity account, roles, department and manager links remain stored.

The transactional employee service loads all owned Pending requests and calls their existing
Cancel transition with the acting HR employee. Each existing AuditEntry records Action=Cancel,
Pending -> Cancelled, UTC timestamp, and Reason="Employee deactivation: " plus the supplied
trimmed reason. Audit reason capacity is 522 characters to retain all 500 reason characters
and the cause prefix. Normal owner cancellation and historical audits retain null reasons.
Approved, Rejected and already Cancelled requests and all prior audits remain unchanged.
No historical request is deleted, rewritten or reassigned. Existing authorized reporting
queries can continue reading preserved records; inactive employees themselves lose access.

State/version, all cancellations, audit rows and lifecycle metadata commit together.
An unexpected failure rolls everything back; tracking is cleared by the existing wrapper.
Structured logs contain identifiers/counts and existing correlation context, never reasons,
passwords or tokens. Unexpected errors retain the sanitized 500 path.

## Security and serialization

The existing Infrastructure SqliteWriteTransaction acquires BeginTransaction(deferred:false)
(BEGIN IMMEDIATE), then attaches it to the scoped EF context before authoritative reads.
Deactivation, employee creation/edit/roles/reassignment, policy writes, submission, decisions,
owner cancellation and active development seeding use this same database-backed reservation.
All participating connections/processes sharing the file therefore follow a serial order.
Expected employee versions are checked after reservation and remain EF concurrency tokens.

Bearer OnTokenValidated queries persisted account/profile active state on every protected
request, including already-issued JWTs. There is no positive-status cache. Login and refresh
now also reserve the writer before account/status reads and token issuance. Refresh revocation
and new token persistence share the transaction. Inactive login/refresh return the existing
generic 401 failure contracts, without disclosing account existence. Stored Identity and
refresh records are retained; active-state checks disable their use. Ordinary JWT lifetime
configuration is unchanged in this focused lifecycle work.

Protected submission and owner cancellation repeat active-owner checks. Manager decisions
repeat active/current Manager membership; deactivation has already cancelled an inactive
owner's Pending requests, so a later approval/rejection encounters a status conflict.
HR employee and policy writes repeat active/current HR checks. A bearer read that preceded
deactivation cannot authorize a write using stale active state. Read-only requests already
in flight may complete using their earlier authorization snapshot; subsequent protected
requests fail. No retroactive cancellation of in-flight HTTP responses is claimed.

The existing three-second provider contention timeout and absence of application callback
replay are retained. Clients must reload and explicitly retry a conflict. SQLite has one
writer for the whole database; login/password checking also holds this reservation, increasing
contention under load. Native waits are synchronous and not an overall request deadline.
Direct SQL, older binaries, other providers, network filesystems and production throughput
require separate evaluation. All application instances must use the upgraded protocol.

## Migration and startup

20261007081210_AddEmployeeDeactivation adds IsActive with a true database default, nullable
employee lifecycle metadata, and nullable AuditEntries.Reason. The actor reference uses
RESTRICT. Existing employee versions and every original profile/Identity/request/audit field
are preserved; existing employees become active. No existing record is silently repaired,
renamed or deleted. SQLite rebuilds Employees for the actor FK; back up and stop all writers
before migration. Downgrading discards lifecycle/reason metadata and can restore access under
old code, so it requires an explicit operational plan rather than routine rollback.

Development Identity provisioning and reporting repairs now share writer protection.
Existing accounts are not reprovisioned and revoked roles are not restored. Reporting seed
repairs skip inactive employees/managers and never set IsActive=true on an existing profile.
Production startup continues not to seed or automatically migrate. Development defaults
remain development-only provisioning, not production account administration.

## Evidence and references

See [executed verification and limitations](../verification/employee-deactivation.md).

- [Microsoft.Data.Sqlite transaction behavior](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions)
- [JWT post-validation hook](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.authentication.jwtbearer.jwtbearerevents.ontokenvalidated)
- [Existing reservation design](0002-consistent-leave-decisions.md)
