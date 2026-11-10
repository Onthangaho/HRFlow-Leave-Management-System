# ADR 0035: Bounded personal leave history

Status: prepared for independent PR review, 10 November 2026; not merged or deployed.
Related existing scope: #18/#17 (history/cancellation), #76 (regression gates), #83/#84
(authorised evidence detail), #90 (personal workspace). Issue search found no dedicated pagination
contract. No issues were created, edited or closed.

## Contract decision

This is a deliberate **response-contract update** to `GET /api/v1/leave-requests/history`:
`{ items, totalCount, page, pageSize, totalPages, capturedAtUtc }` replaces the old unbounded array.
The response-consuming SPA hook is `useEmployeeLeaveHistory`/My Leave; both are migrated together. Request
submission/recovery only invalidates its existing account prefix or navigates to the route; it does
not consume the array. Browser history links and per-request timeline/document APIs are unchanged.
No compatibility fallback preserves an unbounded default. External array consumers must migrate;
coordinate API/client rollout rather than mixing these response versions.

Optional query fields:

- `status`: exact `Pending`, `Approved`, `Rejected`, `Cancelled`; absent means all. Numeric/unknown
  names fail with 400. Empty query fields model-bind as absent.
- `leaveTypeId`: nonempty GUID. Missing/inaccessible/nonexistent type IDs simply match no owned
  records, without an existence lookup or another owner's data. Malformed/empty GUIDs fail 400.
- `start`, `end`: ISO `YYYY-MM-DD`, independently optional. Inclusive **intersection**:
  `request.EndDate >= start` and `request.StartDate <= end`. Reversed/malformed dates fail 400.
  These are recorded leave dates, not UTC audit timestamps or a new business-date calculation.
- `page`: positive Int32, default 1. Values exceeding Int32 fail model binding with 400.
- `pageSize`: 1–100, default 25. Invalid values fail 400; no silent truncation of a requested size.

Order is StartDate descending, then ID descending. Count is taken after filtering. The returned page
is clamped to the last matching page (minimum page/totalPages 1, including empty results). Offset
arithmetic uses that bounded page; it cannot overflow on a huge valid requested page. Each selected
request retains its entire decision history in Timestamp/ID ascending order, notes and UTC timestamp
semantics. No balance calculation uses this page. No description/evidence/medical fields are added.

## Protection and query shape

The controller supplies only the authenticated Identity actor, never a query-selected employee.
The existing deferred SQLite read transaction first checks current credentials. Inside that same
snapshot the handler resolves current active/activated Employee-or-Manager access, captures UTC time,
filters/counts, orders/pages, and projects only selected requests and their audits. HR-only is denied;
combined personal roles remain additive. Reporting does not reserve a writer or make business writes.

The real handler's six SELECTs are bounded in number independently of history size: credential proof,
owner, lifecycle, current roles, filtered count, page-plus-audit projection. Count/projection share the
transaction. The retained interceptor test excludes the separate HTTP bearer gate and counts only
this handler path. No cache weakens current server authorization. No index/schema/dependency change.

## Interaction and state

My Leave separates editable draft filters from applied filters; Apply/Reset start at page 1. Page
controls and result labels use the returned page/count. Changing query keys does not keep another
page's rows as placeholder data. Background refresh labels the retained same-key snapshot; errors
hide rows and expose retry rather than zero totals. Cancellation confirmation state lives at page
level so a background-error/retry cycle does not discard it; explicit filtering/paging resets that
intent. A terminal row cannot offer confirmation. Server page clamping handles a now-empty final
page after a filtered cancellation. Balances are invalidated/reloaded independently from full history.

History keys include account, session generation, applied filters and page. Requests consume query
abort signals; callbacks reject superseded generations. The workspace resets on account/session
replacement, never on ordinary token refresh. Cancellation remains explicitly non-replayable and
has no retry. Existing submission/recovery, timeline and supplemental-evidence links remain working.
The type selector uses the existing complete balance type list, not types on the displayed page.

## Boundaries

No request/approval rules, immutable records, statutory units, evidence requirements or recovery
retention rules change. This does not paginate Manager queue/HR monitoring, balances or per-request
history and is not a production load claim. See [executions and measurements](../verification/personal-leave-history.md).

## Finalisation caller inventory

Tracked-source searches cover the endpoint literal, query type, history hook and route across source,
tests, scripts and tools (including files ignored by ordinary `rg`). No remaining production array
consumer was found. New HTTP tests consume `items`; the measurement runner alone accepts the historical
array to measure the unchanged baseline. Existing browser workflows consume the migrated UI.
`tools/pitch/rehearse.mjs` navigates My Leave and the per-request timeline; it never decodes the history
API array. Its fresh synthetic owner has one request, within the first page.
`tests/browser/evidence.real-scanner.ts` likewise navigates the UI and targets its fresh fixture request.
Issue-creation shell scripts contain historical contract prose, not HTTP history clients; they were
not executed or rewritten. Pitch/real-scanner drills were inspected, not newly executed in this slice.
External clients remain outside this inventory: deploy API/client together and migrate their array
consumers before rollout. Rolling back just one side is unsupported.
