# ADR 0018: Actor-scoped CSV review and row-atomic onboarding

14 October 2026. Implemented locally for review under #89; not production deployed.
PR #111 confirmed merged. Base: 1d2345268ae2a3c90fe7df0ce86f8c571b64cd1b.

## Decision

Use CsvHelper 33.1.0 in Infrastructure for RFC4180 quoted fields/newlines and strict UTF-8 decoding.
[Official parser documentation](https://joshclose.github.io/CsvHelper/getting-started/) checked 14 October 2026.
Fully parse at most 1 MiB / 100 data records before returning headers or storing preview rows.
An optional UTF-8 BOM is accepted. Duplicate headers, invalid encoding, credential/password/token/hash
headers and malformed CSV fail the entire upload without displaying records. Inspect returns headers only.

FirstName and LastName are trimmed and joined with one space into canonical FullName. Roles use
semicolon-separated existing full role names; dates are yyyy-MM-dd. Department names are exact.
ManagerEmail must resolve to one existing active/activated same-department Manager. No within-file
manager dependencies. Blank manager is permitted. Gender is optional in the template but nonblank
values are explicitly Invalid: storage/visibility has not been designed, and no value is silently discarded
or used for leave eligibility. Blank WorkScheduleCode means Unknown, not an invented pattern.

Existing employee revisions have no reusable codes. The smallest additional HR lookup is a unique,
versioned named template with seven whole working-minute values. Imported revisions copy values/name
and use the confirmed start date. Template changes invalidate a preview and never rewrite history.
Only fixed weekly patterns are supported. Future dates do not defer activated-account access; leave
charging remains inclusive calendar days. No statutory calculations, holiday engine or cycles.

## Atomicity and authorization

Manual creation and import use the same EmployeeManagementService validation/persistence core.
The core takes no nested transaction and performs no external delivery. Current active/activated HR
and credential generation are rechecked after the existing SQLite writer reservation. Duplicates use
Identity NormalizeEmail/NormalizeName and profile email, including inactive/pending/unlinked records;
normalized employee numbers remain unique in the database. No overwrite/reactivation/role repair.

A server-persisted preview binds actor, source SHA-256, mapping, review version, normalized rows and
resolved department/manager/template generations. Clients select row numbers, not employee DTOs.
Every row is freshly validated inside its own writer transaction; Identity/profile/roles/reporting,
employment facts, initial schedule and successful row outcome commit together. Other valid rows may
continue after a persistence failure. There is no automatic callback replay. Contention returns 409;
explicit reload/revalidation precedes retry. Failed persistence outcomes are recorded only after rollback.

Actor-scoped confirmation UUID uniqueness and durable selection/outcomes serialize competing processes.
Already-created rows are never created again. Revalidation preserves successes and starts an explicit
new confirmation generation for failures. Cancellation serializes between rows, stops remaining work,
and never undoes prior commits. A response lost after commit is uncertain: reload outcomes, do not
assume failure or create again. Delivery is outside the writer reservation; Prepared/DeliveryFailed rows
require the existing versioned resend workflow if interrupted or delivery fails.

## Lifecycle and privacy

Review authorization expires after 30 minutes. Normalized preview fields remain in private SQLite;
raw CSV is not persisted. The hourly cleanup worker strips fields/errors/mapping from up to 20 reviews
whose expiry is more than 24 hours old, marks them cancelled and rotates versions. Downtime/backlog
can delay cleanup; this is not a guaranteed purge SLA. Minimal row outcomes/IDs/source hashes remain;
no automatic committed-outcome purge/retention administration is implemented.

Only the initiating current HR can read/cancel/revalidate/confirm/export their batch. Exports include
row number, server-generated outcome, employee ID and delivery state only: uploaded spreadsheet
formula cells and personal fields cannot enter exports. No invitation/password is returned or logged.
Template and batch reads use consistent authorization snapshots. UI drafts and callbacks are isolated
by account/session, mutations non-replayable, polls bounded and cancelled on exit.

## Migration/recovery

AddCsvOnboarding adds only two tables/indexes. Existing data is unchanged; no legacy import history
is invented. Down drops templates and preview/outcomes, not created employees or copied revisions.
Re-upgrade cannot reconstruct those records. Rollback/re-upgrade was not exercised.
Encrypted restore cancels/expires every restored batch and rotates review generations while preserving
committed outcomes/templates. Existing credential/password/refresh/invitation invalidation remains.
Operator authorized-access recovery remains the existing runbook, not an import authorization bypass.
