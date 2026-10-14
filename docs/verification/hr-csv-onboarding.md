# HR CSV onboarding verification

14 October 2026. Branch feat/hr-csv-onboarding; base 1d2345268ae2a3c90fe7df0ce86f8c571b64cd1b.
Working tree was clean before branching from fetched origin/main. PR #111 was confirmed merged
(9 October 2026). No existing database or unrelated changes were reset. Temporary tools and
private fixtures/invitations/logs are excluded from the review diff. No test framework/files added.

## Executed local HTTP and persisted-row checks

A new disposable copy of the synthetic employment fixture was migrated. Original rows/columns
across 20 pre-existing tables matched exactly; integrity_check ok and foreign_key_check empty.
The new migration creates onboarding batches/templates only; no account/schedule history invented.

Two independent API processes shared that disposable SQLite store. Actual HTTP plus SQL inspection:
- Employee direct onboarding API denied 403. Current HR role removed in SQLite: old-token read denied
  403; fixture membership restored only for continued synthetic verification.
- Final bounded multipart checks rejected invalid UTF-8 and 101-row files; another combined-role HR
  received safe 404 reading the initiating HR preview.
- Password columns, duplicate headers, unterminated quotes, differing field counts and oversized files
  rejected 400. BOM and quoted commas/newlines accepted. Nonblank optional Gender explicitly Invalid.
- Preview wrote review metadata but no Identity/profile/invitation. Ready, Invalid unknown roles,
  normalized file duplicate emails and existing duplicates were shown.
- Inactive, Pending activation and unlinked profile email collisions were Duplicate, not overwritten.
- Confirmation created a passwordless Pending activation account, profile/roles/reporting/employment
  facts and one copied schedule. Pending login denied. Private Development invitation redeemed;
  normal login then succeeded. No token/password appears in report or screenshots.
- Two-process overlapping numbers: Created/Duplicate and exactly one account/profile, no orphan.
  Two-process repeated same confirmation key likewise yielded exactly one account and durable outcome.
- Template edit after preview prevented creation until explicit revalidation. New pattern copied
  300 minutes; earlier employee revision remained 450 minutes. Manager edit generation after preview
  likewise invalidated confirmation until review.
- Injected schedule INSERT failure rolled back Identity/profile/roles/facts/schedule and persisted Failed.
  Injected Identity INSERT failure left no orphan; removing the temporary trigger and explicitly
  revalidating/retrying produced one committed account. Temporary triggers were removed.
- Cancellation from the second API process after partial commit preserved 23 employees, stopped
  remaining 2 of 25 rows and left batch cancelled. It is not rollback of prior commits.
- An expired batch could not confirm (409). Minimal CSV results contained row identifiers/states only,
  no uploaded personal data or formula cells.

After shared validation/normalization hardening, the main API/two-process suite was rerun with fresh
synthetic identities/numbers and passed. After the dependency patch, real manual creation/edit passed
and retained employment facts/manager/roles; invalid email became an Invalid preview rather than 500;
normalized preview and confirmation passed. Existing authenticated bound-document download returned
200 and own Profile remained usable. No new scanning/outage or full leave-decision matrix was run.

## Real browser evidence

Actual Chrome via temporary Playwright used the TLS development client/API, not static rendering.
Custom headers were mapped to canonical fields; validation showed one Ready and one Invalid row.
Explicit confirmation created an account and SQL confirmed its pending Identity/profile.
Confirmation starts on Cancel; Escape dismissed it and restored focus to the trigger. Mobile
390x844 and desktop 1366x900 had no page-level overflow; table scrolling stays within the container.
Employee login after logout was denied direct onboarding route and showed no prior import rows.
Actual Settings theme selection supported Dark review/mobile results and Light desktop screenshots.

Browser checks found incorrect JSON default headers on multipart requests: fixed with explicit
multipart headers on inspect and preview. Mapping controls now have explicit accessible names.
Earlier failed harness attempts used ambiguous wrapped-select labels and an incorrect post-login
URL expectation for a guarded return route. Corrected real runs passed. An interrupted local Vite
process caused connection refusal; restarting the disposable client restored the run. These failures
are not counted as product passes.

Screenshots are synthetic and contain no credentials, invitation URLs, phone data or machine paths:
- [Desktop Dark review](screenshots/hr-csv-onboarding/review-desktop-dark.png)
- [Mobile Dark results](screenshots/hr-csv-onboarding/results-mobile-dark.png)
- [Desktop Light upload/templates](screenshots/hr-csv-onboarding/upload-desktop-light.png)

## Encrypted recovery

Fixture API/workers were stopped before operator backup/restore. Initially broad disposable ACLs were
correctly refused; only the new disposable directories were restricted, then the drill passed.
Independent encrypted restore preserved employee facts/revisions, templates and batch row outcomes
exactly. Every restored batch was cancelled/expired with a rotated generation, so it cannot authorize
more creation. Existing password clearing, credential rotation, invitation clearing and refresh
revocation remained. Request/document rows and clean/quarantine bytes matched, integrity/FKs clean.
No real recipient delivery or restored API startup/provisioning was performed.

## Build, model and dependency evidence

Backend build passed with zero warnings/errors. EF pending-model check: no changes since migration.
Frontend build passed: 825 modules, main JS 561.89 kB / 167.40 kB gzip. Existing Vite >500 kB warning.
Lint passed with only existing useAuth.tsx:223 Fast Refresh export warning after fixing new poll deps.
No frontend package/lock changes. npm audit returned zero vulnerabilities.

CsvHelper 33.1.0 is the sole parser addition. The initial current-feed solution audit reported
System.Security.Cryptography.Pkcs 8.0.1 through PDFsharp 6.2.4 -> Infrastructure -> API/Operations
(runtime dependency, not development-only). High [GHSA-846f-f764-76mx](https://github.com/advisories/GHSA-846f-f764-76mx)
and moderate [GHSA-2fmv-rv2c-59x7](https://github.com/advisories/GHSA-2fmv-rv2c-59x7), published
13 October 2026. Microsoft's official guidance identifies 8.0.2 as the compatible patch for both.
A focused direct 8.0.2 pin was added, without EF/PDFsharp/major upgrades. The subsequent solution-wide
vulnerability check reported no vulnerable packages in all five projects using current configured feeds.
Build/model, final creation/input/document regression and encrypted recovery ran against that patch.
No exploit reproduction or SDK/runtime update is claimed; operators must follow Microsoft servicing guidance.

## Explicitly unexecuted

100-row maximum-success and exhaustive encoding combinations; every role/account combination; department rename/deletion after preview; HR revocation specifically while waiting behind
writer contention; delivery-failure/resend rehearsal; template UI
stale-edit/keyboard matrix; exhaustive duplicate-click/network loss and delayed same-account relogin
responses; restart cleanup/backlog timing SLA; comprehensive screen-reader/contrast tests; full
leave submission/decision/medical/scanner/notification/report regressions; restored API permission/
operator access recovery, rollback/re-upgrade, Linux, load and power-loss checks remain unexecuted.

No production provider, SMTP, deployment, statutory calculations or legal-compliance claim. #89 stays
open for review. See [ADR](../adr/0018-hr-csv-onboarding.md) and
[operations/API](../operations/hr-csv-onboarding.md) for supported contracts, retention and recovery.

Implementation review: 29 intended source/migration/documentation/screenshot files. Working-tree diff check and
new-file whitespace/UTF-8/sensitive-value scans passed; both instruction files are byte-identical.
Synthetic screenshots were visually reviewed and are each below 300 kB. Fixture logs contained no
known credentials, raw JWTs or invitation URLs. No files were staged, committed or pushed during implementation.

## Independent-review finalisation

Finalisation fixes bundle the unchanged header-only CSV with a README documenting unsupported
nonblank Gender, field formats and password prohibition. UI downloads the ZIP; extract its CSV
before upload. Credential header detection now recognizes punctuation-separated names/common
aliases. Nonblank Gender is explicitly Invalid even when that row also duplicates an existing account.
Earlier screenshots show the implementation-time download label; no private data was introduced.

Finalisation model check reported no changes since the migration.
Finalisation reruns: backend build passed with zero warnings/errors; frontend build passed
(825 modules, main JS 561.90 kB / 167.41 kB gzip); lint passed with the existing Fast Refresh warning.
Solution vulnerability check reported no vulnerable packages on configured feeds. Assets inspected
for Infrastructure, API and Operations consistently resolve System.Security.Cryptography.Pkcs 8.0.2;
Domain/Application do not include that package. No unrelated major/library upgrades.

Real HTTP: current HR downloaded ZIP; archive contained CSV and explicit unsupported-Gender README.
Credential aliases Pass-word/P_w_d/API Key/Authorization were denied before preview. Nonblank Gender
was Invalid on a duplicate row. Local Production startup correctly rejected configured Development
pickup. With pickup removed, local Production API login succeeded, preview returned 201, confirmation
failed safely with 400 and persisted account/profile counts remained unchanged. This is local evidence,
not hosted Production delivery. Delivery-failure/resend remains unexecuted.

Earlier browser/two-process/rollback-injection/recovery results above were not newly rerun during
finalisation. Outstanding scenarios remain outstanding except the now-executed local Production gate.

Finalisation also replaced a nonprinting ellipsis in loading text; frontend build/lint were rerun
and passed with the same documented existing warnings.
