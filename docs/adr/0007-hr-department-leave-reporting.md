# ADR 0007: Current-department leave reporting

## Metric definitions (defined before implementation)

The selected period uses inclusive calendar dates, limited to 366 days. A request matches
when its dates intersect the period, including either boundary. Pending requests means
matching requests currently Pending; Approved requests means matching requests currently
Approved. Rejected and Cancelled requests contribute nothing.

Approved request-days sum `(min(request end, selected end) - max(request start, selected
start)).Days + 1` for each Approved request. Only the intersecting portion counts. Same-type
and cross-type overlaps are counted separately. This is explicitly summed request duration,
not unique people-days absent, working days, annual usage or a staffing coverage indicator.
No unique active people-days metric is introduced.

Distinct employees with Approved leave counts each employee once across matching Approved
requests, regardless of overlap or type. Inactive employees' history is included. Separate
inactive Approved request, employee and request-day contributions explain how much of each
total is historical; active employee counts can be read as total minus inactive employees,
but none of these metrics measures simultaneous absence or available staff.

All attribution uses the employee's CURRENT department. Historical department snapshots do
not exist; reassignment moves preserved requests into the new department's report. Requests
and audits are never rewritten. Overall totals apply the identical period/department filter
and equal the department breakdowns. Each employee belongs to one current department, so
distinct employee counts remain additive across departments. Zero-result departments appear.

## API contract and protection

`GET /api/v1/reports/department-leave?start=YYYY-MM-DD&end=YYYY-MM-DD&departmentId=<optional GUID>`
requires HR JWT access and repeats current active employee/current Identity HR Administrator
membership inside the deferred snapshot. The actor comes from authentication. Employee and
Manager-only accounts are denied; combined HR accounts retain HR capability. Invalid/missing/
reversed/oversized dates or an empty department GUID return 400; an unknown department returns
404, unlike a valid department with no leave (200 with zero metrics). Inactive bearer accounts
receive the existing 401; role revocation with an old HR JWT receives 403.

Response: `start`, `end`, `departmentId`, `totals`, `departments`. Each department has
`departmentId`, `departmentName`, `metrics`. Each metrics object has `pendingRequests`,
`approvedRequests`, `employeesWithApprovedLeave`, `approvedRequestDays`,
`inactiveApprovedRequests`, `inactiveEmployeesWithApprovedLeave`, `inactiveApprovedRequestDays`.
No employee names, emails, Identity IDs, reasons or audits are exposed. Departments are ordered
by name and ID. No migration, annual reset, accrual or working-day redesign is required.

Application owns authorization/scoping and arithmetic. `ILeaveReportingReadTransaction`
shares the existing Infrastructure deferred SQLite wrapper with the manager read seam.
Authorization, roles, departments and request projection use the same scoped EF context and
snapshot established at the first SELECT. It never acquires BEGIN IMMEDIATE or saves changes.
Only narrow BUSY/LOCKED errors map to a safe 409; the existing three-second per-command timeout
and absence of application replay remain. WAL permits concurrent writers; rollback journaling
may delay writer commits. In-flight reads may return their earlier authorized snapshot;
subsequent reads recheck persisted membership/lifecycle state. See [ADR 0006](0006-manager-team-leave-summary.md)
and its official transaction references. A provider change must preserve these guarantees.

## Dashboard contract

HR-only `/admin/leave-reports` defaults to the current calendar month. Draft date/department
inputs are separate from applied filters; Apply validates and updates the query, Reset applies
the current month/all departments, and Refresh reloads the applied selection. Results are
labelled from the API's returned period/scope, never from unsaved input. Refetches explicitly
mark loaded results as refreshing; failed reads hide results and retain inputs for retry.

Cards show the four primary metrics and identify inactive contributions. A Recharts horizontal
bar comparison has explicit request-day units, tooltips, keyboard accessibility and disabled
animation. The full department table is the readable alternative. Long names use shortened
axis labels, with full names in tooltips/table. A read-only Pending monitoring link provides
operational follow-up without approval actions.

Query keys contain account, session epoch, dates and department; Axios consumes cancellation
signals, role guards disable unauthorized queries and entry always refetches. Session changes
remount filter state; delayed old responses cannot populate the new query namespace. Applicable
same-session submission/cancellation/decisions, HR employee/reporting/lifecycle and configuration
writes invalidate the account's report prefix. Other users' caches are refreshed on entry/focus
or explicit Refresh, not invalidated remotely.

Verification results and unexecuted checks are recorded separately in
[the verification report](../verification/hr-department-leave-reporting.md).
