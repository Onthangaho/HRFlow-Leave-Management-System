# ADR 0017: Confirmed employment facts and append-only weekly schedules

Status: implemented locally for review, 9 October 2026. Scope: employment-data foundation slice of #78.

## Decision and boundaries

Leave charging remains inclusive calendar days. These are HR-confirmed inputs, not statutory calculations.
#75 qualified legal review and #22 shared calculation contracts still gate cycles, accrual, holidays,
workday deductions and historic charged-day migration. #78 stays open. No CSV onboarding is added.

Employee numbers are trimmed, uppercased invariantly and restricted to 1-32 ASCII letters/digits,
hyphens/underscores, starting with a letter/digit. Internal whitespace is rejected, not removed.
A unique database index includes inactive employees; checks run inside the existing writer reservation.
Creation requires both number and confirmed start date. Dates are date-only, 1900-01-01 through
2100-12-31, inclusive. Legacy fields remain null/Unknown. Account creation/activation timestamps
are never interpreted as employment dates.

Update defaults to preserving employment facts, including Unknown. ConfirmEmploymentFacts=true
requires a complete number/date pair; supplying fields without confirmation is rejected. Existing
employee expectedVersion covers these facts together with profile/Identity/roles/manager changes.
A failed Identity save rolls everything back. Deactivation preserves facts and number ownership.

Future starters are supported as facts. A future date does NOT delay activation, login, leave or
manager eligibility. Existing active/activated/role guards remain authoritative. HR must control
invitation timing separately; there is no automatic start-date access gate.

## Schedules

Each employee has an independent ScheduleVersion (zero generation for an empty migrated stream).
HR appends immutable WeeklyScheduleRevisions: stable ID, name, date-only EffectiveFrom, seven
Monday-to-Sunday integer-minute values, actual UTC RecordedAtUtc and acting employee ID.
Zero means unscheduled; at least one day must be positive. Selected days support 1-720 minutes
(up to 12 hours). This bounded simple-pattern representation is not a statutory maximum claim.
UI displays/accepts working hours and converts to exact whole minutes (7.5 hours = 450 minutes).
Rotating shifts, overnight spans, variable hours and holiday calendars are unsupported.

A revision applies from its date until the next chronological revision. Past confirmed effective
dates may be recorded explicitly; no historical request or charged duration is rewritten. A duplicate
effective date is a conflict, not replacement. No revision edit/delete API exists. Corrections use
another explicit effective date; same-date correction/supersession workflows are not implemented.
The stream expectedVersion rejects stale intentions even when two proposed dates differ.

## Protection, access and privacy

Current active/activated HR is required, with credential-version revalidation inside the shared
SQLite writer reservation before authoritative reads. This serializes competing numbers/schedules,
role changes, reassignment and lifecycle writes across API processes. No callback replay is added.
Target inactive employees cannot receive revisions. Read authorization/history share the existing
deferred snapshot; reads do not reserve the writer. Own Profile uses server-derived identity and
adds read-only facts/history for Employee, Manager, HR-only and combined roles. Private self-profile
versions remain independent. Public reporting/notification DTOs gain no employment fields.
Manager scope, no self-decisions and HR without decision authority remain unchanged.

## Migration/recovery

AddConfirmedEmploymentData adds nullable employee facts, an empty schedule generation and a new
FK-restricted revision table with unique employee/effective-date index. No fabricated numbers,
dates, schedules, account changes or transition audits. Rollback drops these fields and all schedule
history; re-upgrade cannot reconstruct newly confirmed facts. Rollback was not executed.
Existing encrypted operator recovery copies the new schema/data without rewriting it; stopped
writers, isolated restoration and explicit credential/refresh/invitation invalidation still apply.
See the verification report for exact comparisons and unexecuted restored-access checks.

[API contract](../operations/employment-data-foundation.md)
[Executed verification](../verification/employment-data-foundation.md)
