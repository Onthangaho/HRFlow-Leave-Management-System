# ADR 0025: private self-service profile images (#95)

Status: prepared for independent review; no deployment or production-readiness claim.

## Separate lifecycle and access

`ProfileImages` is independent of supporting documents, request binding, medical classification and
leave audits. Existing users have no image rows and retain initials. All supported roles may manage
their own image. Identity comes from authentication. Current active/activated membership and
credential version are checked at the gate and again inside protected transactions. HR may read
current employee thumbnails; Employee/Manager alone cannot read arbitrary employee images or
mutate another employee's image. HR visibility includes preserved inactive profiles. Missing and
out-of-scope content use the same 404. No image access expands leave decision permissions.

## Processing and scanner cutoff

JPEG/PNG only, at most 2 MiB input, 4096 per dimension and **4,000,000 decoded pixels**. Signature,
format, PNG chunk CRC/ending, single-frame and dimension checks precede full raster allocation.
Optional PNG text/ICC metadata is removed before the native codec sees it, avoiding compressed
metadata expansion. Only pixel/transparency chunks reach the PNG codec. The installed codec does
not expose PNG eXIf orientation: a bounded IFD0 orientation-only reader handles that tag separately
(64 KiB maximum, no nested/GPS/thumbnail traversal). JPEG uses the codec's EXIF origin.
Two RGBA rasters are bounded to 32 MB combined, plus bounded input/output/native codec overhead.
This is a per-operation bound, not a whole-process memory guarantee or throughput/load result.
The existing SkiaSharp dependency decodes completely, applies all eight EXIF orientations,
validates a square crop and re-encodes a fresh 256×256 PNG. No original is stored or served.
Multipart buffering remains in bounded memory (threshold equals the bounded whole request), rather
than spilling original profile-image bytes to ASP.NET temporary files. Original metadata
(EXIF/GPS/comments) is not copied. Crop x/y are fractions of oriented width/height;
size is a fraction of the shorter oriented dimension. The square must fit entirely inside it.

Real ClamAV scans **the exact processed PNG bytes**, not the original upload. Only exact clean
INSTREAM approval permits publication. Original ancillary payloads are discarded rather than
retained; scanning does not claim to detect every exploit or establish original-photo safety.
Outage, timeout, rejection or ambiguous results never create a current image. Image processing and
scanning occur in memory outside SQLite protection. No always-clean fallback or medical quarantine
permissions are reused. Keep codec dependencies/signatures patched; production load/security review
remains necessary. [Codec/origin documentation](https://learn.microsoft.com/en-us/dotnet/api/skiasharp.skcodec)
and [Skia orientation semantics](https://github.com/google/skia/blob/main/include/codec/SkCodec.h)
were checked 14 October 2026.

## Publication and versioning

Register Processing metadata under BEGIN IMMEDIATE before work. Publish generated image IDs into
`profile-images/clean`, with temporary writes in `staging`; `locks` and permanent `removed`
tombstones are separate from documents. Publication/removal share OS exclusive file handles across
API/worker processes. This is the existing supported single-host local-filesystem topology, not
multi-host/network-file storage. Locks are bounded to three seconds; SQLite retains its existing
three-second contention handling and no automatic callback replay.

After publication, a new writer reservation rechecks credentials, roles, ownership, original
version and Processing status. Retire the old current row and switch the new row inside the same
transaction. A filtered unique index permits one Current row per owner. Old-row SaveChanges occurs
inside the still-open transaction before publishing the new relational current pointer; rollback
restores it. A failed replacement retains the previous committed image. Download verifies size and
SHA-256 into a bounded byte snapshot; changed stored bytes never acquire the original clean verdict.

Initial absence version is Guid.Empty. Removal always installs a new, zero-size Current absence
generation, including when no image exists, and retires in-progress operations. This prevents
absence→image→absence ABA and pre-removal uploads from reviving an image. Removal markers make
late filesystem publication fail. Failed compensation leaves durable rows for reconciliation.
The worker expires Processing after one hour and revisits Removed rows in round-robin batches of
20 every minute, including after restart. Historical acknowledgements cannot suppress cleanup.
Large transfers/decoding/scanning never hold SQLite writer protection.

## Client/session and recovery

Profile includes crop sliders usable with arrow keys, progress/cancel, explicit retry/reload,
conflict retention and safe removal confirmation. Original edit versions remain loaded until
explicit reload or a confirmed successful mutation. Cancellation may have an uncertain server
outcome; reload before retrying. Blob queries, callbacks and object URLs are account/session scoped;
URLs are revoked on replacement/unmount/session change. Header updates without reload. HR thumbnails
use the HR-authorised endpoint only. Both image writes are explicitly non-replayable.

Downloads use no-store and nosniff; no public reusable storage URL or static original exists.
Already-authorised in-flight bytes have a read-snapshot cutoff; later revocation cannot retract
bytes already transferred. The image API never logs contents, original filenames or private paths.

Encrypted backup includes the entire image namespace. Current nonempty blobs must match metadata
and have no removed marker. Removed rows require their permanent marker and no clean blob; operators
must finish cleanup before backup if removal is incomplete. Processing rows remain inaccessible and
are compensated after restart. Restore does not claim a pending publication is current. Existing
credential/invitation/refresh invalidation remains mandatory. See the existing recovery runbook.

Migration adds only the new table, restrictive employee FK and filtered index. No legacy photos,
profile history or leave audits are fabricated. Down migration loses image metadata/version history;
private objects remain, inaccessible. Stop writers and use matching backup/tool versions before any
rollback. Re-upgrade begins with initials; it cannot rediscover lost image ownership. Do not delete
document evidence while reconciling image-only leftovers.

Removed image metadata and lock/tombstone files currently remain indefinitely. Old image bytes are
removed, but markers/rows grow with operations. No purge/legal-retention policy is implied; a future
reviewed offline compaction procedure must prove no delayed publisher can reuse removed IDs.
