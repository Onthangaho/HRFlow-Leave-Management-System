# ADR 0037: Bounded read-only HR Pending monitoring

Status: implemented for local verification, 11 November 2026, from merged PR #142
(`0f18f7cde195d38feeb86080fa6ac0a7aff62c2b`). Publication authorised for independent review; no merge, tracker edit or deployment.
This extends current-data HR monitoring; it does not complete calculation-dependent #92 or #76.

## Contract and compatibility

`GET /api/v1/leave-requests/monitoring/pending` changes from an unbounded array to
`{ items, totalCount, page, pageSize, totalPages, capturedAtUtc }`. API/client deployment and rollback
must be coordinated. External array consumers must migrate; there is no unbounded compatibility mode.
Manager queues and personal history retain their own existing envelopes and authorization.
Already-open old clients need an explicit refresh as part of that rollout; no automatic form reload is added.

- `search`: optional employee canonical name/email literal substring, at most 100 UTF-16 characters
  before trimming. Blank means no filter. SQLite LIKE is ASCII case-insensitive; non-ASCII case
  folding is not promised. Percent, underscore and backslash are escaped and match literally.
- `departmentId`, `leaveTypeId`: optional nonempty GUIDs. Unknown IDs match no rows. Department is
  the employee's **current** department, not reconstructed historical membership.
- `start`, `end`: optional ISO calendar dates, intersecting inclusively:
  `request.EndDate >= start` and `request.StartDate <= end`. Either bound can stand alone.
  Invalid dates or reversed bounds fail 400. These are recorded calendar dates, not charged units.
- `page`: positive Int32, default 1; excessive positive pages clamp to the last matching page.
- `pageSize`: 1–100, default 25. Invalid bounds fail 400 rather than truncating. Empty results have
  page 1, totalPages 1 and an empty items collection. No offset overflow for extreme valid pages.
- Ordering remains earliest request start, then request ID. Only Pending requests are listed.

`capturedAtUtc` is server UTC, captured inside the read operation after authorization. Count and rows
share that operation's deferred SQLite snapshot; separate requests may observe later transitions.
Successful responses are no-store. Logs contain aggregate counts/correlation metadata, not search terms.

## Authorization, privacy and persistence

Authentication supplies the Identity actor. The existing deferred read transaction rechecks its
credential version and current account access; Application authorization checks active/activated
status and current Identity HR membership **inside that same snapshot**, before count/page reads.
The transaction is deferred, not a SQLite writer reservation. No new Identity joins or caches.

Filters precede count, stable ordering, Skip/Take and DTO materialization. The retained SQL interceptor
confirms six SELECTs at 20 and 2,000 rows in one transaction; the extra filtered count raises the old
five-SELECT projection to six. There are no per-row queries, silent truncation or large IN lists.

Rows reuse the existing minimal Pending DTO: request/employee/type IDs, canonical name/email,
type label, dates and status. No descriptions, decision notes, medical filenames/content, private
phones, evidence metadata or deactivation reasons are added. History links continue to perform current
server authorization and document privacy checks. HR cannot decide, edit requests or bulk act.
Manager decisions, immutable requests/snapshots/audits/outbox and calendar-day calculations are unchanged.

## UI and session behaviour

The existing HR route now uses Apply/Reset filters, matching counts, page controls and server capture
time. Drafts do not label old results. Different-key queries have no previous-page placeholder data;
ordinary same-key refresh marks its older snapshot, while failure/permission denial hides protected
rows. Loading/empty/error/retry and filter-choice errors are explicit; missing data is not zero.
Unavailable last pages clamp after a transition; controls remain mounted through refetch.

Keys contain account, session generation and all applied filters/page values. Reads consume AbortSignal
and check generation on completion. Replacement login, including the same account, remounts filters;
there is no unscoped persistence. Reads do not replay business mutations. Approval/rejection/cancellation
invalidate the initiating session's monitoring namespace; employee/configuration/submission invalidation
prefixes still cover every filter/page. Other accounts' decisions appear on entry or explicit refresh,
not instant cross-account invalidation. The separate HR workspace counts/preview are not page-derived.

Shared theme/form/card styles, semantic controls, keyboard buttons and reduced-motion loading are used.
The responsive list retains safe history links; no new calendar, chart or design dependency.

## Consumer inventory and limits

The SPA monitoring hook/page are the only production response consumers and migrate together. The
existing real-HTTP workflow test now explicitly filters its intended owner/date rather than assuming
the request appears in page 1. New tests use the envelope. Pitch tooling navigates the UI on its fresh
single-request store and does not parse the old array; it was inspected, not rehearsed again here.
Dashboard/report/Manager/personal projections remain independent. Historical ADR 0036's unchanged-array
statement describes its earlier phase; this ADR supersedes it for HR monitoring.

No schema/index/dependency or retention change is needed. Data survives upgrade/downgrade; incompatible
client/API contract pairing is the rollback risk. Count/sort/search may still scan many matching rows;
offset pages can shift across snapshots. Type/department catalogues and report/trend/full-history reads
elsewhere are not bounded by this change. Payload reduction is not a production-capacity claim.
The page uses two existing catalogue GETs for filter choices; endpoint SQL measurements exclude those
and the header/notification queries. End-to-end navigation latency was not benchmarked.

[Actual executions, measurements, screenshots and limitations](../verification/hr-pending-monitoring.md).
