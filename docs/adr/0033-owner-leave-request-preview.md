# 0033: Explicit, non-binding owner leave preview

26 October 2026. Locally implemented for review on `feat/leave-request-preview` from
`c9780f87767b28dfe287243cb37f1cbdd0c4e235` (merged PR #137). Publication authorised for independent PR review on 27 October 2026; no deployment.

## Decision and contract

`POST /api/v1/leave-requests/preview` is read-only despite POST. JSON inputs are `leaveTypeId`
(nonempty GUID), `startDate` and `endDate` (ISO `yyyy-MM-dd`, bounded .NET DateOnly range).
The body is capped at 2,048 bytes. No employee identity, descriptions, document IDs or file bytes
are needed. An overposted identity is ignored; authentication alone supplies the actor.

Current credential validation, active/activated Employee-or-Manager membership, owner resolution,
manager relationship, type/policy and Approved history reads share the existing single deferred
SQLite snapshot, without a writer reservation. HR-only is denied; combined roles are additive.
No other employee information, overlapping request IDs, medical metadata or notes are returned.

Response fields:

- `capturedAtUtc`, echoed `leaveTypeId`, `startDate`, `endDate`, `unit`, `submissionDateBasis`.
- `requestedDays`, `entitledDays`, `approvedUsedDays`, `remainingDays`,
  `projectedRemainingDaysIfApproved`. Projection is conditional arithmetic, can be negative, and is
  neither approval eligibility nor a reservation. Reversed dates have null duration/projection.
- `requirements`: current `typeVersion`, `policyId`, `policyVersion`, `descriptionMode`,
  `evidenceMode`, `evidenceClass`, plain-text `instructions`. Field/file compliance is not evaluated.
- `findings`: owner-safe `code`/`message` pairs: `StartDate`, `EndDate`, `ManagerAssignment`,
  `InsufficientBalance`, `ApprovedOverlap`. A 200 assessment can contain blocking findings.

Malformed/out-of-range/missing inputs return safe 4xx; unavailable type returns 409. Authorization
remains 401/403, not a correctable finding. Infrastructure/contention errors remain problem responses,
not successful empty assessments or fake zero balances. Responses use `Cache-Control: no-store`.
No bodies, employee details or private draft contents are logged by this endpoint.

## Shared rules and historical truth

The existing `LeaveBalanceCalculator` is authoritative. Domain `LeaveRequest.PolicyProblems` is
shared by preview and submission/approval enforcement; preview never constructs an aggregate or
even an in-memory Submit audit. Shared `CalendarDays` retains inclusive days. Application
`LeaveSubmissionDateRules` shares submission date checks. Approved usage covers all Approved
history of the type under current configured entitlement. Pending neither reserves balance nor
blocks overlap. When prohibited, overlaps are inclusive same-type Approved intersections only.
When overlap is allowed, Approved request durations are still summed; they are not distinct absence days.

Weekends/leap days count. There are no annual cycles, accrual, holidays, working-day deductions,
unpaid or pay-eligibility changes. The existing submission lower bound is the **UTC calendar date**,
explicitly displayed/returned. Near SAST midnight it can differ from dashboard business dates;
this feature does not silently change that pre-existing rule.

Preview never scans/binds/reserves evidence or assesses private descriptions. Submit remains
non-replayable and rechecks current credentials, relationship, type/policy versions, required content,
balance, overlap and document metadata under writer protection, following existing byte verification
outside it. Approval still revalidates current balance and overlap. Existing immutable snapshots,
transition audits and Medical privacy remain unchanged; Required Medical remains unsupported.

## Interaction and session boundaries

Preview is explicit and optional. Loading, error/retry, result and outdated states are inside the
existing form. Results echo captured response dates, never live replacement input labels. Relevant
input changes hide previous figures. Account ID, session generation, type ID, reviewed versions and
dates identify results; cancellation, attempt sequence and callback guards reject superseded work.
Unmount/input/session changes abort transport. No persisted browser or cross-session preview cache.
As a read-only POST, preview is eligible for the existing bounded refresh/retry; submission remains
explicitly non-replayable. The retry retains the initiating cancellation signal and session guards.
Explicit result/error completion receives focus in a named group; shared light/dark styles and
status/alert states apply. Successful explicit requirements review focuses its retained-form notice,
because the triggering conflict/preview control can disappear.

Receiving current versions does not replace reviewed submission versions. A distinct explicit
refresh/review action can load requirements while retaining dates, description and selected drafts;
its callback also checks session and selected type. Existing submit-conflict recovery shares this
action. Description/document edits are not declared evaluated by a date/balance preview.

## Scope and consequences

No schema/migration/dependency/listening/CORS changes or backfill/downgrade data loss. Preview can
be stale immediately and guarantees neither submission nor approval. The owner/type's Approved
history is still materialized for the existing calculator; no aggregate performance claim.

Issue search found related closed [#15](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/15)
and [#12](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/12), and open
[#84](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/84), but no dedicated preview
issue. No issue edits/closures are implied. Qualified #75/#22 and cycle/category decisions remain
outstanding. [Execution evidence and limits](../verification/leave-request-preview.md).
