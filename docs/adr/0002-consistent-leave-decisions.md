# Consistent leave decisions on SQLite

Approval must validate current policy and approved history, because Pending requests do not reserve
balance. Submission checks alone cannot prevent two pending requests spending the same entitlement.

## Decision

Approve, reject, and cancel run through `ILeaveDecisionTransaction`. Its shared Infrastructure
implementation, `SqliteWriteTransaction`,
explicitly calls `SqliteConnection.BeginTransaction(deferred: false)` before decision reads and enlists
the scoped EF Core context using `UseTransactionAsync`. In Microsoft.Data.Sqlite 8.0.23, this begins
`BEGIN IMMEDIATE`, reserving SQLite's single writer. A second decision, even in another API process,
must wait before it can read the request, manager relationship, policy, and approved history.

The wrapper detaches any unchanged entities loaded by the controller before the lock. It refuses
units of work with unsaved changes or an existing transaction rather than discarding unrelated work.
Approval reuses `LeaveBalanceCalculator` and `LeaveRequest.ValidateAgainstPolicy`. The domain remains
responsible for the transition and its existing audit entry. One transaction saves and commits both;
failure rolls back and clears tracked state. Historical approvals are never rewritten.

Permissions are checked before returning a status conflict. Missing resources remain 404 and denied
permissions remain 403. A request that is no longer Pending returns 409. Policy failures retain the
existing 400 ProblemDetails with the specific balance or overlap explanation. Only SQLite BUSY (5)
and LOCKED (6), including those wrapped in `DbUpdateException`, map to a safe contention 409. Other
database failures retain the unexpected-error path. Existing structured logging and correlation
headers remain in use.

## Contention and limitations

The connection's default timeout and EF command timeout are temporarily set to three seconds.
Microsoft.Data.Sqlite automatically waits/retries lock acquisition and busy commands up to that
timeout. After acquiring the writer reservation, command-level waiting cannot allow another writer
to change the validated state. There is no application-level replay of the decision callback. A timed-out operation is
rolled back; a new HTTP request loads and validates fresh state. This avoids reusing a tracked status
or an unsaved audit entry. The timeout is per database command, not a total request deadline; SQLite
calls are synchronous and a cancellation cannot interrupt every native lock wait.

The writer reservation covers the whole database, including different employees and unrelated
writes. This is deliberately simple for the current single-organization SQLite application, but
limits write throughput. Employee management uses the same writer reservation so reporting changes
serialize with decisions. All API instances must run this decision safeguard and share the same
database file on a filesystem that supports SQLite locking. Direct SQL writers, older API binaries,
and future transition paths must not bypass the validation protocol. A future provider change must
replace this implementation with equivalent protection of the full read/validate/write operation;
a request-row concurrency token alone does not protect different requests sharing a balance.

No schema migration or stored balance is required. Calendar-day counting, inclusive endpoints,
same-type overlap rules, policy overlap permission, and zero balance use by Pending/Rejected/Cancelled
requests are unchanged.

## Official references

- [Microsoft.Data.Sqlite transactions](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/transactions)
- [Microsoft.Data.Sqlite 8.0.23 transaction source](https://github.com/dotnet/efcore/blob/v8.0.23/src/Microsoft.Data.Sqlite.Core/SqliteTransaction.cs)
- [EF Core external transactions](https://learn.microsoft.com/en-us/ef/core/saving/transactions#using-external-dbtransactions-relational-databases-only)
- [SQLite busy errors and bounded provider retries](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/database-errors)

## Verification

Temporary tooling outside the repository starts two API processes sharing a newly created disposable
database. Requests go through real login, submit, approve, reject, and cancel endpoints. Independent
DbContexts inspect persisted statuses, approved calendar days, and matching audit rows after each
scenario. The verification does not introduce automated test files or modify existing databases.

On 6 October 2026, all requested A-G scenarios passed, with each concurrent scenario repeated five
times across the two processes. Balance competition persisted only 15 of 20 entitled days; prohibited
overlaps persisted only one approval. Each same-request race persisted exactly one matching terminal
audit. Valid concurrent approvals both persisted. HR-only, unassigned-manager, and self-approval calls
returned 403; a missing request returned 404. Forced lock contention returned 409 in approximately
3.1 seconds and left the request Pending without a terminal audit; a fresh request then succeeded.

Additional checks passed for policy changes, reporting changes, and role revocation after submission
(including an existing JWT), inclusive overlap endpoints, overlap across different types, permitted
overlap, and zero balance use by rejected/cancelled requests. An injected non-contention audit-insert
failure followed the existing safe 500 path and rolled back both status and audit; it was not
misclassified as a retryable conflict. No injected trigger remains after verification.
