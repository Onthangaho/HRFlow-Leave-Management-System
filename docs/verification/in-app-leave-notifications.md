# Durable in-app notifications verification

Executed 9 October 2026 (Africa/Johannesburg) on feat/in-app-leave-notifications.
Base: b394056124bdb274bc26035f364919354a6b1890. Initial tree was clean; PR #104 confirmed MERGED.
Issue #93 preference wording was clarified: inbox/read state is this slice; general preferences remain #94.
These checks were performed before finalisation; no commit, push, PR, issue closure or deployment was performed during verification.

## Reproduction and isolation

Use a fresh disposable SQLite database and two API processes pointing to it. Generate private JWT/refresh and seed configuration outside the repository; never publish it. Use the four synthetic Development accounts, a 100-day overlap-enabled policy and a synthetic leave type. Exercise real HTTP operations and inspect LeaveRequests, AuditEntries, LeaveNotificationEvents and LeaveNotifications with separate SQLite connections. Use a private temporary abort trigger for late persistence/delivery failures. Do not run these steps against existing employee records.

Real installed Chromium controlled by Playwright used the actual Vite client and actual disposable APIs. No static rendering or Axios-adapter substitute was used. Temporary tooling/configuration/databases/logs remain outside the review diff (ignored tooling or OS temporary storage).

## Executed HTTP and persisted evidence

- Submit, approve, reject and owner cancel: six events and six delivered rows, four Manager alerts and two owner alerts; HR inbox count zero. Decision-note text absent from inbox payloads.
- Two API processes ran workers against the same database. Each source event/recipient had exactly one notification. Combined Manager/HR recipient received one alert per event for 22 real submissions delivered across bounded batches.
- Cross-user mark-read returns 404. Two-process same-item mark-read both returned 204; unread count fell once and original read time remained stable.
- Filter/page validation: invalid filter, page zero and pageSize 51 return 400; pageSize 1 returns one item. Stable timestamp/ID ordering is defined in the query.
- Repeated approval 409 and HR-only approval 403: no additional audits/outbox rows.
- Injected outbox INSERT failure after approval: 500 for this deliberately unexpected persistence error; request remains Pending, audit/outbox counts unchanged. No deliverable partial event.
- Reassign a report with one Pending request: new Manager receives one Pending-work event. Former Manager inbox retains generic unavailable items with null request IDs, no employee names; old timeline link returns 404.
- HR deactivation cancels one Pending request and alerts its current active Manager; response cancellation count one. Raw synthetic personnel reason absent from inbox; inactive owner's old token gets 401.
- Delivery INSERT trigger failure leaves the committed event undelivered. Failure removal permits retry. A second failure/restart check preserved RetryAfterUtc across both API restarts, then automatically delivered once without resetting the retry time.
- Synthetic duplicate-processing fixture (reset delivered marker on an existing delivered event) acknowledged the already-existing notification without another insert. This is a delivery recovery fixture, not evidence that atomic production acknowledgment partially commits.
- Removed last capability returns inbox 403; pending activation and changed credential generation return 401.
- Mark-read issued while a separate connection held BEGIN IMMEDIATE: role removal committed before release -> 403; inactive state / credential generation change -> 401. ReadAtUtc remained null in every case. Restoring fixtures was confined to disposable data.
- SQLite-consistent disposable backup: rollback of the new migration and re-upgrade preserved every Employee, Identity, role, request, audit and refresh row byte-for-value. Rollback removes notification tables; upgrade creates empty tables and no historical backfill.

## Executed interactive browser evidence

- Real header unread badge and recipient inbox; All/Read/Unread filters; authorised history links; useful HR empty inbox.
- Double click produced exactly one PATCH; server-confirmed read state refreshed inbox and header.
- Keyboard Tab from filter to Refresh; mobile drawer Escape restored trigger focus. Desktop 1440 x 960 and mobile 390 x 844 had no document-level horizontal overflow.
- Real 20-item pagination Next/Previous using committed rows.
- Synthetic 503 on inbox read: error/retry retained filter; removing the failure and clicking retry loaded actual API data.
- Held real inbox response across logout/different-account login and same-account relogin: replacement Overview remained, old inbox/success absent.
- Held real successful mark-read response across both session replacements: no old success notice and no replacement-session clearing.
- Synthetic 401 on non-replayable mark-read: one PATCH, zero refresh requests, login reached.
- Existing HR employee/policy/report/monitoring and Manager queue/team routes reached. These were route checks, not full mutation regressions.
- No browser runtime errors in executed inbox checks.

Screenshots contain synthetic Development data only, no credentials, tokens, activation links or local paths:

- [Desktop inbox](screenshots/in-app-notifications/desktop.png)
- [Mobile inbox](screenshots/in-app-notifications/mobile.png)
- [HR empty inbox](screenshots/in-app-notifications/empty.png)

## Checks and limitations

- dotnet build HRFlow.sln: passed. The first final build retried copies because owned verification processes held DLLs (21 MSB3026 warnings); after stopping them, dotnet build HRFlow.sln --no-restore passed with 0 warnings / 0 errors.
- dotnet ef migrations has-pending-model-changes --no-build: no model changes since the migration.
- npm run build: passed, 819 modules; main bundle 517.70 kB (156.13 kB gzip), reports chunk 368.31 kB (106.74 kB gzip). Existing >500 kB Vite warning observed during this slice; no unrelated bundle optimisation.
- npm run lint: passed with the existing useAuth.tsx:223 Fast Refresh warning.
- No dependency/lockfile changes; npm audit was not rerun.
- git diff --check and new-file whitespace checks passed; instruction files match byte-for-byte. Documentation/private-log scans found no generated credentials, signing material, raw JWT patterns, activation URLs or local-user paths. Screenshot size/metadata checks passed.

Not executed in this slice: full activation/resend/password-change/employee create-edit and policy mutation browser regressions; request submission through its browser form (submission here used real HTTP); explicit no-self-recipient HR-as-new-manager fixture; simultaneous leave decision versus reassignment/deactivation beyond existing safeguards; sustained load, database corruption/disk-full, process termination during COMMIT, retention purge, multi-host filesystem deployments, assistive-technology testing, and exhaustive mobile focus/contrast/reduced-motion audits. Existing independent reports remain historical evidence, not rerun claims.

SQLite has one writer. Worker batches are at most 20, poll every five seconds, and retry failed items after 30 seconds; indefinite retries are bounded in frequency, not total attempts. No automatic purge ships: storage growth and repeated failures need operational monitoring and a reviewed retention policy. Delivery and visible badge updates are eventual (worker cadence plus visible-tab 30-second polling), not push delivery. Offset pagination is predictably ordered but can shift when new events arrive. No email/SMS or preferences UI. Not a production-readiness claim.
