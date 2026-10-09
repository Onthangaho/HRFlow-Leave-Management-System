# Employment-data foundation API and operator notes

This is #78's confirmed facts/fixed weekly schedule slice, not statutory leave calculations.
Refer to [ADR 0017](../adr/0017-confirmed-employment-data.md). #78 remains open for #75/#22 dependencies.

## HR employee create/edit

POST /api/v1/employees retains current activation, role and optional manager contract, and requires
employeeNumber plus employmentStartDate (ISO yyyy-MM-dd). HR never receives a chosen password.
For example, a space-padded sa-001 normalizes to SA-001; 2024-02-29 is a valid leap date.
2025-02-29 fails. Numbers contain 1-32 ASCII letters/digits/hyphens/underscores, starting alphanumeric.
Date bounds: 1900-01-01 to 2100-12-31. Future dates are facts only: activated accounts can access
HRFlow before that date under existing permissions. No automatic start-date access rule exists.

PUT /api/v1/employees/{id} retains expectedVersion and the complete role replacement plus manager
Preserve/Assign/Clear semantics. Omit confirmation and employment fields to preserve existing/Unknown
facts. Set confirmEmploymentFacts=true and provide BOTH employeeNumber and employmentStartDate to
assign/revise confirmed facts. Clear-to-Unknown is not supported; never enter a guessed date.
Numbers remain unique including inactive employees. No overwrite of another account is allowed.
HR list/detail adds nullable employeeNumber/employmentStartDate. Null is displayed as Unknown.

## Schedule history

GET /api/v1/employees/{id}/schedules returns version and revisions ordered effective date, then ID.
POST to the same route appends with this body (Monday through Sunday minutes):

```json
{
  "expectedVersion": "00000000-0000-0000-0000-000000000000",
  "name": "Fixed daytime week",
  "effectiveFrom": "2027-01-01",
  "minutes": [450, 450, 450, 450, 240, 0, 0]
}
```

Zero generation is valid only for an empty stream; subsequently use the loaded returned version.
Schedule names: trimmed 1-100 characters, no controls. Seven integers required, each 0-720; at least
one positive. Dates use the same bounds. Returned revision IDs are stable; timestamps are explicit UTC.
Future/past effective dates do not change leave charging. Existing dates cannot be replaced.
A separate schedule Save does not save the employment form. Employee HR Version and ScheduleVersion
are independent. Read-only inactive details expose preserved history with no schedule save controls.

400: shape/name/number/date/hour/confirmation validation. 403: current HR permission denied.
404: target missing. 409: duplicate number/date, stale employee/schedule, inactive target or writer
contention. Normal contention retains the existing bounded timeout, without transaction replay.
Keep entered values until explicit reload; schedule reload confirms discarding dirty values.

GET /api/v1/me adds own nullable number/start date and scheduleHistory under its existing snapshot.
Self-profile writes still allow only preferred name/phone; overposting cannot change employment facts.
The UI retains the original private-profile generation independently of HR employment edits.

## Operations

Stop all API/worker writers and take an encrypted verified backup before applying the migration with
the existing offline migration tool. Do not copy a running SQLite file or automatically repair data.
The new columns are nullable, so all legacy rows migrate without inventing facts; the new revision
table begins empty. Downgrade drops numbers/start dates and ALL newly recorded schedules. Re-upgrade
cannot restore them. Follow [backup/restore](backup-restore.md) and recovery credential invalidation;
restored old security state must not be trusted. Local drill includes schedule/fact row comparisons
and actual clean/quarantine blobs. Production, Linux, load and full restored browser access remain
unverified. No extra statutory retention period or automatic deletion is introduced.
