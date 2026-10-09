# Private supporting documents — executed verification and limits

9 October 2026. Issue #83, local branch `feat/private-supporting-documents`, based on fetched
`origin/main` at `75f058201148fea54b73c058f6225c3735408e5c`. PR #108 was confirmed merged before
branch creation. The starting tree was clean. This report separates implementation evidence from
the finalisation reruns below. Draft review only; no deployment or issue closure.

## Environment and method

Disposable synthetic databases and generated PDF/JPEG/PNG files, private Windows ACLs, local TLS
API/client and real headless Chrome interactions. Two independently running API processes shared
one disposable SQLite database for the concurrency cases. Temporary harnesses, certificates,
credentials, antivirus definitions/logs, encryption keys, packages and restored databases remain
outside review artifacts. Browser/harness trust exceptions applied only to the disposable local
certificate; API token validation and application security configuration were not disabled.

The actual ClamAV 1.5.4 daemon used main/daily/bytecode databases successfully fetched by FreshClam.
No always-clean implementation was used. Its real INSTREAM adapter returned Clean for valid
fixtures and Rejected for a valid PDF containing the harmless EICAR test string. This demonstrates
the exercised detection path, **not comprehensive malware detection or legal/production compliance**.

Temporary executable harnesses were used, not committed automated-test files. Some initial harness
attempts failed because of route assumptions, local MIME defaults, fixture limits or missing command
configuration. Only the successful corrected checks below are reported as passed. Browser checks
are interactions with the actual client/API, not static rendering or Axios-adapter simulations.

## Executed HTTP and persisted-state checks

- Valid PDF/JPEG/PNG accepted and real-scanner Clean; verified again after strict parser changes.
  Broken PDF cross-reference pointer, truncated PDF/JPEG/PNG, corrupt PNG chunk CRC, content/type
  mismatch, empty and greater-than-10-MiB content returned 400. Review uncovered PNG CRC tolerance;
  explicit CRC validation now rejects that case. PDF reader problem handling is fail-closed.
- Embedded EICAR PDF became Rejected through the actual upload adapter; download returned 404.
- Missing daemon, daemon shutdown, unresponsive TCP fault and bounded two-second rehearsal timeout
  yielded inaccessible ScanUnavailable. Unscanned binding conflicted. Restarting the real daemon
  plus explicit retry made the immutable quarantined fixture Clean. Normal configured default is
  thirty seconds; supported timeout bounds are documented separately.
- Same owner/upload key persisted one object. Changing classification under that key returned 409.
  Two API processes concurrently using one key also persisted exactly one object. This is
  idempotency of a registered upload, not automatic replay of a transfer after an uncertain outcome.
- Clean owned documents bound in real submission; persisted request, bound IDs/timestamps and one
  initial submission AuditEntry inspected. Two processes competing to bind one clean draft produced
  one successful existing-contract 200 and one 409, exactly one new request and one submission audit.
- Six-document, duplicate-ID and null collections returned 400 without a new request/audit. Exactly
  five clean owned documents bound successfully to one request with one submission audit.
- Cross-user binding and download returned safe 404. Medical Manager DTOs contained status/class
  but no content size/MIME/download permission. Manager medical download returned 404; owner,
  current HR and combined-role HR downloads succeeded. Ordinary eligible Manager download succeeded.
  No original filename is stored or returned. Download headers included attachment, no-store, nosniff.
- Reassignment removed the former Manager's scope. Revoked Manager and HR roles using old JWTs were
  denied; inactive owner using an old JWT was denied. Removing HR from a combined Manager/HR account
  retained Manager capability but removed medical-content access/redacted its DTO. These reporting,
  role and lifecycle fixture changes used direct SQL in the disposable database; they are not claims
  of browser employee-management verification.
- Existing eligible approval with a decision note succeeded; request had one submission plus one
  approval audit. Bound removal returned 409; stale draft removal returned 409; permitted removal
  retained metadata and one corresponding object audit. DocumentAccessEntry never creates a leave
  state transition. No evidence was fabricated for legacy requests.
- Injected late audit-insert failure produced no new request, binding or submission audit. Disk-path
  obstruction produced no bindable content and retained Receiving metadata. Aged interrupted metadata
  was reconciled on API restart: Removed plus exactly one system DraftExpiry/BlobCleanup, no blobs.
  This simulates persisted interruption state; it is not a process-kill-at-every-file-write test.
- A substituted private documents junction was rejected before creating staging directories/blobs
  through it; the original synthetic store was restored. Ancestors are checked before creation.
- Real cancelled upload waiting behind a SQLite writer left no metadata. Review found existing aborted
  reads logged as unexpected 500; the narrow request-aborted exception handling now records safe
  client cancellation instead. Non-cancellation exceptions retain existing handling.
- Repeated fixture runs exercised the twenty-draft 409 and the upload/rescan 429 rate safeguard.
  Unused drafts were removed only from the disposable fixture, then format cases reran successfully.

## Executed browser checks

- Real file chooser/multipart upload, progress/waiting status, failed-scan feedback and explicit scan
  retry (one observed POST). Browser MIME fallback is normalised only for absent/octet-stream MIME;
  the server still independently validates extension, MIME and actual bytes.
- Real clean PDF upload/download with downloaded bytes matching the source; checkbox selection,
  actual Request Leave submission, personal history link and authorised timeline evidence. Persisted
  request, binding and exactly one submission audit checked, rather than HTTP alone.
- Real cancellation, retained recovery instructions, disabled submission during processing and no
  automatic submission/replay. Delayed actual upload responses across logout/same-account relogin
  and different-account replacement did not restore old progress or success notices.
- Real keyboard focus through classification/file controls, a reduced-motion browser context and
  desktop/mobile no-page-overflow checks after viewport layout settled. Existing account theme was
  dark for clean workflow screenshots; final outage screenshots were recaptured from the actual final client/API with the daemon stopped.

Screenshots contain synthetic display data and generic document labels, not passwords, tokens,
activation links, private phone data, storage paths or medical content. PNGs are approximately
59–176 KiB each, suitable for review:

- [Desktop clean upload/form](screenshots/private-supporting-documents/clean-request-desktop.png)
- [Desktop outage/status](screenshots/private-supporting-documents/request-desktop.png)
- [Mobile outage/status](screenshots/private-supporting-documents/request-mobile.png)
- [Desktop authorised timeline](screenshots/private-supporting-documents/timeline-desktop.png)
- [Mobile authorised timeline](screenshots/private-supporting-documents/timeline-mobile.png)
- [Mobile replacement-session form](screenshots/private-supporting-documents/request-mobile-session.png)

## Migration, cleanup and attachment-aware recovery

Migration comparison preserved **every row across 17 existing tables** in an independently copied
synthetic pre-document database. Both new tables began empty. Existing Identity, lifecycle, requests,
audits, settings and notification data were not rewritten. Model consistency check passed.

With both API writers stopped, the existing operator tool produced an encrypted package and restored
to a new isolated destination with mandatory recovery security invalidation. SupportingDocuments and
DocumentAccessEntries matched row-for-row; required clean, rejected and unavailable quarantine objects
matched byte-for-byte. Receiving remains honest incomplete metadata, never a clean/bindable file.
Restored quarantine statuses were preserved; no notifications or invitations were sent.

Missing required clean content refused backup without publishing a package. An authenticated fixture
package with a deliberately removed metadata-required blob (and correspondingly consistent ZIP
inventory) refused restore with safe invalid_input_or_package feedback and no published destination.
SQLite integrity/foreign-key and package checks remain those of the existing operator tool. This
extends #82 evidence to real attachment objects, not a guarantee for future images or large stores.

Rollback drops document/access tables but does not delete private blobs. Re-upgrade cannot reconstruct
provenance from those blobs. Rollback execution after collecting evidence was not rehearsed.

## Actual build and dependency outputs

- `dotnet build HRFlow.sln --no-restore`: Build succeeded, **0 warnings, 0 errors** after final backend changes.
- `dotnet ef migrations has-pending-model-changes` with an explicit disposable connection: **No changes
  have been made to the model since the last migration.** An initial invocation without the required
  offline connection was refused; the correctly configured check passed.
- Frontend `npm run build`: passed, **823 modules**, main JS **534.66 kB / 160.72 kB gzip**; existing
  Vite chunk-over-500-kB warning remains. `npm run lint`: exit 0; existing useAuth.tsx:223 Fast Refresh warning.
- New Infrastructure dependencies: PDFsharp 6.2.4, SkiaSharp 4.153.1 and matching Linux native assets.
  No frontend dependencies changed. The earlier NuGet vulnerability check reported the **transitive
  SQLitePCLRaw.lib.e_sqlite3 2.1.6 High advisory GHSA-2m69-gcr7-jv3q**; no new parser-package advisory
  reported. Finalisation investigated and resolved this SQLite advisory as recorded below; this
  earlier result is not a claim that the current native runtime remains vulnerable.
- Tracked/new-file whitespace checks passed; both instruction files were confirmed byte-identical.
  Intended text/docs and private API logs passed fixture-secret/JWT scans. Screenshots were inspected;
  databases, keys, scanner pickup/configuration and temporary tooling are excluded from the review diff.

## Independent-review finalisation: checks rerun on 9 October 2026

Review found a byte-integrity gap: scan/promotion reopened a path, and binding/download only checked
existence. The final implementation hashes a bounded immutable byte snapshot against persisted
size/SHA-256, scans and promotes the same bytes, and verifies clean bytes before binding/download.
Large file reads and scanner work occur outside the writer reservation. Binding checks the original
verified metadata version, ownership and Clean/unbound state inside submission protection. Download
rechecks current scope, credentials, status and version before its access audit commits, then returns
the verified snapshot. A private filesystem change cannot substitute unchecked downloaded bytes.
The private store still requires restricted operator/service access; filesystem and SQLite are not
one atomic resource. Snapshots buffer at most 10 MiB per file; hostile-load memory capacity is unverified.

The following were actually rerun after these changes and the native SQLite pin:

- Backend `dotnet build HRFlow.sln`: **0 warnings, 0 errors**. Frontend build **823 modules**, main
  **534.66 kB / 160.72 kB gzip**; existing Vite size warning. Lint exit 0 with the existing Fast Refresh warning.
- Model consistency with an explicit disposable connection: **No changes have been made to the model
  since the last migration.** Migration preserved every row in **17 existing tables**; new tables empty.
  The first model invocation without the required offline connection was correctly refused.
- Real ClamAV valid PDF/JPEG/PNG, malformed/CRC/truncated/mismatched/empty/oversized rejection,
  embedded EICAR rejection and denied download. Initial daemon-startup check returned ScanUnavailable;
  clean acceptance passed after daemon readiness. Explicit retry passed with the real scanner.
- Two API processes: competing upload key yielded one object; competing binding yielded 200/409,
  one new request and one submission audit. Cross-user binding, revoked HR and combined-role medical
  redaction checks passed with persisted rows inspected.
- Same-size clean-blob tampering: binding **409**, no new request; download **404**, Missing metadata,
  no successful Download audit. Original synthetic bytes were restored for recovery verification.
- Unresponsive scanner bounded timeout remained inaccessible. Injected late audit failure rolled back
  request/binding/audit. Disk-path failure retained unbindable Receiving metadata for reconciliation.
- Actual Chrome upload, byte-matching download, form submission/history/timeline, desktop/mobile,
  keyboard/reduced motion and cancellation. Delayed completion after same-account and different-account
  relogin did not restore old upload/success state. A rerun first reached the 20-draft safeguard; unused
  disposable drafts were removed through the API and the full session scenario then passed.
- Stopped both disposable API/worker processes, then reran real encrypted backup/isolated restore.
  Document and access rows and clean/quarantined bytes matched; statuses were preserved. Missing a
  required clean blob refused backup without publishing a package. A temporary harness filename
  substitution initially caused a syntax error; the corrected harness ran successfully.
- Six synthetic screenshots visually reviewed: no tokens, credentials, activation links, medical
  contents or machine paths. New screenshot captures reflect actual finalisation browser interaction.
- Finalisation staging allowlist admitted only the 36 intended source/migration/documentation/PNG
  files. Staged whitespace and fixture-secret/JWT/machine-path scans passed; working and staged
  instruction files were byte-identical. Temporary harnesses and private runtime artifacts were excluded.

### SQLite advisory investigation and focused resolution

Before: **SQLitePCLRaw.lib.e_sqlite3 2.1.6**, High,
[GHSA-2m69-gcr7-jv3q / CVE-2025-6965](https://github.com/advisories/GHSA-2m69-gcr7-jv3q).
Dependency path: API/Operations → Infrastructure → Microsoft.EntityFrameworkCore.Sqlite **8.0.23**
→ SQLitePCLRaw.bundle_e_sqlite3 **2.1.6** → native lib **2.1.6**. This is the running database engine,
also used by migration and offline recovery tooling, not merely a development advisory. HRFlow does
not accept arbitrary SQL through its APIs, but that does not make a native memory-corruption defect safe.

Official guidance recommends SQLite **3.50.2 or newer**; advisory metadata lists affected package
versions through **2.1.11** and still says “None” under patched versions. The current
[official NuGet 2.1.13 package](https://www.nuget.org/packages/SQLitePCLRaw.lib.e_sqlite3/2.1.13)
is available on the same 2.x line (2.1.12 is also outside the published affected range).
Only this native package is pinned directly to **2.1.13** in Infrastructure; EF Core, managed
SQLitePCLRaw core/provider/bundle and existing application packages are unchanged. No forced upgrade,
provider switch or unnecessary override. Calling `sqlite3_libversion` on the DLL actually shipped
with the rebuilt API reported **3.53.3**, above the recommended fixed engine level. Windows migration,
two-process transactions, scanner and attachment recovery reruns establish exercised compatibility;
Linux deployment remains unverified.

After: solution-wide `dotnet list HRFlow.sln package --vulnerable --include-transitive` reported
**no vulnerable packages for Application, Infrastructure, API or Operations**, but still reported a
separate **High Microsoft.Extensions.Caching.Memory 8.0.0** advisory in the Domain project:
[GHSA-qj66-m88j-hmgj / CVE-2024-43483](https://github.com/advisories/GHSA-qj66-m88j-hmgj).
Its path is Domain → Microsoft.AspNetCore.Identity.EntityFrameworkCore 8.0.0 → EF Relational 8.0.0
→ EF Core 8.0.0 → Caching.Memory 8.0.0. Application/runtime projects resolve the later dependency
graph without this finding. This project-level exposure remains: review the Domain dependency and
apply a focused patched Memory version (8.0.1+ per the advisory) in a separately scoped change rather
than assume every consumer is safe. **The solution audit is not clean.**

## Explicitly unexecuted / remaining limits

- Linux/native decoder/daemon deployment, realistic hostile load, full malformed-file/malware corpus,
  signature-freshness operations and production scanner/storage supervision.
- Pending-activation/stale credential-version document-specific matrix, role revocation precisely
  during scanner or writer contention, and full deactivation-versus-upload/download race matrix.
  Existing current-account/API/transaction guards are reused; source inspection is not these live tests.
- Actual process termination at each transfer/promotion boundary, power loss, disk exhaustion, OS
  ACL-denial/file-locking scenarios and
  two-process cleanup-worker scheduling/failure matrix. Restart of aged interrupted records was executed.
- Starting the restored API and re-provisioning access to download quarantine, full restored role/login
  matrix and Windows-to-Linux recovery. Preserved metadata/blob comparisons and operator invalidation
  were executed; earlier #82 evidence is not described as newly rerun.
- Full screen-reader/contrast assessment, exhaustive light/dark workflows and live HR employee/policy/
  monitoring/report regressions. Submission/history, timeline, download and eligible decision paths
  were exercised; broader regression matrices remain outstanding.
- Finer HR medical-access groups, automatic classification, configurable evidence requirements (#84),
  images (#95), approved bound-evidence retention/hold administration and large-store recovery.
  User-selected Ordinary can misclassify medical content; default Medical and explanations mitigate
  but do not eliminate that risk. Bound evidence is not automatically purged; storage growth is real.
- An already authorised download can finish after a later permission change commits. The protected
  authorization/access record precedes streaming; no claim of interrupting an already-open remote download.

See [ADR 0015](../adr/0015-private-supporting-documents.md) and [scanner/runbook](../operations/supporting-documents.md).
