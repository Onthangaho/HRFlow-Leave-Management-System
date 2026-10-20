# ADR 0027: bounded HR directory and independent Manager lookup

Date: 20 October 2026. Implemented locally for review; no tracker change or #77 completion claim.

## Context and decision

The [readiness review](../planning/08-current-readiness-review.md) identified unbounded employee and
schedule materialization, activation/role lookups per row and authenticated thumbnail fan-out.
PR #122 is merged. Searching all existing issue titles found no dedicated pagination issue; #76
covers retained regression work, while #77 is the application shell. No issue was created/edited.

Keep Controllers thin: MediatR handlers run current HR authorization and the Infrastructure reader
inside the existing deferred read transaction. Its credential check establishes the same SQLite
snapshot as current-role authorization, count and page. No writer reservation, SaveChanges or new
schema/index is needed. A snapshot authorised before a concurrent permission change may finish;
subsequent requests recheck current permissions, as in existing reporting.

## API contract (collection response change)

`GET /api/v1/employees` now returns `{ items, totalCount, page, pageSize, asOfUtc }`, **not an array**.
There is no unbounded compatibility mode. Defaults: page 1, pageSize 25; maximum pageSize 100.
Page/size must be positive integers, and their offset must fit Int32. Out-of-range pages return
200 with an empty items list and the filtered count. Invalid values return useful 400 problems;
current HR/credential denial remains 401/403. Actor identity is always server-derived.

Optional query filters, combined with AND before counting/paging:

- `search`: trim outer whitespace, at most 100 characters, literal substring in canonical name,
  email or employee number. ASCII case-insensitive using SQLite lower-case semantics; non-ASCII
  characters retain case, with no accent folding/transliteration. `%` and `_` are literal text,
  not wildcard syntax. Department/role/Manager name search is replaced by explicit filters/search.
- `status`: `All` (default), `Active`, `Inactive`, referring to employment lifecycle independently
  of activation. `departmentId`: an existing department GUID (unknown IDs return 400).
- `reportsTo`: current direct-report Manager employee GUID, used by the deactivation safeguard
  link. No arbitrary acting Manager is accepted. Unmatched IDs yield empty results.
- `attention`: `all`, `employment`, `schedule`, `future-schedule`, `activation`, `delivery-failed`,
  `delivery-pending`. Definitions match the HR workspace: active Unknown number/start; active
  confirmed starters on/before SAST today without an effective schedule; its future-only subset;
  active employment with linked Pending activation; and nonexpired Pending activation with the
  corresponding delivery state. Expired invitations do not count as delivery-failed/pending.
  An absent expiry does not invent an expiration. Unknown inputs are not legal violations.
- `sort`: `name` (default), `email`, `number`, all ascending. Name/email use ASCII-case-insensitive
  ordering. Numbers are canonical, with nulls last. Employee ID is always the deterministic tie-breaker.

One captured UTC as-of instant supplies invitation expiry, SAST business date and response metadata.
`GET /employees/{id}` retains the complete single-employee edit DTO, independently of list filters.
Both forms of the HR-only DTO gain nullable `profileImageVersion`, only for Current rows with
`Size > 0`. Empty Current rows preserve own-account concurrency versions but supply no directory/detail
hint; no image blob, public URL or private path is returned. Existing lifecycle reasons remain HR-only;
no public selector changes. See the [removed-image correction verification](../verification/removed-profile-image-hints.md).

`GET /employees/managers?departmentId=...&employeeId=...&search=...&page=1` is HR-only, with fixed
25-row pages. Returns `{ items: [{ id, fullName }], totalCount, page, pageSize, assignedManager }`.
Search semantics match the directory. Choices are current active, activated same-department Identity
Manager members, excluding the employee being edited. The optional employee ID identifies the edit
target, not the actor. Its actual assigned Manager is returned separately, even if no longer eligible
or matching search, so preservation is explicit. The loaded employee DTO also retains Manager ID/name.
Assignment cycles, latest role/department eligibility and versions remain authoritative protected
write checks; a lookup is not permission to commit a stale assignment.

## Query and client design

Only the selected page is projected; roles are batch-loaded for its Identity IDs. Effective-schedule
and image-presence checks use SQL predicates/correlated projections, not all-history lists. The
reader's default nonempty page uses three data commands (count, page, roles), excluding the existing
authorization/credential gate and transaction commands; an explicit department adds a validity read.
Count/search still scan matching data, and fixed command count does not mean constant CPU/load.

SQLite GUID Identity keys are stored as text. An allowlisted unmapped SQL account projection reads
the key as text without CAST/function-wrapping the indexed Identity side; NOCASE comparison handles
existing GUID casing. It selects activation metadata only, never credentials. This is an explicit
SQLite-specific read implementation, not connection-string-only provider portability.

Directory and Manager query keys include account, session generation and all applied filters/page.
No previous-page placeholder data is shown under new filters. Draft filters apply explicitly and
reset page to 1. Refresh labels previously loaded results; errors hide rows, and 403 hides protected
forms/dialogs too. Empty last pages clamp to the last valid page after refreshed mutation results.
Edit snapshots live outside list query results; background refresh never rebases their versions.
Explicit stale reload fetches the detail endpoint and preserves existing confirmation behaviour.

Create/edit, deactivation reload, resend, schedules and CSV onboarding now invalidate the initiating
session's directory namespace. No cache write assumes the old collection-array shape. Manager
search/page changes retain an explicitly selected choice; department/operation changes clear that
new choice, while existing reporting stays Preserve unless deliberately Assign/Clear. Open form
fields survive list paging/refresh. Non-replayable writes and server transactions are unchanged.

Directory avatars require a current image-version hint and viewport entry (100px prefetch margin).
Browsers without IntersectionObserver fall back to page-mounted loading, still at most one page.
Images are authenticated Blobs with account/session/employee/image-version keys, consumed cancellation
signals, no retained cache after the last observer, URL revocation and initials on failure. StrictMode
may still start/cancel duplicate requests for a visible hinted row; no global one-request claim.
Header/Profile still use their own authenticated image query and never fetch the HR directory.

## Consequences and limits

All collection callers migrate together; external consumers must adopt the envelope. No migrations,
dependencies, leave charging, roles, reporting writes or audit semantics change. Reverting the API
and UI requires reverting their contracts together; there is no data rollback. Department choices,
HR workspace aggregate inputs, individual schedule history and bounded-date report materialization
remain separate unbounded-read concerns. See [measurements and verification](../verification/bounded-employee-directory.md).
