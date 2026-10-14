# ADR 0019: Current-data personal leave dashboard

14 October 2026. Issue #90 current-data slice, not statutory/cycle completion. PR #112 was
confirmed merged; clean base c96111f21690cb0f8263e94ae8d22c97c917db71.

## API and current-model contract

GET /api/v1/leave-requests/dashboard has no employee/date/filter input. The controller derives the
Identity actor from authentication and delegates to a MediatR Application handler. Current active,
activated Employee OR Manager capability is required; HR-only is denied. Combined roles are additive.
An arbitrary employeeId query cannot change owner scope. DTOs contain no employee enumeration,
descriptions, decision notes, evidence metadata, deactivation reasons or correlation identifiers.

Authorization, current policies/types and owner requests share the existing deferred read snapshot
(including credential-generation validation). It does not reserve a SQLite writer. AsOfUtc is captured
once inside that snapshot after authorization; it is projection time, not an assertion of live data
throughout rendering. Already-authorized snapshots retain the existing valid serial-read semantics.
No writes, state transitions, audit changes, migrations, policies or dependencies are introduced.

Response fields:
- asOfUtc: explicit UTC server timestamp.
- basis: timeZone, unit, balanceBasis, today, upcomingStart, upcomingEnd, inclusive and three list limits.
- balances: all CURRENT types, ordered name then ID, using existing LeaveBalanceCalculator over all
  owner Approved history per type; entitledDays/usedDays/remainingDays retain the existing DTO.
- pendingCount: ALL Pending requests, any dates. pending: up to 5, start ascending then ID ascending.
- upcoming: up to 10 Approved requests intersecting [today,today+90 days], endpoints inclusive,
  ordered complete start date then ID ascending. Includes ongoing leave ending today and starts on
  day 90; excludes leave ending yesterday or starting day 91. The window includes 91 calendar dates.
- recent: up to 10 REQUESTS, not audit events, ordered newest maximum recorded audit timestamp or
  known ProcessedOn fallback, then request ID descending. Unknown timestamps sort last. No invented
  submission time; links lead to the existing authorized full timeline.

Request summaries: id, leaveTypeName, DateOnly startDate/endDate, status, nullable lastActivityUtc.
Full spans are shown, not clipped in these lists. Audit/activity timestamps serialize with UTC Z.
Configured entitlement minus all Approved inclusive calendar request-days is NOT an annual allowance,
accrual, payment eligibility or statutory-compliance statement. Pending reserves nothing. Approved
future leave also consumes balance under the current model. Overlaps count separately in summed
request-days; no unique absence, people-days or coverage total is presented.

## South African date basis

SAST is UTC+02:00: [NMISA official annual report, Time standard](https://www.thedtic.gov.za/wp-content/uploads/NMISA-AR-2024-25.pdf),
checked 14 October 2026. Convert one UTC TimeProvider instant to that fixed current SAST offset for
business today, independent of server/browser timezone. This is the single-organisation SAST basis,
not historical time-zone reconstruction or a configurable worldwide calendar. Dates remain calendar
facts, displayed without browser-offset shifts. Activity and as-of times display explicitly in UTC.

## UI, refresh and isolation

Personal Overview has primary Request Leave, balance cards, bounded Pending/ongoing/upcoming/recent
lists, history/timeline navigation and a link to the existing recipient-scoped inbox. Cancellation stays
in the existing history confirmation workflow. Team and HR quick actions remain working links, not
new manager/HR dashboards. HR-only does not mount the personal query. No charts/dependencies added.

Query key: personal-leave-dashboard/account Identity/session epoch. Axios supplies cancellation signals;
no previous-query placeholder data. Entry and focus refresh; visible-page interval 60 seconds, stops
while hidden/unmounted/signed out. A 403 hides cached personal data and stops periodic polling; explicit
retry/sign-in refreshes permissions. Other recoverable refresh errors label old results as stale, and
missing data never becomes zero. Logout/replacement/same-account relogin cannot expose delayed old
results. Mutations remain non-replayable with initiating-session guards.

Submit/cancel/approve/reject success invalidates only this initiating account/session dashboard.
A manager's decision does NOT directly invalidate another account's cache. The owner's entry/focus,
visible polling or explicit Refresh observes decisions elsewhere. No cross-tab instant-update claim.

## Deferred and operational implications

#90 stays open for #85 reviewed annual cycles and #22/#75 shared qualified calculation contracts.
No holidays/accrual/working-day charging or inferred legacy employment facts. #91/#92 remain separate.
All approved history must be read for the current calculator; balances include all configured types,
while request lists are bounded. No new large-history load guarantee is claimed. Existing SQLite
single-host operational/read-contention limitations remain. There is no new migration/rollback data
loss; rollback of this source change removes the dashboard endpoint/UI only.
