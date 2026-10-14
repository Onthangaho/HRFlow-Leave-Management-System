# ADR 0020: Manager workspace and read-only team calendar

14 October 2026. Issue #91; PR #113 confirmed merged before branching from fetched main
42cd7152bdc213bcbe2c5db95433f74e21260c28. No schema or dependency change.

## Scope and API

Overview composes the existing personal dashboard with Manager decision attention, ongoing/upcoming
Approved team leave and a read-only calendar/list. The existing /team-leave route shares that component.
Employee-only/HR-only accounts do not mount Manager queries; combined roles retain the union of menus.
No calendar decision controls. HR still cannot approve/reject; decisions stay in the protected queue.

GET /api/v1/leave-requests/manager-attention accepts no actor/employee/filter input. The controller
supplies the authenticated Identity actor to an Application query. Response: asOfUtc (server UTC),
pendingCount (ALL-date Pending request count), previewLimit (5), pending (requestId, employeeName,
leaveTypeName, DateOnly startDate/endDate). Preview order: start date then request ID ascending.
The count means requests eligible for the current reporting queue, not guaranteed approval or days.
No emails, medical metadata/content, descriptions, decision notes or personnel reasons.

Authorization and count/preview share the existing deferred read transaction, including current
credential generation, active/activated account and actual Identity Manager membership. No writer
reservation, persistence or audit changes. Current same-department direct reports only; self excluded.
The existing team handler now explicitly uses CurrentAccountAuthorization for the same active,
activated and current-membership check inside its existing snapshot.

GET /api/v1/leave-requests/team-summary remains unchanged: Approved requests intersecting supplied
start/end inclusive, at most 62 calendar dates; full request spans; start, employee name, ID order.
Inactive reports' historical approvals remain with isActive=false. Reassignment changes current
visibility, without rewriting requests/audits. Missing/reversed/over-limit dates return existing 400;
non-Manager/revoked permissions 403, inactive/pending/stale credentials denied by existing gates.
Expected read contention remains safe 409, three-second command timeout, no callback replay.

Each panel is an independent authorized snapshot, not a claim of one cross-endpoint transaction.
Attention has server as-of UTC. Existing team collection has no server as-of envelope: UI explicitly
labels its UTC CLIENT RECEIPT time, not an invented database cutoff. Audit timestamps stay UTC.
See [ADR 0006](0006-manager-team-leave-summary.md) and [ADR 0019](0019-personal-leave-dashboard.md).

## Dates and exact metric meanings

Calendar default/Today use current browser instant converted to SAST UTC+02:00, independent of
browser local timezone. Browser clock accuracy still matters; this is a documented limitation, not
a claim of synchronized server time. Personal balances/dashboard retain their server SAST basis.
Date facts and labels use UTC midnight arithmetic so browser daylight-saving changes do not shift days.

Upcoming team preview intersects [SAST today,today+61], 62 dates inclusive, shows up to ten complete
request spans, earliest start first. It is request preview, not unique absence or staffing capacity.
Monday-first month cells include adjacent dates through Sunday; 28-42 dates, always within 62.
Month navigation January 1900 through December 9998 keeps adjacent dates inside DateOnly bounds.
The month/range heading matches its query key; no previous-range placeholder data relabelled as new.

Daily count is distinct ACTIVE employee IDs with at least one Approved request covering the date,
unioning same/cross-type overlaps. Inactive records are labelled/listed and never count as active
absence. No working schedules, public holidays, capacity, accrual or statutory interpretation added.
Full request spans remain visible in the accessible list and selected-date details.

## Client/session and refresh

Keys: manager-attention/account/session; team-leave-summary/account/session/start/end. Authenticated
Axios consumes cancellation signals; no automatic mutation replay introduced. Session replacement
remounts Manager local month/date state; ordinary navigation/token refresh does not reset active forms.
Entry/focus and visible-page polling at 60 seconds or explicit Refresh observe other users' changes.
Visibility listener stops/resumes polling; permission denial stops periodic retries. Errors hide
team/count data rather than showing zero; refreshing previous results is announced as unconfirmed.

This session's approve/reject/cancel success invalidates attention; approve/reject also invalidate
current-session team keys. HR employee edits/creation/deactivation invalidate their initiating
session's attention alongside existing reference/team caches. These callbacks retain existing epoch
checks. No cross-account invalidation or instant remote-tab update is claimed.

Desktop/tablet calendar uses ordinary keyboard-focusable date buttons with full date/absence labels;
selected date uses aria-pressed. Date input and whole-range list are available to all, with mobile
list-first presentation. No new calendar dependency, dialogs, animations or approval logic.

## Preservation and limits

No migration, account/role changes, audit mechanism or leave charging change. Existing inclusive
calendar-day balances and decision writer protection remain. Source rollback removes these views
and endpoint; it does not discard stored data. Large teams/history still need separate load review.
#92 and statutory/cycle work #85/#22/#75 remain separate; automated-test convention #76 unchanged.
See [executed evidence and limitations](../verification/manager-workspace-calendar.md).
