# HR CSV onboarding contract and operations

See [ADR 0018](../adr/0018-hr-csv-onboarding.md) and [actual verification](../verification/hr-csv-onboarding.md).

## HR workflow

Open CSV onboarding under Administration. Download/extract the password-free template ZIP and its README, upload, inspect headers,
map columns, validate, review Ready/Duplicate/Invalid, select Ready rows and explicitly confirm.
Preview creates no accounts/invitations. Roles: Employee;Manager;HR Administrator (full names, semicolon).
FirstName/LastName join into FullName. Dates: yyyy-MM-dd, real calendar date, existing 1900-2100 bounds.
Department exact existing name; ManagerEmail existing active/activated same-department Manager, or blank.
Gender must be blank: nonblank supplied data is explicitly rejected. WorkScheduleCode is a unique
uppercase ASCII template code; blank leaves schedule Unknown. HR can create/edit named templates on
this page; each loaded version is required to replace an existing code. Template edits do not change
copied employee revisions. Hours convert to whole minutes, at least one weekday, maximum 12 hours/day.

**Future employment dates do not delay activated-account access.** HR must control invitation timing.
Leave still uses inclusive calendar days. Schedules are confirmed facts, not statutory calculations.

## API

All /api/v1/onboarding routes require current active/activated HR and current credentials.
- GET template: ZIP containing UTF-8 header-only CSV and README, including explicit unsupported-Gender guidance.
- POST inspect: multipart file; whole-file validation, returns headers only.
- POST previews: multipart file plus mapping JSON (canonical field -> source header). Returns 201/Location
  and id/version/expiresAtUtc/cancelled/rows, including row numbers, states, errors and snapshot references.
- GET previews/{id}: actor-owned review/outcomes. Missing or other actor's batch returns 404.
- POST previews/{id}/revalidate: expectedVersion. Explicitly refreshes live validation; retains successes.
- POST previews/{id}/confirm: expectedVersion, UUID idempotencyKey, distinct Ready row numbers.
- POST previews/{id}/cancel: stop remaining rows, preserving committed employees.
- GET previews/{id}/results: no-store minimal row outcomes CSV, no uploaded personal/formula cells.
- GET schedule-templates; POST schedule-templates: code/name/seven Monday-Sunday minutes and
  nullable expectedVersion for creation, required matching version for replacement.

File limit 1 MiB UTF-8, optional BOM, 100 data rows; transport allows bounded multipart overhead.
Request form buffering stays in memory within this bounded transport size, avoiding raw-upload temp files.
Invalid mapping/CSV/configuration: 400. Lost permission: 403. Missing batch: 404. Stale/expired/key or
writer contention: 409. Duplicate existing/file rows never overwrite accounts. HTTP 200 confirmation
can contain partial successes, Invalid/Duplicate/Failed rows: inspect the row outcomes.

## Failure, delivery and retention

Do not automatically resubmit a lost/timed-out confirmation. Reload outcomes, explicitly revalidate
remaining failures, then confirm with a fresh key. Cancellation is not rollback of committed rows.
Invitation Prepared/DeliveryFailed does not mean account creation failed: use Employee management's
versioned resend. Development private pickup is not email and never appears in API/results/screenshots.
Production creation/import is denied before any account write until an approved provider is configured.
No SMTP/provider/deployment is added.

Previews expire after 30 minutes. Hourly cleanup strips personal fields/errors/mapping after expiry
plus 24 hours, in batches of 20; restart/backlog delays are possible. Minimal committed outcomes remain
without an implemented retention purge. SQLite/files/backups remain private; follow the operational
access/key-custody controls. No personal fields or CSV rows belong in application logs.

Backup/restore preserves templates, employee copied patterns and committed row outcomes. Recovery
invalidates unfinished reviews (cancelled, expired, new version); fresh authorized upload/review is
required. Existing security invalidation/operator recovery in [backup runbook](backup-restore.md) applies.
Rollback drops onboarding review/templates/outcomes; accounts and copied revisions remain. Re-upgrade
cannot recover dropped review data. Do not migrate automatically during recovery.
