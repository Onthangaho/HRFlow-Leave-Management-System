# ADR 0015: private evidence, quarantine and scoped content (#83)

Status: implemented locally; independent review required. 9 October 2026.

Use the existing restricted `Storage:PrivateFilesRoot`, never public/static roots. Application owns
the draft/binding/access lifecycle; Infrastructure supplies local objects, PDF/image parsers and
the real loopback ClamAV adapter. Domain evidence records have no framework dependencies. No
profile images or configurable evidence requirements are included (#95/#84 remain separate).

## Classification and permissions

Classification is explicitly Medical or Ordinary; default Medical. Leave-type names do not classify
content. User selection cannot establish whether a document contains health information: users must
choose Medical whenever uncertain. Misclassification can expose content to an eligible Manager.
This limitation needs HR training/product review; no automatic classification/compliance claim.

Current active, activated credentials and current Identity membership are authoritative. Owners need
Employee/Manager capability for drafts and submission. Current HR Administrator membership is the
explicit organisation-wide medical-content capability in this slice; finer HR medical-reader groups
are not implemented. Ordinary bound content is available to its owner, current same-department
direct-report Manager and current HR. Manager-only medical readers get class/status/version/ID,
without size, MIME, filename, thumbnail, download URL or content. Combined HR/Manager capabilities
are additive. Missing/out-of-scope objects return the same 404. Account revocation remains enforced
at the API gate and protected transactions. A read snapshot/in-flight authorised download has a
defined cutoff; revocation does not retract bytes already authorised/transferred.

## Durable lifecycle and file/database boundary

Generated document ID is also the storage identifier; no supplied path or original filename is saved.
Owner/upload-key uniqueness makes retry account-scoped and non-overwriting. Twenty unfinished drafts
per owner bound storage growth; five distinct clean owned drafts may bind to one new request.
Receiving metadata is committed before transfer. Bounded content validation and quarantine writes
occur outside the writer reservation. Quarantined → Scanning → Clean/Rejected/ScanUnavailable uses
short protected reads/writes; only exact ClamAV `stream: OK` permits a clean copy. No clean substitute.
Unavailable/timeout/ambiguous/cancelled scans never bind. Explicit scan retry reuses immutable quarantine.
Submission binds evidence inside its existing transaction with its single Submit audit. Failed
binding/policy/status operations leave neither a new request nor a duplicate leave transition audit.

Scanning/promotion use one bounded immutable snapshot whose size/SHA-256 matches persisted facts.
Clean binding and download also verify bytes outside writer protection. The subsequent protected
operation rechecks the original metadata version and current authorization; downloads return only
the verified snapshot. This closes path-reopen substitution without holding the SQLite reservation
for file transfer/hash work. Restricted private-store permissions remain mandatory. A changed or
missing blob cannot produce an unchecked download; unavailable content keeps its history metadata.

Files and SQLite are not atomic. Partial/interrupted transfers retain Receiving metadata; promotion
may leave an inaccessible extra clean copy if final database commit fails. Removed drafts remain
metadata-backed until idempotent deletion completes. The worker processes 20 eligible drafts every
five minutes; Receiving/Scanning older than one hour and other unbound drafts older than seven days
expire. These are technical abandoned-draft limits, **not statutory retention periods**. A durable
BlobCleanup records a completed cleanup; it is never a permanent exclusion from reconciliation.
The worker separately expires at most twenty eligible drafts and revisits a round-robin page of
twenty Removed records every five minutes, including historically acknowledged records. Failed
physical deletion remains retryable without blocking the rest of that removed-page batch.
Bound evidence is never automatically removed or expired. Legal holds therefore retain content;
an approved retention/hold administration workflow remains outstanding. Metadata and request/audit
history survive removal, and missing authorised content is marked unavailable rather than rewriting
a leave decision. A missing required blob also prevents a successful backup/restore declaration.

### Publication/removal correction (draft PR #109)

Before publishing staging/quarantine or clean content, Infrastructure takes the document's persistent
OS lock file with `FileShare.None` and checks its removal tombstone under that same lock. Upload bytes
are received/validated first; scanning runs before clean publication. Neither transfers, scans nor
file-lock waits run under SQLite writer protection. Removal first commits terminal Removed metadata,
then takes the same file lock, flushes a persistent tombstone and deletes staging/quarantine/clean
content. Only successful physical cleanup may be acknowledged. A publisher that wins before physical
removal is compensated by removal; a publisher resumed after cleanup sees the tombstone and cannot
recreate content. Final metadata still rechecks lifecycle under the SQLite reservation.

OS contention has a bounded three-second wait; failure retains inaccessible Removed/Receiving/Scanning
metadata for reconciliation. No business transaction callback is replayed. Restart releases abandoned
OS handles; persistent locks/tombstones are never deleted or reused. Empty coordination files contain
no medical payload and remain in private backups. They add persistent filesystem/storage overhead.
These guarantees require all publishers/removers to use the protocol on the same locking-compatible
local store. Stop all old API/worker processes before upgrading; mixed-version writers are unsupported.
Windows separate-process interleavings were exercised; Linux/filesystem/load and power-loss evidence
remain outstanding. See [FileShare semantics](https://learn.microsoft.com/dotnet/api/system.io.fileshare).

Existing AuditEntry remains the sole leave-transition mechanism. DocumentAccessEntry separately
records content Download/Remove/ContentUnavailable and system DraftExpiry/BlobCleanup; it does not
fabricate old/new leave states. System actions have no invented human actor. Denied operations use
existing safe request/action/actor/correlation logging. Neither original filenames nor content,
diagnoses or scanner signature text are logged/returned. Scanned/bound/removed UTC timestamps and
independent versions preserve provenance. Legacy requests get no invented documents.

## Content, UI and recovery

PDFsharp parses bounded, complete, nonempty PDFs (up to 500 pages); SkiaSharp decodes single-frame
JPEG/PNG with signature/end-marker/MIME/extension checks and a 20-million-pixel bound. Parsers and
antivirus reduce risks; they do not certify all files safe. PDFs and images are download-only using
authenticated bytes, generic filenames, `nosniff` and `no-store` headers. No public storage URLs or
static serving; temporary browser blob URLs are created only after authorised downloads and revoked.
Keep parser/scanner/signature updates maintained. Platform-native decoder and Linux deployment need
their own operational verification.

The request form uploads owned drafts, reports progress/cancellation/per-file status, retries scans
explicitly, selects clean files and removes eligible drafts. Authorised request history renders the
same evidence panel. All operations use account/session-scoped state and cancellation; mutations
are non-replayable. No medical certificate is automatically required. UI cannot confer access.

The #82 operator package now checks document metadata against required clean/quarantine blobs and
hashes before backup and isolated restoration. Stopped API/worker/file writers are still mandatory.
Scanner verdict/lifecycle metadata survives restoration; quarantine remains inaccessible. Retained
clean plus quarantine copies increase backup storage; the existing 100 MiB package bound may reject
larger stores. Plan capacity/retention before deployment; this is not a large-store recovery solution.

Migration adds only SupportingDocuments/DocumentAccessEntries, ownership/request restrictive FKs,
owner/upload-key uniqueness and version metadata. Existing requests/Identity/history are not modified.
Rollback drops evidence metadata/access records **without deleting private blobs**; do not roll back
after collecting evidence without a protected export/recovery/retention plan. Re-upgrade creates empty
tables and cannot reconstruct lost evidence provenance from blobs. No schema repair or automatic backfill.

See [scanner setup and contracts](../operations/supporting-documents.md) and
[executed verification/limits](../verification/private-supporting-documents.md).
