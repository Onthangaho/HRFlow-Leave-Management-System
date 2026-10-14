# ADR 0021: Current-data HR workspace

14 October 2026. #92 current operational slice. PR #114 confirmed merged before clean branch
feat/hr-workspace from fetched origin/main 5a87f31115935051c77597e829058d3517a9f454.
#85/#22/#75 calculation/legal dependencies and #76 deferred-test convention remain separate.

## Read contract and authorization

GET /api/v1/reports/hr-workspace?start=YYYY-MM-DD&end=YYYY-MM-DD&departmentId=optional-guid.
The controller derives Identity actor from authentication; no client employee/actor scope is accepted.
Current active/activated HR and credential generation are checked in the same existing deferred
SQLite read snapshot as every projection read. No writer reservation, callback replay, state change
or audit write. Employee/Manager-only denied; combined HR roles retain their independent capabilities.
HR gains no approval/rejection authority. Separate personal/Manager panels remain separate snapshots.

Missing/malformed/reversed dates or more than 366 inclusive dates: 400. Unknown department: 404
for authorized HR. Revoked HR: 403; inactive/pending/stale credentials use existing access denial.
Existing bounded busy/locked reporting behaviour remains safe 409 and three-second command timeout.

Response: server asOfUtc and businessToday, dateBasis (SAST UTC+02:00), dayUnit (summed Approved
inclusive calendar request-days), activeEmployees, inactiveEmployees, departmentCount,
organisationDepartments (ID/name/active/inactive counts), pendingCount, pendingPreviewLimit (5),
pending (request ID, canonical employee name, leave type, dates), attention, report and trends.
No email, private phone, descriptions, notes, medical metadata/content, deactivation reason or
activation secrets. Actor-owned import links expose only batch IDs, UTC expiry and row counts.

As-of captures projection time after authorization, not a guarantee of live data throughout rendering.
All database reads share the snapshot; status expiry uses the existing safe activation-status reader.
No historical-status reconstruction is claimed. Audit/event timestamps stay UTC.

## Operational versus applied-period definitions

Operational totals are organisation-wide CURRENT state across ALL dates, unaffected by the period
or department report filter. Active/Inactive counts mean employment lifecycle, not activation,
scheduled availability or capacity. Department count includes zero-employee departments. Pending
means every currently Pending request; preview earliest start then ID, at most five, links read-only.

The applied report reuses the existing department report reader/calculator, without a nested
transaction or duplicated clipping logic. The existing endpoint now explicitly uses shared active,
activated/current HR authorization. Existing DTO/metric semantics remain unchanged:
- Pending/Approved request counts: matching current statuses intersecting inclusive period.
- Distinct employees with Approved leave: IDs once across matching Approved requests.
- Approved request-days: each Approved request clipped to selected period, both endpoints inclusive,
  summed per request. Same/cross-type overlaps count separately, never unique absence or staffing.
- Inactive contributions: separate Approved request, distinct employee and request-day metrics.
- Departments: employee CURRENT department, not nonexistent historical department snapshots.
- All included departments have rows, even zero matches. Filtered overall totals reconcile with
  breakdown; each employee currently belongs to one department.

Trends use calendar-month buckets clipped at both selected-period boundaries (at most 13 buckets
for 366 dates). Request COUNTS are attributed ONCE to the bucket containing max(request start,
selected-period start). An ongoing request that started before the period counts in the first bucket.
Approved DAYS split across every intersecting bucket using the SAME clipping calculator. Thus bucket
Pending/Approved counts, Approved days and inactive days sum to the period totals. Distinct employees
are not summed across buckets. These are current-status planned-leave-date trends, not submission
or decision-event activity. No months converted to entitlement days, accrual/holiday/statutory rules.

## Precise administrative attention

Rules are shared by Overview and HR directory through Application EmploymentAttention:
- Unknown employment facts: ACTIVE employee with null employee number OR unconfirmed start date;
  each profile counted once. Incomplete inputs are not called legal violations.
- Missing current schedule: ACTIVE, confirmed employment start on/before server SAST today, with
  NO revision effective on/before today. Unknown starts/future starters excluded from this rule.
- Future schedule only: subset of missing current schedule with at least one future revision.
  A current revision plus a future revision is not flagged. Historical revisions remain preserved.
- Pending activation: ACTIVE employment with RequiresActivation. Delivered/expired invitations may
  still await activation. Inactive accounts are excluded; no account is activated/reactivated here.
- Delivery failed/pending acknowledgement: ACTIVE Pending activation accounts whose current safe
  delivery state equals DeliveryFailed/PendingDelivery. Expired safe state is counted in Pending
  activation, not mislabelled as a current delivery outcome. Private pickup is not email delivery.
- Unfinished imports: ONLY initiating HR's unexpired, uncancelled batches with non-Created rows.
  Counts all such batches/rows; latest created-time then ID list limited to 20. Ready and all other
  uncreated outcomes (Invalid/Duplicate/Failed) are separate. Created outcomes never count as work
  needing account recreation. Stored review states require authoritative revalidation at confirmation.
  Delivery recovery uses employee resend, not row recreation. Other HR's previews remain private.

Attention categories overlap and are not summed as unique employees. Directory HR-only DTO adds
three computed flags (no schema fields). Allowlisted attention URL filters select the same flags/
activation states; invalid filters default to All. Employment status label clarifies lifecycle.
Existing edit/schedule/resend controls are destinations. /admin/onboarding?batch=ID loads only owned
outcomes with cancellation/epoch guards, announced loading and disabled competing upload controls.
It does not confirm/revalidate automatically or alter the original loaded version. Expired/cancelled
reviews cannot authorize creation, regardless of a stale link. Existing server safeguards remain.

## UI/cache/date design

HR-only Overview is administrative; combined roles compose existing personal/Manager workspaces.
Existing Administration quick cards are replaced by working links in the HR panel; sidebar remains
additive with one Overview. No approval/rejection controls or speculative alerts.

Draft start/end/department inputs require Apply. Result labels come from the applied API report.
New keys have no previous-selection placeholder; refresh labels earlier results as stale/unconfirmed.
Errors without data do not display zero; 403/401 hide protected cached data and stop periodic retries.
Valid empty results explicitly identify confirmed zero metrics. Retry retains drafts/applied filters.

Initial month uses browser SAST UTC+02:00, independent of browser timezone and dependent on browser
clock until server data is available. Reset uses the latest server businessToday when available.
Attention uses server SAST today; as-of/expiry labels explicitly UTC. Inclusive dates display without
browser offset shifts, reusing established date helpers. No authoritative browser-clock claim.

Authenticated Axios signal + account/session/start/end/department key. Entry/focus and visible-page
60-second polling or explicit Refresh observe changes elsewhere, not instant cross-account updates.
Initiating-session callbacks invalidate HR workspace after employee edits/lifecycle/resend/schedules,
imports, configuration, decisions/cancellation/submission. Existing non-replayable writes preserved.
Late different-account and same-account relogin responses cannot replace the current session's data.

Recharts reused in a lazy module/shared build chunk; matching trend/department tables, explicit
units, tooltips, accessibility layer, no animations, semantic Light/Dark chart tokens. Scrollable
table containers have keyboard focus. Existing forms/dialogs remain, with no new dependency.

## Migration, preservation and limits

No migration/dependency/schema/calculation change. No inferred employment facts or fabricated audit,
request, import or activation history. Source rollback removes views/projections/filters only, not
persisted records. Large-organisation load, per-active-account activation reads and all own live
review decoding still need load review; bounded lists do not guarantee bounded total read cost.
No deployment/compliance claim. #92 remains open for calculation-dependent completion.
See [executed evidence and limitations](../verification/hr-workspace.md).
