# ADR 0030: HR workforce attention aggregation

21 October 2026. Focused performance follow-up to the readiness review and #92; no dedicated matching
performance issue was found among current/open/closed issues. No issue was created or changed.
PR #130 merged at b72a5274c1d7365a350dfa9fb5c11095b94ffd28 before branch
perf/hr-workspace-attention-queries. Initial working tree was clean.

## Decision and unchanged contract

Keep GET /api/v1/reports/hr-workspace and all response fields, metric units, filters and permissions.
Current active/activated HR and initiating credentials are still validated by Application authorization
inside the existing deferred read transaction. The scoped Infrastructure reader uses the SAME DbContext
and transaction; it neither creates a context/transaction nor obtains a writer reservation. AsOfUtc is
captured once after authorization. Its SAST business date and UTC instant classify every schedule and
invitation consistently. Previously each activation lookup consulted the wall clock separately.

[IHrWorkforceAttentionReader](../../src/HRFlow.Application/Interfaces/IHrWorkforceAttentionReader.cs)
returns only workforce counts and existing department-summary DTOs. The handler retains composition,
reporting and trends; provider-specific Identity handling stays in
[HrWorkforceAttentionReader](../../src/HRFlow.Infrastructure/Services/Employees/HrWorkforceAttentionReader.cs).

Two data commands replace full employee/schedule materialization plus activation calls per active
employee: one scalar aggregate using conditional counts and indexed schedule EXISTS, then sorted
all-department summaries with correlated counts. Zero departments/empty workforces are handled without
inventing accounts. No giant IN list, full Identity entity, application cache, arbitrary cap or silent
truncation. SQL command count is bounded; database work still scales with workforce/schedule size.

## Preserved attention semantics

- Employment Active/Inactive counts remain separate from activation. Unknown employee number/start
  facts are null, not inferred; incomplete facts are not legal violations.
- Current-schedule attention requires active employment and a confirmed start on/before SAST today,
  with no revision effective on/before today. Future-only is its subset with a future revision.
  Unknown starts and future starters are not missing-current-schedule cases.
- Pending activation includes active profiles whose linked Identity account RequiresActivation,
  including Accepted, PickupReady and expired invitations. Accepted is not mailbox delivery.
- Failed-or-uncertain attention counts DeliveryFailed OR DeliveryUnknown; acknowledgement-pending
  counts PendingDelivery. Both exclude inactive/unavailable and expired safe statuses.
- Expiry is exclusive: expires <= captured asOf means Expired. Null expiry preserves the existing
  safe persisted state rather than fabricating an expiry or repaired invitation.

The old activation reader used Guid.TryParse then the native Guid-keyed Identity lookup. A deterministic
connection-local SQLite function normalizes only the employee join operand to that key; it preserves
noncanonical parsable legacy strings and returns null for malformed/unlinked values. No new EF model
function or schema is introduced. The account metadata projection contains no credentials, names or
contacts. A DISTINCT active canonical-profile join preserves the former safe-status availability check,
including unusual legacy aliases, without multiplying rows. This is deliberately not account repair.
These SQLite-specific joins/functions require implementation work for another provider, not merely
a connection-string change.

## Snapshot and write concurrency

Retained barriers pause the reader AFTER authorization and before its aggregate, with another real
connection acquiring writer protection and changing department/lifecycle/role/credential state.
Default DELETE journal permits a writer reservation/writes but delays its commit until the read ends;
explicit WAL fixtures permit commit during the read. Both return the old consistent snapshot, then
reject the revoked credential on the next request. No product journal-mode change was made. An
already-authorized in-flight snapshot is not promised instantaneous revocation. No timeout increase,
automatic callback replay or weakened permission check is introduced.

## Remaining materialization and scope

Existing department report and trend code still separately materialize every matching Pending/Approved
request in the selected inclusive period (maximum 366 days). The report calculator, overlaps, inactive
contributions, current-department attribution and request-once trend attribution are unchanged.
Actor-owned live import reviews still materialize their RowsJson before aggregate decoding. Department
summaries deliberately include all departments; the contract is not paginated. Those are separate
possible measurement slices, not silently truncated here. Leave calculations, decisions/audits, API
contracts, frontend product code, dependencies, schema and production delivery are unchanged.

[Measurements, all runs and limits](../verification/hr-workspace-query-efficiency.md).
