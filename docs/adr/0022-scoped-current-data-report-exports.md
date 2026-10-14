# ADR 0022: Scoped current-data report exports

Date: 14 October 2026. Status: implemented locally for independent review.
Partial issue #96; #22-dependent preserved/statutory charged units remain outstanding.

## Decision and scope

Export existing projections, not a second calculation engine. Application export handlers send
GetDepartmentLeaveReportQuery or GetTeamLeaveSummaryQuery through MediatR. Each existing handler
checks current active/activated Identity capability and credential generation within its existing
SQLite deferred read snapshot. No arbitrary manager/employee ID or client-held rows are accepted.
Infrastructure encodes the resulting immutable DTO after the snapshot completes. Controllers map
bytes to an authenticated attachment response. No schema, dependency, audit or transition changes.

An export is a fresh snapshot, not a copy of a previously loaded screen. Metadata uses
ExportGenerationStartedUtc (UTC before the query), not an invented exact database snapshot timestamp.
Period dates are inclusive calendar dates; SAST business date selection is unchanged in the screens.
No annual accrual, statutory charged units, paid entitlement, staffing capacity or historical department
reconstruction is implied. There is no legal-compliance or production-readiness claim.

## HTTP contracts

- GET /api/v1/reports/department-leave/export?start=YYYY-MM-DD&end=YYYY-MM-DD&departmentId=UUID
  Current HR Administrator, inclusive maximum 366 days. Department is optional; unknown ID is 404.
- GET /api/v1/leave-requests/team-summary/export?start=YYYY-MM-DD&end=YYYY-MM-DD
  Current Manager, inclusive maximum 62 days. Current same-department direct reports only; excludes self.

The actor comes from authentication. Missing/malformed/reversed/out-of-limit selections are 400;
missing/invalid/revoked credentials or inactive/pending accounts are denied at the existing API gate;
current capability loss is 403. Existing read contention maps to 409. Successful downloads set
Content-Type text/csv; charset=utf-8, attachment with a generated date-only filename, Cache-Control
no-store and X-Content-Type-Options nosniff. Filenames contain no account/employee names.

## CSV contract and quantities

UTF-8 with BOM, comma delimiter, CRLF records, invariant numbers, yyyy-MM-dd dates and UTC ISO timestamps.
CsvHelper 33.1.0 (already present in Infrastructure) quotes every field and escapes embedded quotes.

HR columns:
RecordKind, DepartmentId, Department, PendingRequests, ApprovedRequests,
DistinctEmployeesWithApprovedLeave, SummedClippedApprovedCalendarRequestDays,
InactiveApprovedRequests, DistinctInactiveEmployeesWithApprovedLeave,
InactiveSummedClippedApprovedCalendarRequestDays, PeriodStart, PeriodEnd,
ExportGenerationStartedUtc, Attribution, Semantics.

A Total row precedes Department rows ordered exactly as the existing table, including zero departments.
Pending/Approved counts are requests intersecting the applied period. Distinct employees have at least
one intersecting Approved request. Approved request-days sum each Approved span clipped to the period,
including both endpoints; overlapping same/cross-type requests count separately. Inactive columns
are subsets of the totals, based on current lifecycle. Attribution uses current employee departments.
A department filter also narrows Total. No-department output still contains Total with zero values.

Manager columns:
RecordKind, RequestId, EmployeeId, EmployeeName, EmploymentStatus, LeaveTypeId, LeaveType,
RequestStart, RequestEnd, RequestStatus, PeriodStart, PeriodEnd, ExportGenerationStartedUtc,
Attribution, Semantics.

A Metadata row with blank request fields always precedes ApprovedRequest rows (including empty teams).
Requests retain full dates, not clipped durations, and predictable start/name/ID ordering. Inactive
records are labelled historical leave; they do not imply active staff absence. This CSV contains no
coverage-day aggregate, descriptions, medical metadata, filenames, decision notes, phone numbers,
personnel reasons, credentials or activation URLs. Combined roles have additive existing capabilities;
HR-only never gains team decision/export authority from HR membership alone.

## Spreadsheet safety

Every untrusted name (department, employee, type) has a literal visible `Text: ` prefix, before its
original characters, including whitespace, controls, Unicode variants, quotes, commas and newlines.
This deliberately avoids relying on trimming/first-character heuristics, CSV quoting alone, or an
apostrophe/tab that a spreadsheet may strip. Values never start with formula syntax after whitespace
trimming. Fixed metadata/headers, server-generated IDs/dates and nonnegative metrics are controlled.
The prefix is part of downloaded data; independent consumers may remove exactly one prefix from
these name columns only when treating the remainder as plain text, never formulas. Exports are not
lossless name-only interchange files. Do not remove safety prefixes and reopen untrusted data as formulas.

Sources checked 14 October 2026:
- [OWASP CSV Injection](https://community.owasp.org/attacks/CSV_Injection): dangerous ASCII/full-width
  formula characters, delimiter/quote handling, save/reopen caveats and no universal spreadsheet strategy.
- [CsvHelper official writing documentation](https://joshclose.github.io/CsvHelper/getting-started/)
  and [writer source](https://github.com/JoshClose/CsvHelper/blob/33.1.0/src/CsvHelper/CsvWriter.cs).

Actual downloaded bytes were independently parsed with temporary csv-parse 6.1.0. Excel and LibreOffice
were not exercised. Do not infer all application/import/resave compatibility from source or parsing checks.

## Bounds, cancellation and logs

Maximum 5,000 Department or ApprovedRequest data rows, excluding header/Total/Metadata. Entire output,
including BOM/metadata, is capped at 2 MiB with a bounded stream. Violations return clear 400
ProblemDetails before any partial file is sent. Nothing is silently truncated. Response buffering is
intentional for this bounded slice. These are output limits, not a load guarantee: reused projections
may read more database rows before rejecting. Large-organisation/load verification remains outstanding.
Cancellation propagates through existing queries and per-row encoding checks.

Success logs record actor/filter IDs, date range, aggregate row/byte counts; existing request logs
supply correlation/status and cancellation outcomes. No row contents, names or leave-state download
audits are created. The existing transition/audit mechanism is unchanged.

## Client session and filter contract

Existing report table, HR workspace period and Manager calendar expose a shared export control.
Selections come from loaded applied report metadata or displayed calendar range, not draft filters
or the selected single calendar day. Export GET uses authenticated Axios with skipAuthReplay=true.
It never automatically refreshes/replays a download after 401. User retries are explicit.

An AbortController/lock prevents duplicate calls. Scope captures identity, login generation and
range. Range/session replacement or unmount aborts requests and revokes temporary object URLs.
Completion rechecks scope before creating/clicking a download link or showing a notice. URLs are
also revoked after download initiation. Cancel stops pending work and offers explicit retry. Network
failure retains filters; messages are scoped to the initiating session. Bytes already successfully
handed to the browser cannot be recalled after a later logout; local downloads require appropriate
operator handling. No protected data is placed into persistent/unscoped browser storage.

## Consequences

No migration or rollback data loss; removing this feature removes endpoints/controls, not records.
Read snapshots can finish in their valid earlier serial order when a concurrent change commits later.
Fresh downloads and subsequent calls recheck current permissions. Other users' screen caches are not
invalidated by an export. Existing reporting/session/transaction and non-replayable writes stay intact.
