# ADR 0006: Read-only manager team leave summary

## Scope and API

Issues #27 (Team leave summary query) and #28 (Manager team summary view) were verified
open, and PR #70 was verified merged before branching from fetched origin/main.

`GET /api/v1/leave-requests/team-summary?start=2026-11-01&end=2026-11-30`
returns a collection, including `[]` for an empty team or an empty range result.
Both dates are required in YYYY-MM-DD format. End must be on or after start;
the range is limited to 62 inclusive calendar days (one or two adjacent months).
Malformed, missing, reversed or oversized ranges return 400 ProblemDetails.

JWT Manager authorization is the first gate. The controller derives the Identity actor
from authentication, and the Application handler reloads the active employee and actual
Identity Manager memberships in the read snapshot. Revoked membership returns 403 even
with an old Manager JWT. Inactive bearer accounts receive the existing 401; an in-flight
request that passed bearer validation before deactivation may receive 403 at the snapshot
check instead. Employee and HR-only accounts cannot use this capability. Combined-role
accounts retain their Manager capability, without adding an HR override.

Only Approved requests of current same-department direct reports are returned, excluding
self. A request intersects when its start is on/before the selected end calendar day and
its end is on/after the selected start calendar day. Complete request dates are returned,
not clipped to the range. Ordering is start date, employee name, then request ID.
Reassignment changes current visibility, without modifying requests or audit rows.
Inactive reports' Approved records remain visible and carry `isActive: false`.

Each DTO contains `requestId`, `employeeId`, `employeeName`, `isActive`, `leaveTypeId`,
`leaveTypeName`, `startDate` and `endDate`. No email, Identity link, reason, credential,
policy internals or audit payload is projected. No schema migration is needed.

## Consistent read without writer reservation

`ITeamLeaveReadTransaction` is the minimal Application seam. Infrastructure explicitly
calls Microsoft.Data.Sqlite `BeginTransaction(deferred: true)` and enlists the existing
scoped EF context. The handler uses no-tracking projections, and the current role lookup
uses this same scoped context. The first employee SELECT establishes a serializable read
snapshot; employee authorization, memberships, reporting/lifecycle state and request/type
projection therefore cannot come from different committed versions within this operation.
There is no save, write, audit entry or upgrade to a write transaction.

This deliberately differs from write validation's `BEGIN IMMEDIATE` reservation. With WAL,
concurrent writers can commit while a reader retains its snapshot. With rollback journaling,
readers may delay writer commits. The read remains short and bounded by the date range;
there is no pagination or load guarantee for very large teams. Busy/locked errors alone map
to a safe 409 refresh message, with the existing three-second provider/command timeout and
no application replay. This is a per-command timeout, not an overall request deadline.
Other database errors remain unexpected failures. Another provider would need an equivalent
consistent-read implementation; do not assume its default transaction provides this snapshot.

A read already in flight may return its earlier authorized snapshot after a concurrent role
change or deactivation. Subsequent reads recheck current persisted authorization. This does
not retroactively retract HTTP responses or require the writer reservation used by mutations.

Official references checked during implementation:

- [Microsoft.Data.Sqlite deferred transactions and isolation](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions)
- [SQLite isolation and WAL snapshots](https://www.sqlite.org/isolation.html)
- [EF Core external transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions#using-external-dbtransactions-relational-databases-only)

## Client contract

Manager-only `/team-leave` shows an employee-grouped list with Previous/This month/Next
controls, complete dates, inactive history labels, refresh and explicit retry. The selected
month remains unchanged after a failed request. Failed refreshes hide coverage data rather
than presenting it as current. The page explains Approved-only scope and links to the
existing Pending approval queue. Navigation is a month view from January 1900 through
December 9999; the API accepts other valid date ranges within its 62-day limit.

The coverage number means distinct active team members with at least one Approved request
intersecting the selected month. Each employee counts once regardless of overlapping
requests; inactive reports are excluded. It is not a daily absence total or total staffing.
Calendar days, entitlement, overlap policies and historical decisions remain unchanged.

Authenticated Axios reads consume cancellation signals. Query keys contain account ID,
login session epoch and both dates; protected page state remounts on session changes.
Entry always refetches, because another account or process can change approvals/reporting.
No previous-account placeholder data is used. This account's approval/rejection, HR employee
creation/edit/lifecycle, and leave-configuration writes invalidate its summary prefix.
These callbacks check the initiating session epoch, including same-account relogin. Other
accounts' caches cannot be invalidated here; entry, window focus and manual refresh provide
fresh reads. No new dependencies, dialogs or decision controls are added.

See [actual verification and limitations](../verification/manager-team-leave-summary.md).
