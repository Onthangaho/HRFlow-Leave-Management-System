# HRFlow

HRFlow is a single-organisation leave-management portfolio application for Employees, Managers
and HR Administrators. It demonstrates layered .NET/React engineering, current-permission
security, versioned writes, auditable decisions and private-file handling. It is **not certified
legally compliant or production-ready**.

## Current status — 21 October 2026

Merged through [PR #133](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/133), including restored-store activation safety, guarded activation coordination, HR attention aggregation and Manager calendar keyboard interaction.
See the [current readiness review](docs/planning/08-current-readiness-review.md),
[issue reconciliation](docs/planning/09-issue-reconciliation.md) and
[unpublished tracker proposals](docs/planning/10-proposed-tracker-updates.md).

Shipped current-feature workflows:

- Employee/Manager request submission, current configured balances, personal history and cancellation.
- Manager current same-department direct-report decisions with optional notes, team workspace/calendar.
- HR employee create/edit/deactivate, combined roles/reporting, confirmed employment facts and weekly
  schedule history, CSV mapping/preview/confirmation, leave type/policy management and organisation reports.
- Immutable transition audit timelines, recipient-scoped notifications and optional category preferences.
- Secure one-time activation with guarded invitation/recipient acknowledgements and private **Development-only** pickup; production delivery remains unavailable. Password change revokes sessions.
- Offline first-administrator provisioning and reviewed existing-account recovery are separate working operator tools.
- Own-account allowlisted profile fields, System/Light/Dark theme and private processed profile images.
- Private scanned supporting documents, medical-content restrictions and company requirement snapshots.
- Scoped spreadsheet-protected CSV exports, operator encrypted backup/restore and synthetic pitch fixture.

Current leave charging uses **inclusive calendar days**. Balance is current configured entitlement
minus Approved request days; Pending does not reserve balance. Annual cycles, accrual, holiday
calculations and statutory categories remain dependent on qualified review (#75) and #22/#85–#88.
Approved decisions remain preserved; current-policy changes can change displayed balances.

## Permissions

Employees access their own leave. Managers additionally decide eligible current same-department
direct-report requests, never their own. HR administers/monitors and has **no approve/reject override**.
Combined roles receive additive capabilities. Server checks current active/activated accounts,
Identity membership, reporting scope and credential version; navigation is not authorization.
Managers see medical evidence status, not raw medical content. HR can view employee images but
cannot replace another employee's image. Private contact data is not public directory data.

## Architecture

```text
React SPA -> HTTP -> API controllers -> Application -> Domain
                       Infrastructure -> Application + Domain
```

Domain has no EF/HTTP dependency. Infrastructure supplies Identity, EF Core/SQLite, private storage,
scanner and transactions. Writes reserve the SQLite writer **before** authoritative reads; consistent
reporting/history reads use deferred snapshots. Versions prevent stale overwrites; transition audits
commit with status changes. Authenticated caches/callbacks are account/session scoped.

Stack: .NET 8 / EF Core 8 / ASP.NET Identity / MediatR / FluentValidation; React 19 / TypeScript 6 /
Vite / Tailwind 4 / React Router / TanStack Query / Axios; xUnit and Playwright retained regression gates.
SQLite is supported on one host with locking-compatible persistent **local** storage. Network-file
multi-host operation is unsupported. A different provider needs equivalent transactions/migrations
and re-verification, not just another connection string.

## Run locally

Prerequisites: compatible .NET SDK/runtime 8, Node (retained fixtures currently document Node 24),
and real ClamAV for successful document/image uploads. See the exact configuration and operations:

- [Single-host configuration and security settings](docs/operations/single-host-configuration.md)
- [Private evidence and scanner setup](docs/operations/supporting-documents.md)
- [Private profile image setup](docs/operations/private-profile-images.md)
- [Isolated synthetic pitch setup/start/rehearsal/cleanup](docs/operations/pitch-rehearsal.md)

Use user-secrets/environment settings for signing material, independent refresh pepper and generated
private Development fixture credentials. Never commit or print them. Development seeding is guarded;
it does not repair existing roles/reactivate accounts. Do not run Development seed defaults on a
real employee store. The isolated pitch/regression tooling generates private synthetic credentials.
No passwords or invitation links are included here.

```powershell
dotnet restore HRFlow.sln
dotnet run --project src/HRFlow.Api
npm.cmd ci --prefix src/HRFlow.Client
npm.cmd run dev --prefix src/HRFlow.Client
```

These commands assume private configuration is already prepared per the linked runbooks. Default
Development HTTP API profile uses `http://localhost:5228`; client API base is configured with
`VITE_API_BASE_URL=http://localhost:5228/api/v1`. Vite normally uses `http://localhost:5173`.
Confirm the actual addresses printed by your local profiles. Production requires trusted HTTPS
origin/host/proxy and persistent paths. Development Swagger is `/swagger`; `/health` is liveness,
`/health/ready` is database/migration readiness, not business-integrity or capacity certification.

Existing databases must be preserved. Back up needed data, stop all writers and follow
[migration preflight](docs/adr/0004-leave-policy-management.md) and
[encrypted recovery](docs/operations/backup-restore.md). Reset only explicitly disposable stores;
no automatic legacy repair or delete-and-restart advice applies to needed records.

## Tests and evidence

Focused maintained automated tests are authorised under #76. Use
[tests/README.md](tests/README.md) for exact fixture, scanner, local/CI and cleanup commands.

```powershell
dotnet build HRFlow.sln
dotnet test tests/HRFlow.RegressionTests --no-build
npm.cmd run build --prefix src/HRFlow.Client
npm.cmd run lint --prefix src/HRFlow.Client
npm.cmd --prefix tests/browser run typecheck
npm.cmd --prefix tests/browser test
git diff --check
```

Run backend and browser suites sequentially. Fixtures use fresh file-backed migrated SQLite and
generated private Identity data, not existing stores or EF InMemory. Scanner-free gates prove
fail-closed behavior; real positive image/document scanning needs the documented real scanner.
Routine traces/video/failure screenshots are disabled to protect credentials/private payloads.

[Dated verification reports](docs/verification/) separate source inspection, real HTTP/browser
interaction, controlled transport barriers and persisted-row/file checks. PR #119 records 19 image
backend tests and a combined 12 browser run after FreshClam daily 28153 (14 October 06:24 UTC).
The full 54-case backend result is earlier recorded evidence, not a new readiness-review run.
Hosted [PR #119 workflow](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/38651078671)
never started its job due to account billing restriction; that block is historical. Original PR #120
then ran and produced 50 backend passes / 4 failures. Merged PR #121 corrected confirmed fixture
and production file/directory creation permissions without weakening validators. Its final
[hosted Ubuntu run](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/38786228861) passed **55 backend and 9 Chromium tests**.
Positive real-ClamAV coverage remains separate local evidence. These retained Linux gates do not
verify all Linux deployment, scanner or recovery scenarios. Billing and protections were unchanged.

Current merged #133 final [hosted Ubuntu run](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/39676852702)
passed **115 backend and 51 Chromium tests**, with build/lint/audit/typecheck. This reconciliation
checked that existing result, not reran it. The [recovery report](docs/verification/restored-activation-safety.md)
preserves the earlier local 111/112 failure and unexplained startup/refusal timeouts; cleanup corrections
are not proven root-cause fixes. No positive real-ClamAV or target-host readiness is inferred from CI.

## Release boundaries

A synthetic current-feature pitch is supported by the retained walkthrough. A real employee-data
pilot still needs qualified applicable leave rules, target-host verification of the
[offline first-admin tool](docs/operations/offline-first-administrator.md), approved production
invitation delivery, privacy/retention/legal-hold decisions, target-host TLS/proxy/storage/recovery,
representative accessibility/volume acceptance and named operational monitoring/support.
Development pickup is not production email. Offline reviewed-identity recovery is not fresh-admin
provisioning. Medical Required submission remains unsupported pending proof/payment versus absence
review. No guaranteed RPO/RTO, load capacity, statutory compliance or production deployment is claimed.

Persistent browser sessions, full department CRUD, payroll, multi-tenancy, hard deletion/reactivation
and general email/SMS notifications remain excluded. Avoid adding inactive placeholder controls.

## Repository and contribution

- `src/`: API, Application, Domain, Infrastructure and Client.
- `tests/`: retained regression gates and browser fixture.
- `tools/`: operator tooling; never exposed through public HTTP.
- `docs/planning/`, `docs/adr/`, `docs/operations/`, `docs/verification/`: rationale and dated evidence.

Create a focused branch from fetched main, preserve layer/permission/transaction/session safeguards,
run relevant retained checks, and use an independently reviewed PR. Never commit secrets, private
files, real personnel CSVs, databases, traces or private invitation pickup content.
