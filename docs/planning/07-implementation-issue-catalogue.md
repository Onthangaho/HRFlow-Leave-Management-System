# Implementation issue catalogue

Created 8 October 2026 after reviewing open and relevant closed issues.
These bodies are planning contracts, not implemented capabilities. Existing #21/#22
are reused through the reconciliation proposal; their GitHub bodies remain unchanged pending review.

---

# [74: Enforce JWT expiry and current permissions across protected HRFlow APIs](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/74)

## Problem and value
Program.cs currently sets ValidateLifetime=false. Older monitoring, queue and personal routes rely on JWT role claims while newer capabilities recheck live membership. Expired or revoked capabilities must not retain access.

## Scope and exclusions
Inventory every protected controller/read/write; enable reviewed lifetime/clock-skew validation; verify current active Identity capabilities and current reporting scope. Keep Application authorization and provider-specific protection in Infrastructure. Use existing deferred read snapshots and writer reservation for authoritative checks; no HR decision override or self-approval. Do not broaden this into session persistence or a database-provider migration.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
API bearer expiry/current-access checks and consistent safe 401/403/404 contracts; Application current-capability guards for legacy reads and protected submission/cancel; client handles expired access through valid same-session refresh and one bounded retry; access loss clears only the initiating session, and explicitly non-replayable operations remain non-replayable. Prefer existing Identity state; add only necessary revocation metadata if design review proves it required.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic business-operation replay or previous-session data placeholder. The bounded authentication retry above is retained. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
None; can start in the first phase.
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Expired JWTs fail with documented clock skew; valid active accounts still work; rotating refresh remains bounded and single-use.
- [ ] Removed Employee/Manager/HR permissions deny all relevant old-token routes, including monitoring, employee reads, selectors, submit/cancel and queues.
- [ ] Authorization and data use the same read snapshot; write authorization occurs after writer reservation. Current eligible managers retain additive personal capability; HR-only never decides.
- [ ] Expired access tokens may use the existing valid same-session refresh and at most one retry; issuer, audience, signature, expiry and a documented small clock skew remain enforced.
- [ ] Invalid, expired or revoked/replayed refresh tokens terminate only their initiating session; inactive accounts remain denied.
- [ ] Current permission loss returns an appropriate denial, not a refresh loop. Combined accounts retain any other current valid capability.
- [ ] Old-session requests cannot refresh, clear or retry against a new session, including logout/login as the same account; delayed success cannot expose old data.
- [ ] Explicitly non-replayable operations remain non-replayable. Logs include identifiers/correlation, never credentials or tokens.

## Verification and failure/regression cases
Use disposable HTTP/browser accounts, expiration/clock boundaries, revoked combined roles, inactive accounts, self/out-of-scope requests, two-process revocation versus writes and unchanged audit rows. Inventory each endpoint's executed result; distinguish already-authorized in-flight read snapshots from subsequent access.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Preserve identities, request/audit rows, versions and existing lifecycle safeguards. No silent role restoration in seeding.

## Priority and relative effort
- Priority: **Essential for pitch**
- Relative effort: **Medium** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [75: Approve the South African leave specification and calculation boundary cases](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/75)

## Problem and value
The shipped calendar entitlement model is not a statutory South African engine. Encoding an ambiguous or outdated rule would make convincing UI misleading.

## Scope and exclusions
Obtain qualified labour-law review of docs/planning/04-south-african-leave-requirements.md and worked fixtures; verify BCEA amendments, operative Van Wyk order, UIF distinction, agreements, schedules/holidays and POPIA. Classify enacted/interim/proposed/company rules with source, section, effective/check dates. No calculation code until relevant rules are approved.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Versioned requirements/decision register and named review ownership; define future rule/eligibility/payment/evidence inputs and outputs. No API/database/UI product change in this review gate; subsequent issues implement approved contracts.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
None; can start in the first phase.
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Resolve adoption-age retained wording, sole-employed/shared allocation, recovery/notice/deadline rules and transitions; do not use court media summary in place of the order.
- [ ] Approve annual/sick/family cycle anchors, fractional units, leap boundaries, incomplete worked-day records and contractual variations.
- [ ] Define certificate proof/payment separately from recording absence; parental eligibility is never solely Gender.
- [ ] Approve minimum-data access/retention and company enhancements; record unresolved decisions as blocked, not assumed compliant.

## Verification and failure/regression cases
Record source retrieval and reviewer decisions with dated boundary examples. Recheck proposed 2026 Bill status and commencement before release. No legal-compliance certification based only on agent research.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
No fabricated legacy dates, parental facts, employment contracts or audits. Safe unknown-data behaviour must be approved.

## Priority and relative effort
- Priority: **Essential for pitch**
- Relative effort: **Medium** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [76: Introduce focused regression gates with an explicit test-convention decision](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/76)

## Problem and value
Temporary HTTP/SQLite/browser verification is valuable but not a repeatable retained regression gate; repository instructions currently defer automated tests.

## Scope and exclusions
First obtain explicit agreement to retire or narrow that deferral and update both instruction files identically. Then add a focused maintained suite for critical existing safeguards and a repeatable browser smoke, extending it alongside new slices. No speculative framework collection or unrelated rewrite.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Document approved strategy, disposable database fixtures, CI commands and failure artifacts; API/Infrastructure regression seams and client interaction checks only as needed. No automated test files before the convention decision is approved.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#74
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Cover live role/lifecycle access, no self/HR decisions, policy balance/overlap, version conflicts, status/audit rollback and cross-process SQLite serialization.
- [ ] Cover JWT expiry/refresh, account/same-account session epochs, stale forms and no mutation replay.
- [ ] Retain migration preservation/legacy fixtures and add reviewed calculation examples as features land.
- [ ] Document actual running gates versus manual accessibility/load/legal review; do not call an unexecuted checklist coverage.

## Verification and failure/regression cases
Demonstrate deliberate failures are caught and two separate connections/processes are used for concurrency; inspect rows/audits, not HTTP alone. Keep synthetic fixtures isolated and instructions identical.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
No production database resets; fixture credentials stay private. Audit/medical payloads excluded from artifacts.

## Priority and relative effort
- Priority: **Essential for pitch**
- Relative effort: **Large** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [77: Add a responsive role-aware application shell around existing workflows](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/77)

## Problem and value
The protected home page remains a link demonstration rather than a cohesive professional workspace.

## Scope and exclusions
Shared responsive sidebar/header and reusable card/table/form/action hierarchy around existing working routes. Group role menus, active indicators and working links; combined roles get additive menus. No decorative notification/settings buttons, fake metrics or unrelated route redesign.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
React shell/shared styles and role-aware route composition; use current authenticated account/session and existing APIs. No database migration or new backend business capability needed.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#74
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Employee/Manager personal links and eligible Manager/HR sections are clear; HR never receives decision actions.
- [ ] Desktop/mobile has no page-level overflow; keyboard/focus/contrast/reduced-motion and menu announcements work.
- [ ] Loading/empty/error/retry and duplicate-action styling is consistent without replacing existing safeguards.
- [ ] Logout/account switching removes protected content; direct URL denials remain authoritative.

## Verification and failure/regression cases
Real browser at desktop/mobile, keyboard and delayed account switches; smoke submission, decision dialogs, employee editing/deactivation, policies and reports. Capture only synthetic screenshots.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Preserve existing query isolation, original edit versions, audit persistence and API contracts.

## Priority and relative effort
- Priority: **Essential for pitch**
- Relative effort: **Medium** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [78: Add versioned employment dates, employee numbers and work schedules](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/78)

## Problem and value
Employees have no employment start date, unique employee number or schedule, so statutory cycle/charging inputs and reliable onboarding are unavailable.

## Scope and exclusions
HR create/edit/read workflow for employee number, confirmed employment start, effective-dated work schedule and cycle configuration. Provide simple named schedules, scheduled weekdays/hours and approved holiday calendar/exchanges; define rotating/part-time limitations explicitly. No attendance/payroll system, full department CRUD or guessed legacy inputs.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Domain employment/schedule data, safe migrations, Application validation/current HR write authorization, versioned DTOs and HR forms plus read-only personal projection. Preserve existing roles/manager operations and active status.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#74, #75
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] HR can configure required inputs with units/effective dates; employee numbers are normalized/unique under concurrent writes.
- [ ] Legacy null/unknown dates and schedules remain visible as incomplete; account creation is never treated as hire date.
- [ ] Department/role/report safeguards, Preserve/Assign/Clear and stale edit conflicts still work atomically with Identity.
- [ ] Holiday sources and schedule revisions are versioned; a future schedule change does not rewrite approved charged days.

## Verification and failure/regression cases
Disposable migration before/after comparison, unknown/invalid/leap dates, schedules/duplicate numbers, two-process stale edits/assignments and real create/edit forms. Existing leave remains on its explicit legacy calendar basis until conversion is reviewed.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Back up/stop writers, dry-run exceptions and require explicit HR confirmation. Optional Gender is unnecessary for calculations and must not gate parental rights.

## Priority and relative effort
- Priority: **Essential for pitch**
- Relative effort: **Large** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [79: Provision HR-created accounts through secure one-time activation](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/79)

## Problem and value
Current HR creation accepts an initial password. CSV passwords and shared credentials are unsafe onboarding and must not become the production contract.

## Scope and exclusions
HR creates an unactivated account and a short-lived single-use activation process to set an Identity password. Choose reviewed secure delivery through an Infrastructure interface; a Development-only private provisioning flow may be documented, but never claim it is production email. No plaintext-password CSV or public self-registration.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Identity-compatible activation/token state and transaction boundaries, Application HR create/activation services, activation API/UI and HR status/resend controls. Store token hashes where applicable; redact token URLs from logs. Accounts cannot access protected workflows before activation and active status checks.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#74
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Tokens are purpose/account-bound, expiring and single-use; repeated/concurrent redemption yields one safe outcome.
- [ ] Creation/resend require current active HR. Intended recipients redeem anonymously using a valid account-bound invitation, without HR membership or login. Activation cannot overwrite a password, reactivate an employee or grant/restore roles.
- [ ] No credential in preview/report/log; activation secrets are delivered only via the approved channel, not downloadable CSV.
- [ ] Document delivery failure/retry and account state; initial credential flow forces password establishment before normal access.

## Verification and failure/regression cases
Real API/browser activation, expired/replayed/tampered links, delivery failure, deactivation/revocation races and transaction rollback; inspect Identity/profile/token state. Verify generic public errors do not enumerate accounts.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Preserve existing accounts/passwords; migrate activation state without locking them out or silently reactivating them. Validate production delivery ownership before deployment.

## Priority and relative effort
- Priority: **Essential for pitch**
- Relative effort: **Large** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [80: Add Identity password change with verified session revocation](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/80)

## Problem and value
There is no permitted self-service password change or explicit old-JWT/refresh invalidation contract.

## Scope and exclusions
Active authenticated users change their own password using current password and Identity validation. Define whether all sessions end (proposed default: all, including caller); no HR knowledge of passwords or full password-reset/email recovery system.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Application/Infrastructure Identity command, protected endpoint and labelled client form. Review security-stamp/account auth-version validation for bearer JWTs, revoke refresh tokens atomically, and clear protected session caches after success.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#74, #79
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Wrong current password, invalid new password and revoked/inactive actor fail without partial changes.
- [ ] After commit, old JWTs and all prior refresh tokens cannot access/refresh; changing Identity SecurityStamp alone is not assumed to revoke bearer tokens.
- [ ] Concurrent refresh/change follows a valid serial order; 401 does not replay writes.
- [ ] No password logs; accessible confirmation/loading/error, duplicate prevention and retained safe form state.

## Verification and failure/regression cases
Two-process login/refresh versus password change with old/new tokens and row inspection; real browser success/failure/session end; active-account and deactivation regressions.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Minimal reviewed revocation metadata migration if required; never replace historical identities or audits.

## Priority and relative effort
- Priority: **Important next**
- Relative effort: **Medium** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [81: Define and verify a safe single-organisation deployment configuration](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/81)

## Problem and value
The original plan assumes free-tier deployment and configuration-only provider portability without verified persistent storage or SQLite transaction equivalence.

## Scope and exclusions
Select documented hosting/storage constraints; deploy a synthetic staging SPA/API only when separately authorised. TLS/CORS, secrets, migration operation, seed-disabled production, health checks and durable private storage contracts. No multi-tenancy or unreviewed provider switch.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Operational docs/config validation and narrowly necessary Infrastructure configuration; specify SQLite-supported local locking/persistent disk, one-writer limitations and filesystem topology. If choosing another provider, implement/reverify equivalent writer/read protection and migrations, not merely change a connection string.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#74
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Production starts without dev seed credentials/default keys and fails safely on missing security settings.
- [ ] Persistent database/files survive restart; storage is private and outside public web roots.
- [ ] Structured logs/correlation and safe errors are useful without credentials, document contents or medical context.
- [ ] Document capacity/timeout/load limits and costs; no unsupported production/compliance claim or deployment performed by this issue's planning step.

## Verification and failure/regression cases
Staging startup/restart, real browser login/CORS, secrets/log review and measured contention; require deployment approval separately. Verify rollback plan with backups before migrations.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
No direct reset/automatic repair of needed data; migration plans preserve lifecycle/history and stop incompatible writers.

## Priority and relative effort
- Priority: **Essential for pitch**
- Relative effort: **Medium** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [82: Implement and rehearse consistent database and private-file backup/restore](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/82)

## Problem and value
A live SQLite file copy and unrelated file copy do not necessarily form a recoverable snapshot.

## Scope and exclusions
Reviewed database-aware backup, coordinated document manifest/object versions, encrypted restricted backups, retention and a synthetic restore drill. No arbitrary file copying while writes run and no disaster-recovery claims without a drill.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Operational tooling/runbook behind existing persistence/storage conventions; define quiescence or consistent SQLite backup method, RPO/RTO targets, checksums, restore ordering and orphan reconciliation. No public backup downloads.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#81
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Restored profile/Identity/roles/versions/lifecycle/request/audit rows agree; inactive access stays disabled.
- [ ] Files and metadata are coherent, quarantined objects remain inaccessible and missing blobs are reported safely.
- [ ] Secrets/keys required for restore are secured separately; migration rollback losing lifecycle metadata is not routine.
- [ ] Drill records measured recovery time and unrecovered window rather than promising zero loss.

## Verification and failure/regression cases
Disposable populated store with concurrent writers, backup then independent restore, row/count/checksum and login/access tests, missing/corrupt backup cases. Repeat when private files are added.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Retain legal holds/retention policy; no deletion of existing stores during verification. Backup access is tightly authorised and logged without contents.

## Priority and relative effort
- Priority: **Essential for pitch**
- Relative effort: **Medium** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [83: Add private supporting documents with quarantine and scoped access](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/83)

## Problem and value
Supporting evidence is absent and medical uploads require stricter access and lifecycle controls than ordinary request data.

## Scope and exclusions
Usable request upload/view/remove workflow with private Infrastructure storage interface, draft ownership/binding, quarantine/scanning, progress/cancel/retry and abandoned cleanup. Start with PDF/JPEG/PNG, configurable maximum 10 MiB per file and five files per request, subject to review. No public URLs, arbitrary executable/SVG/HTML uploads, diagnoses or attachment-enabled feature before scanning is available.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Application attachment metadata/status/access contracts, migrations, authenticated endpoints and real upload UI. Verify file signatures/content rather than extension/MIME alone; random storage identifiers. Separate ordinary evidence, medical evidence and later profile images. Bind only clean owned uploads to owned eligible requests.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#74, #81
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Manager sees medical evidence status only; owner and explicitly authorised current HR get medical content. Ordinary documents use documented owner/current-scope/HR permissions.
- [ ] Cross-user upload binding/download, revoked/inactive access and quarantined content are denied without existence leakage.
- [ ] Stage file before DB binding, commit metadata/status, compensate or schedule cleanup on failure; transactions do not pretend database/blob writes are atomic.
- [ ] Retention/removal/legal hold and access auditing reuse existing conventions without competing transition audits; log actor/object/action/result, never content or diagnosis.
- [ ] Cancelled/failed uploads cannot bind; retry uses scoped idempotency, scan timeout/failure remains quarantined, safe download headers and limited previews.

## Verification and failure/regression cases
Real upload progress/cancel/retry/download; malicious/mismatched/oversize/truncated content, scanner outage, disk/DB late failure, cross-user and role-change races, abandoned cleanup and backup restore. Inspect DB/blob state and medical privacy DTOs.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Legacy requests have no invented evidence. Approved decision/audit truth survives file expiry; retention per class requires privacy/legal approval.

## Priority and relative effort
- Priority: **Important next**
- Relative effort: **Large** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [84: Configure request descriptions and evidence requirements with submission snapshots](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/84)

## Problem and value
The current leave form has only type/dates; descriptions and proof expectations cannot vary safely by leave type.

## Scope and exclusions
HR configures separate NotRequested/Optional/Required description and evidence settings in current management screens, with conditional rules only supported by the reviewed engine. Dynamic request fields/explanations and client/server validation; no generic rule scripting or retrospective invalidation.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Versioned policy/type requirement DTOs, immutable submission requirement snapshots, Application validation and existing create/form/file binding workflows. Distinguish submission-required evidence from later employer-requested proof for sick payment; recording absence must remain possible.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#74, #83, #22
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Type selection shows correct fields and explanations; server rejects missing required descriptions/clean evidence independently of client state.
- [ ] Snapshot rule version/requirements at submission; later config changes affect future submissions but do not fabricate requirements/evidence on legacy records.
- [ ] Approval rechecks reviewed applicable entitlement and only the explicitly defined evidence/payment workflow; no silent retroactive upload demand.
- [ ] Preserve drafts on configuration/version conflicts, require explicit refresh, prevent duplicates and use session-scoped invalidation.
- [ ] Descriptions are bounded plain text, never requested diagnoses; manager medical access remains status-only.

## Verification and failure/regression cases
All three settings, conditional boundaries, configuration changes while form open, ownership/scan binding, legacy requests and rollback; real HR configure then Employee/Manager submit flow and current approval regression.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Nullable legacy snapshot/unknown handling, no invented historical descriptions or audits. Retention/access apply to text as well as files.

## Priority and relative effort
- Priority: **Important next**
- Relative effort: **Large** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [85: Deliver reviewed annual leave cycles and schedule-based accrual end to end](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/85)

## Problem and value
Current lifetime calendar subtraction cannot express employment-anniversary annual cycles or agreed accrual methods.

## Scope and exclusions
Implement signed-off BCEA annual rules and company enhancements using the shared engine, effective-dated schedules/holidays, day/hour units and clear cycle displays. No payroll settlement, guessed carry-over forfeiture or automatic conversion of all legacy policies.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Extend versioned HR policy forms/API and calculation outputs for reviewed annual method/agreement; employee balance/request explanation, approval current-rule validation and report charged-unit presentation share the engine. Preserve SQLite write coordination.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#75, #78, #22
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Employment-anniversary cycles, 21 consecutive-day baseline/schedule equivalents and agreed 1/17 day/hour methods use explicit units and real qualifying inputs.
- [ ] Holiday and schedule boundaries, fractions/rounding and spanning-cycle allocation match approved examples; months never become constant day counts.
- [ ] Approved charges and rule/schedule references are stored as historical facts; future edits cannot silently recalculate them.
- [ ] Legacy incomplete inputs display unavailable/legacy basis with an explicit HR remediation path; no fabricated dates or audits.
- [ ] Pending reserves nothing unless separately re-scoped; competing approvals cannot exceed the reviewed allocation.

## Verification and failure/regression cases
Signed-off worked fixtures, leap/cycle/public-holiday boundaries, fractional units, two-process over-entitlement approval, policy edits versus approval and persisted charge/audit rollback; real configure/request/approve/balance/report workflow.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Safe opt-in migration and comparison report; preserve original requests/decisions/audits and explain differences from legacy calendar totals.

## Priority and relative effort
- Priority: **Essential for pitch**
- Relative effort: **Large** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [86: Deliver sick-leave cycles with conditional evidence and payment review](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/86)

## Problem and value
Fixed annual-style balances and mandatory-upload assumptions misrepresent sick cycles and proof/payment rules.

## Scope and exclusions
Reviewed 36-month entitlement, first-six-month one-per-26 worked-day rule, episode thresholds and employer proof requests; usable recording and HR evidence/payment review. No diagnoses, medical adjudication, occupational compensation engine or automatic payment denial.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Engine sick rule and worked-day provenance, absence/evidence/payment-status contracts, HR review UI and owner status/explanations; eligible managers continue existing decision capability but receive medical status only.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#75, #78, #84, #22
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Six-week schedule entitlement and first-cycle deduction avoid double charge; distinguish actual worked inputs from scheduled estimates.
- [ ] More-than-two consecutive days or more-than-two occasions/eight weeks trigger the reviewed employer-request workflow, including exceptions.
- [ ] Absence can be recorded when proof/pay eligibility is unresolved; unavailable data is visible, not silently zero.
- [ ] Medical access is owner/authorised HR only; safe notices and disputes keep managers out of raw content.
- [ ] Historical charges, reasons/audits and pending current-rule validation remain truthful and atomic.

## Verification and failure/regression cases
Reviewed sick boundaries at six/36 months, 25/26/52 worked days, 2 versus 3 days/occasions, weekend episodes, exceptions, missing proof and HR dispute; real form/review/privacy workflows and failure/concurrency regression.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
No fabricated worked days, certificates or legacy proof. Explicit HR reconciliation for missing inputs; retention and payment status require legal review.

## Priority and relative effort
- Priority: **Important next**
- Relative effort: **Large** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [87: Deliver event-based family responsibility and company compassionate leave](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/87)

## Problem and value
Family responsibility eligibility/events and enhanced compassionate leave cannot be represented by a universal fixed balance.

## Scope and exclusions
Reviewed service/schedule eligibility, annual employment cycle, qualifying events/relatives, part days, reasonable proof and separately labelled company enhancements. Childbirth uses the parental workflow, not outdated family guidance.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Engine event/eligibility rule, minimal event declarations and HR configuration; dynamic owner request, manager decision/evidence status and balance explanation UI using shared contracts.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#75, #78, #84, #22
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Longer-than-four-months and at-least-four-days/week boundaries, three statutory days and reviewed lapse/variation rules are explicit.
- [ ] Qualifying versus enhanced events are labelled without unnecessarily collecting relatives' personal details.
- [ ] Proof/payment and absence recording stay distinct; medical content is not revealed to managers.
- [ ] Cycle-spanning/part-day charge and historical rule snapshots agree across submission/approval/balance/report.

## Verification and failure/regression cases
Exactly-four-months, three/four-day schedules, eligible/unsupported events, partial days, empty entitlement and future enhancement changes; real workflow plus rollback/permissions/current-reporting regressions.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Retain legacy basis and existing decisions. Do not fabricate event evidence or retrospectively reject approved history.

## Priority and relative effort
- Priority: **Important next**
- Relative effort: **Medium** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [88: Deliver reviewed parental, adoption and commissioning leave cases](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/88)

## Problem and value
Generic day balances and gender-only gating cannot represent Van Wyk allocations, recovery and family arrangements.

## Scope and exclusions
Implement only legally signed-off interim/enacted rules with explicit effective dates, calendar-month periods, employment/parental relationships, allocation declarations, birth recovery, notice and adoption/commissioning anchors. Block unresolved adoption-age/order interpretation from automatic eligibility coding. No UIF benefit calculator or external-employer integration.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Minimal private case/allocation data and snapshots, central engine rule, HR configure/review plus owner request/explanations and manager evidence-status/decision workflow. Company-paid enhancements separated from unpaid statutory leave and UIF information.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#75, #78, #84, #22
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Actual court order rather than media summary governs single/sole-employed versus shared allocation; do not hardcode calendar months as days.
- [ ] Recovery/fitness and miscarriage/stillbirth safeguards, continuity and reviewed disagreement deadlines are enforced without Gender being the sole eligibility input.
- [ ] Other parent's external employment/allocation is a minimised declaration with HR review, not claimed verified interoperable data.
- [ ] Policy/rule changes preserve charged history and case evidence; current pending validation shows applicable rule and any unresolved review.
- [ ] UI distinguishes time-off rights, employer pay and uncertain UIF benefits; no unsupported legal-compliance promise.

## Verification and failure/regression cases
Approved birth/adoption/commissioning fixtures, leap/month starts, sole/shared/overlap/disagreement cases, recovery/notice failures, legacy unknowns, sensitive access, rollback and real owner/manager/HR workflows.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
No retrospective parenting/gender inference; source review gates and retention cover third-party/medical facts.

## Priority and relative effort
- Priority: **Important next**
- Relative effort: **Large** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [89: Add HR CSV onboarding with mapping, preview and safe row confirmation](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/89)

## Problem and value
Manual HR entry is slow, but the sample Password CSV risks credential leakage and partial/duplicate accounts.

## Scope and exclusions
HR-only upload/map/validate preview/confirm workflow. Template: FirstName, LastName, EmailAddress, EmployeeNumber, Roles, Department, ManagerEmail, EmploymentStartDate, WorkScheduleCode, optional Gender. Reject Password columns; no silent overwrite/reactivation/role change. Initially managers must already exist; no within-file manager creation ordering.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Application import orchestration reuses employee validation/Identity atomic persistence. Bound file to 100 data rows and 1 MiB UTF-8 CSV initially, reject malformed/overlimit input before confirmation. Short-lived server preview is actor/file/hash/config-bound with safe retention; real HR mapping/Ready-Duplicate-Invalid UI and result download.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#74, #78, #79
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Use Identity email normalizer; detect duplicates within file and against profile/Identity NormalizedEmail/NormalizedUserName, including inactive/unlinked accounts and employee numbers.
- [ ] Validate departments, existing active Manager/same-department/cycle rules, full roles and employment/schedules; optional Gender neither required nor parental gate.
- [ ] Revalidate each row under writer reservation/current HR; per-row transaction commits all Identity/profile/roles/reporting or rolls back all. Other valid rows can succeed, no orphan accounts.
- [ ] Confirmation has actor-scoped idempotency and stable row outcomes; repeated requests do not re-create successes. Contention/failure rows require explicit retry after refreshed validation; imports race safely.
- [ ] Activation replaces passwords; no secrets in logs/previews/results. Formula-injection-safe downloads contain minimal authorised row errors; delete temporary upload/preview by documented retention.

## Verification and failure/regression cases
Real mapping/preview/confirmation, duplicates/case/inactive/unlinked accounts, invalid roles/relationships, revoked HR, two-process overlapping imports, late Identity failure, repeated/cancelled batches and malicious CSV/formula cells. Inspect account/profile rows and activation outcomes.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Existing records never overwritten or reactivated. FirstName/LastName mapping to current FullName is explicit; no fabricated legacy employment data. Batch and size values are product limits, not legal rules.

## Priority and relative effort
- Priority: **Important next**
- Relative effort: **Large** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [90: Build an API-backed personal leave dashboard with explicit periods and units](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/90)

## Problem and value
Employees and Managers land on links rather than their own balances and actionable leave information.

## Scope and exclusions
Personal balances, Pending requests, upcoming Approved leave, recent history and Request Leave action; Manager includes the same personal capability. No placeholder numbers, HR-only personal access, decorative settings or implied annual entitlement for every type.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Minimal owner-scoped dashboard projection with consistent read/current roles and explicit as-of/period/unit fields; React cards/lists link to existing submission/history/timeline. Reuse central calculation outputs and clear legacy-unavailable states.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#77, #85
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Counts/dates/balances are real API data; Paid/unpaid/cycle/legacy units are labelled accurately.
- [ ] Pending and upcoming periods are documented, overlapping requests never imply unique absence metrics.
- [ ] Loading/empty/error/retry/refresh and accessible desktop/mobile links work.
- [ ] Account/session keys, cancellation and initiating-session invalidation prevent delayed leaks; entry refreshes.

## Verification and failure/regression cases
Real owner/Manager/HR-only denial, status/period boundaries, unknown legacy inputs, account switches/delays and mobile/keyboard; submission/cancel/approval updates without reload and persisted totals agree.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
No employee enumeration or historical rewriting; display source basis and current calculation date.

## Priority and relative effort
- Priority: **Essential for pitch**
- Relative effort: **Medium** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [91: Build the Manager workspace and read-only team calendar from scoped data](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/91)

## Problem and value
Managers need personal leave plus eligible decisions and a usable coverage view without gaining HR or self-decision authority.

## Scope and exclusions
Compose personal information, eligible queue attention, upcoming team absences and accessible month calendar/list from existing authorised team endpoints. Preserve 62-day range semantics, current same-department direct reports and inactive history. No new calendar dependency or approval controls inside read-only calendar.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Minimal snapshot projections only where existing APIs lack counts; manager dashboard/calendar React views share account/session/range-scoped queries and list alternative. Links go to existing queue/team/history.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#77, #90
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Own requests appear in personal section but never eligible queue; current live Manager scope is authoritative.
- [ ] Inactive approved history is labelled and excluded from active coverage; count distinct active people per date when shown, unioning overlaps.
- [ ] Counts have explicit date periods/units; month navigation, retry/refresh and keyboard/mobile work.
- [ ] Reassignment/decisions refresh this session's views; delayed account responses cannot appear.

## Verification and failure/regression cases
Real manager/combined-role workflow; self/non-reports/other department excluded, overlapping types count person once per date, inclusive boundaries, inactive/reassigned reports and session-delay regression. Existing approvals unchanged.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Current reporting scope, not historical department reconstruction; no raw evidence/medical data.

## Priority and relative effort
- Priority: **Important next**
- Relative effort: **Medium** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [92: Build the HR workspace with scoped monitoring trends and administrative attention](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/92)

## Problem and value
HR has working reports/monitoring but no coherent administrative dashboard with accurate operational attention.

## Scope and exclusions
Compose organisation Pending attention, selected-period trends, policies/employment-input attention and administrative links using existing reports. Extend rather than replace reporting; no approval/rejection actions, fake alerts or unique-absence claims from summed durations.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Current active HR deferred-snapshot projection only for missing metrics, explicit filters/as-of/unit metadata, accessible dashboard charts with matching tables and same-session/query cancellation. Reuse Recharts; no unrelated upgrades.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#77, #85
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Selected period/request counts/distinct people/calendar durations/charged units are separate; inactive contributions and current-department attribution are explicit.
- [ ] Draft filters never relabel old results; refresh/loading/error/empty and table alternative work.
- [ ] Combined HR users get capability; revoked/inactive HR and non-HR direct URLs/API are denied.
- [ ] Administrative attention reflects real incomplete inputs or failed onboarding, not speculative notifications.

## Verification and failure/regression cases
API/database reconciliation of overall/department totals and overlapping history, real desktop/mobile/keyboard/filter/retry, delayed account switches and existing HR employee/policy/monitoring/report regressions.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Historical charges remain stable; no medical content or deactivation reasons in aggregate dashboard/logs.

## Priority and relative effort
- Priority: **Essential for pitch**
- Relative effort: **Medium** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [93: Deliver durable scoped in-app notifications from committed leave events](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/93)

## Problem and value
There is no durable recipient notification workflow; transient UI feedback cannot reliably inform employees/managers after committed decisions.

## Scope and exclusions
Real unread list/read state and safe links for submit/decision/cancel events, manager assignment changes and relevant administrative outcomes. Start in-app, not email/SMS; no decorative bell before usable data. Define recipient rules for current assignment and event-time recipients.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Transactional outbox/notification metadata persisted with the existing domain transition (not a second audit), idempotent delivery worker behind Application/Infrastructure seams, recipient-scoped APIs and working client inbox integration. General notification preferences are a separate dependency in #94 and do not ship in #93.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#74, #21
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Only committed events deliver; late rollback delivers none and retries never duplicate a notification.
- [ ] Current active recipients can read only their own notifications; deep links recheck current scope and can safely be unavailable after reassignment.
- [ ] Medical content, diagnoses, raw deactivation reasons and note text do not appear in notification payloads/logs.
- [ ] Read/unread behaviour works across retries and sessions with bounded polling; general preferences remain #94; no automatic replay of leave decisions.

## Verification and failure/regression cases
Submission/decision/owner/HR cancellation, rollback and worker restart/retry, role/lifecycle/reassignment changes, cross-user IDs and delayed session responses; inspect outbox/notification/audit uniqueness and real browser links.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Do not manufacture past notifications/audits; define retention and minimal event metadata. No duplicate audit mechanism.

## Priority and relative effort
- Priority: **Important next**
- Relative effort: **Large** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [94: Add permitted self-service profile fields and working preferences](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/94)

## Problem and value
Profile/preferences workflows are absent; HR-controlled employment details must not become casually editable settings.

## Scope and exclusions
Read-only name/email/employee number/roles/department/manager/employment/lifecycle details; explicitly permit only preferred display name and optional contact phone after privacy review. Working theme and actual in-app notification preferences. No self-role/reporting/email/employment edits, placeholders or session persistence expansion.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Minimal versioned self-profile/preferences DTOs and current-account Application writes plus client forms/theme integration. Canonical HR name remains the audit actor display source unless a separately reviewed change is made.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#77, #80, #93
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Only allowlisted fields are accepted server-side; unknown privilege fields cannot change employee management state.
- [ ] Stale edits conflict, inactive/revoked accounts fail and values are private/minimised.
- [ ] Theme actually applies with contrast/reduced-motion; notification toggles affect delivered categories but cannot hide mandatory security notices.
- [ ] Profile,password and preferences use session-scoped callbacks; no old-account settings/success after logout.

## Verification and failure/regression cases
Real self-edit/stale/overposted fields, HR edit concurrency, theme persistence per account, preference delivery and keyboard/mobile; Identity/password/session and employee manager/version regressions.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
No canonical HR/audit rewriting; nullable new fields for legacy accounts and clear privacy notice/retention.

## Priority and relative effort
- Priority: **Important next**
- Relative effort: **Medium** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [95: Add safe private profile image upload and removal](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/95)

## Problem and value
Profile imagery needs image-specific processing and must not inherit medical-document permissions or expose metadata.

## Scope and exclusions
Owner upload/remove with reviewed HR visibility; JPEG/PNG only initially, 2 MiB and bounded decoded pixel count, reencode/crop with orientation correction and EXIF stripping. No SVG, external image URL, facial recognition or public bucket.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Separate image storage category/metadata/pipeline behind Infrastructure storage interface and authenticated versioned owner APIs; usable crop/preview/progress/cancel/remove UI with accessible initials fallback.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#83, #94
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Actual content/dimensions/decompression limits are checked; malformed/polyglot/oversize images cannot become public content.
- [ ] Cross-user bind/remove and inactive access fail; old blobs are cleaned after successful replacement with safe failure compensation.
- [ ] Images carry no EXIF/location data; medical retention/access is never reused.
- [ ] Session change cancels/hides upload completion and duplicate actions are blocked.

## Verification and failure/regression cases
Real browser upload/remove, rotation/EXIF, malformed and decompression fixtures, DB/storage failure, interrupted replacement, cross-user permissions and backup restore; inspect safe processed output.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Optional image data with default initials; no biometric or historical employee inference. Separate retention/deletion and access metadata.

## Priority and relative effort
- Priority: **Future**
- Relative effort: **Medium** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [96: Extend existing leave reports with scoped formula-safe CSV exports](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/96)

## Problem and value
Reporting is usable but has no safe export, and future statutory charged units must not be confused with calendar durations or staffing coverage.

## Scope and exclusions
Extend current authorised HR department and Manager team reports with bounded CSV export and accurate charged-unit columns/trends. Matching readable tables; no unrelated BI system, medical evidence/reasons or historical department reconstruction.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Application scoped consistent-read export projections and streamed/download endpoints; UI uses applied filters/date limits, labels export scope and disables duplicates. Neutralise formula-leading values including whitespace/control variants using reviewed spreadsheet-safe encoding.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#92, #91, #22
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Exports repeat live current authorization, active lifecycle and existing scope/range filters; no arbitrary manager or unrestricted employee ID accepted.
- [ ] Request counts, distinct employees, summed calendar request-days and preserved charged units have separate headers/units; overlaps remain explicit.
- [ ] Empty/invalid/stale permission and interrupted downloads are safe; filenames and cells disclose no secrets or raw personnel context.
- [ ] Export access logs contain actor/filter identifiers/counts, not row payloads; results remain session-bound.

## Verification and failure/regression cases
API/row totals versus displayed tables, inactive/current-department changes, malicious =,+,-,@/tab/CR cells, Excel-compatible safe output, revoked roles and delayed account switching; audit/history unchanged.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
Preserve legacy unknown charging basis and privacy retention; export never fabricates historic department or entitlement facts.

## Priority and relative effort
- Priority: **Important next**
- Relative effort: **Medium** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.

---

# [97: Create a repeatable synthetic three-role pitch demonstration](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/97)

## Problem and value
A polished pitch needs a repeatable truthful scenario, not ad-hoc production data or unsupported compliance claims.

## Scope and exclusions
Isolated synthetic dataset/runbook covering HR onboarding/configuration, Employee request/history, Manager decision/team view and HR reports/audit. Include combined-role and inactive/legacy limitations; use current sources/rules only after review. No resetting existing stores or committing credentials.

## Roles and authorization
Retain additive Employee/Manager/HR capabilities. Employees access only their own information;
Managers decide only current same-department direct reports, never self; HR administers and monitors
read-only, never overrides decisions. Check current active accounts and relevant current Identity
membership server-side. Client role guards are not authorization. Apply the narrower feature scope
above for documents, profiles, imports, notifications and exports.

## API, database and UI changes
Development-only fixture/runbook, checked screenshot set and documented startup/readiness/cleanup; anchor demo dates reproducibly and keep password/signing material outside repository.
Keep Controllers delegating to Application, Domain framework-free, and provider/file handling in
Infrastructure. Reuse versioned edits, existing AuditEntry persistence, SQLite writer reservation
before validation reads and deferred snapshots for consistent reads. No in-memory-only safeguard,
automatic mutation replay or previous-session data placeholder. Plain validation/conflict errors
must not expose raw database details or sensitive payloads.

## Dependencies and implementation order
#76, #90, #91, #92, #21
Use the phased roadmap; #22 is the existing remaining calculation-foundation issue, not a new
duplicate. Relevant legal rules require signed-off requirements before calculation implementation.
Planning references: docs/planning/04-south-african-leave-requirements.md and
docs/planning/05-product-roadmap.md (documentation review branch).

## Acceptance criteria
- [ ] Three-role walkthrough works without manual SQL fixes or page reloads; HR never decides and Manager cannot self-approve.
- [ ] Only synthetic names/addresses/documents are used; screenshots contain no secrets/local paths.
- [ ] Each metric's units/date period and legal/legacy limitations are spoken explicitly; no full production/compliance claim.
- [ ] Fixture restart cannot reactivate existing accounts or overwrite roles; source database is untouched.

## Verification and failure/regression cases
Recreate in a new disposable store, execute real browser workflow desktop/mobile and inspect persisted charge/status/audit rows; document failures/unexecuted device/accessibility limits.
Use disposable synthetic data and inspect persisted outcomes, not responses alone. Record executed
checks separately from limitations. Follow the current deferred-test instruction until the explicit
test-convention decision; do not add automated test files by assumption. Run applicable build/lint,
audit for changed dependencies, and diff checks during implementation.

## Migration, privacy and historical truth
No real employee/medical data, credentials or public activation links. Keep original verification reports distinct from new demo evidence.

## Priority and relative effort
- Priority: **Essential for pitch**
- Relative effort: **Small** (relative sizing, not the old minute estimates)

This issue plans a focused usable slice; it does not claim implementation or legal compliance.
