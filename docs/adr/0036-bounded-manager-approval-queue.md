# ADR 0036: Bounded Manager approval queue

Status: implemented for local review, 10 November 2026. No publication, issue changes or deployment.
Update, 11 November: combined queue/editor publication authorised for independent review. No merge,
issue closure or deployment; prior results and uncertainty remain in the linked verification reports.
Related existing scope: #21 (decision queue/notes), #76 (retained regressions), #91 (independent
Manager attention). Issue search found no dedicated queue-pagination issue; none was created.

## Contract and compatibility

`GET /api/v1/leave-requests` deliberately changes from an unbounded array to
`{ items, totalCount, page, pageSize, totalPages, capturedAtUtc }`. Coordinate API/client deployment;
external array consumers must migrate. Do not roll back only one side. No array fallback exists.
The HR `/monitoring/pending` array is a different, unchanged contract, not a Manager fallback.

Optional query parameters:

- `status`: defaults to Pending; only Pending (case-insensitive) is supported, preserving the existing
  Pending-only workflow. This does not turn the queue into a request directory.
- `employeeId`, `leaveTypeId`: nonempty GUIDs, intersected with authorized Pending scope. Unknown or
  inaccessible IDs match zero rows without disclosing existence. Employee IDs never grant scope.
- `start`, `end`: independently optional ISO dates. Inclusive intersection is
  `request.EndDate >= start` and `request.StartDate <= end`; reversed/malformed bounds fail 400.
- `page`: positive Int32, default 1. The server clamps it to the last matching page, minimum 1.
- `pageSize`: 1–100, default 25. Invalid values fail 400; no silent size truncation.
- `requestId`: optional nonempty GUID exact lookup for explicit dialog-availability refresh. It still
  intersects current Manager/Pending/reporting scope, independent of the visible filter/page.

Earliest StartDate first, then ID ascending. Filtering/counting/paging occur before DTO materialization.
No descriptions, evidence metadata, medical filenames/content, decision notes or personnel reasons
are added. Existing allowed row identity/type/date/status fields remain unchanged. Capture time is UTC;
date intersection operates on recorded calendar dates, not an instant conversion or new charge rule.

`GET /api/v1/leave-requests/queue-people?search=...` searches eligible Pending owners across the whole
queue, independently of its displayed page. Literal case-sensitive name substring, trimmed search,
maximum 100 supplied characters; at most 25 names/IDs ordered by name then ID. Refine the search to
reach later matches. This is not an employee directory. The UI preserves a selected option when a
new search does not return it; selection never bypasses a fresh server scope check.

## Authorization and decisions

The controller derives the actor from authentication. The existing deferred read transaction checks
credential version, then current active/activated Manager capability and current same-department
direct-report scope, matching count and page in one snapshot. Self requests and non-Pending requests
are excluded. HR-only/Employee-only users are denied. Combined roles remain additive. No writer is
reserved for queue/selector reads. EF parameterizes filter values; no giant ID list, cache, or truncation
replaces authoritative checks. The existing HR-only monitoring query no longer has a Manager branch.

Queue membership is not a decision guarantee. Existing approval/rejection commands still reserve the
SQLite writer before current authorization, Pending, balance and overlap checks, and keep status,
decision-note audit and outbox persistence atomic. Those commands/calculations were not changed.
Existing deactivation cancels Pending requests transactionally, removing them from this queue.

## Client interaction and isolation

Draft filters and applied filters are separate; Apply/Reset start page 1. Another key's rows are never
placeholder data under new labels. Errors hide rows and do not display zero. Controls remain mounted
through loading; background refresh marks the previous same-key snapshot. Returned clamps become
canonical page state after decisions empty a last page. All-date dashboard attention is a separate
full-scope count/preview and never inferred from this page or its filters.

The existing modal retains the original selected request and note outside queue rows. Background
replacement cannot silently switch it to another request. Modal filters are unavailable until the
dialog closes; filter state changes do not replace its captured request. Explicit availability refresh
looks up the original ID independently of the visible page, not `items.some` on that page. If no longer
eligible, safe feedback retains the note and disables confirmation. A network failure is not proof of
ineligibility. Decisions remain server-confirmed, duplicate-click protected and non-replayable.

Queue keys contain account, session generation, applied filters and page. Selector keys contain account,
session and search. Reads consume abort signals; late queue/selector completions check generation.
The workspace remounts on replacement login, including same account, not ordinary token refresh.
Owned availability reads abort on unmount. Decision invalidation targets the initiating session's queue,
selector and existing attention/team namespaces. Employee changes also invalidate the selector.
No cross-account instantaneous refresh is promised; entry/explicit refresh observes other actors.

## Caller inventory and limits

Tracked source/tests/scripts/tools were searched, including ignored-by-default tracked tooling.
The SPA `usePendingLeaveRequests`/Manager page are the only production response consumers and migrate
together. HR monitoring keeps its separate array hook. Existing browser and pitch tools navigate the
UI, not the queue array; fresh pitch fixtures contain one request. New HTTP tests consume the envelope.
The measurement runner accepts the historical array solely to compare unchanged baseline binaries,
not as a production compatibility layer. Pitch/real-scanner drills were inspected, not rerun here.

No schema/dependency/CI or permission/calculation change. Count still scans matching requests; offset
pages can shift between separate snapshots. HR monitoring, balance and report reads remain separately
unbounded. This is not a production-capacity or accessibility-compliance claim. See
[actual separate executions and measurements](../verification/manager-approval-queue.md).
