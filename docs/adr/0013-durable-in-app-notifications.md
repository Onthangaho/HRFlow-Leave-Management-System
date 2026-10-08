# Durable in-app leave notifications (issue #93)

## Recipient contract

Recipients are resolved from current active, activated Identity accounts inside the originating writer reservation. Combined roles do not multiply delivery and actors never notify themselves.

| Committed event | Event-time recipient | Current read rule |
| --- | --- | --- |
| Submission | Eligible assigned same-department Manager | Current Manager/report relationship or another independently valid timeline capability |
| Approval / rejection | Request owner with personal capability | Current personal capability and ownership |
| Owner cancellation | Eligible current same-department Manager | Current timeline scope |
| HR deactivation cancellation | Eligible current same-department Manager; inactive owner receives nothing | Current timeline scope; no raw reason |
| Manager reassignment | Newly eligible Manager, once per existing Pending request | Current relationship; previous recipients lose details and links |

The administrative outcome included here is reassignment of Pending work. Employee creation, profile edits, role edits, activation delivery and policy CRUD do not broadcast notifications to HR. HR deactivation's operational outcome is represented by each cancelled-request event to its eligible Manager. The initiating HR account already receives the authoritative operation result.

## Persistence and access design

AuditEntry remains the sole audit. Minimal targeted outbox rows (event key, recipient, request ID, category, UTC event time) are saved in the same transaction as the existing transition/audit or reassignment. No historical backfill. A worker delivers bounded batches in short SQLite writer reservations, with uniqueness on event/recipient identity. Creation and processed marking commit together. Delays occur outside reservations; restart and competing workers safely retry undelivered rows.

Inbox reads, unread count and mark-read recheck active/activated credentials and current roles within the existing deferred snapshot or writer reservation. A lost request scope returns a generic unavailable notification without a request ID, employee details or link. Unavailable unread items remain included in the count until marked read. Payloads never contain names, emails, notes, medical details, deactivation reasons or correlation tokens. Deep links use the existing independently authorised timeline.

No automatic retention deletion ships here: records remain until an explicitly reviewed retention policy is implemented. Polling and batches are bounded, not database growth; operators must monitor storage. Preferences are issue #94, not an inactive control in #93. No email/SMS.

## API and processing contract

- GET /api/v1/notifications?filter=all|read|unread&page=1&pageSize=20: own page, total and safe items. Page 1–10000, pageSize 1–50. Descending original UTC timestamp then notification ID.
- GET /api/v1/notifications/unread-count: unreadCount includes generic unavailable items.
- PATCH /api/v1/notifications/{id}/read: 204; idempotent first read time. Other recipients and missing IDs both 404. Invalid page/filter 400; revoked role 403, invalid credentials/lifecycle 401, expected write contention 409.
- Controller derives the Identity actor; Application contract delegates to scoped Infrastructure services. Deferred snapshots include credential/current-role validation and projection. Mark-read uses the same three-second writer reservation and post-reservation credential validation as other protected writes.
- Batches contain at most 20 due events. Each delivery is a separate short writer transaction, without automatic callback replay. Five-second polling and persisted 30-second failure delays occur outside reservations. A failed item is skipped until due, letting other work progress. Competing workers re-read delivered state under protection and database event/recipient uniqueness is the final duplicate safeguard.
- Logs contain event IDs and exception type categories only, never source payloads. No external work is performed by this in-app worker.
- The UI polls every 30 seconds while authenticated and visible. Query keys include account/session/filter/page; request cancellation and existing epoch guards reject old completions. Mark-read is explicitly non-replayable; local busy protection prevents duplicate clicks.

## Migration and rollback

AddLeaveNotifications adds two tables and indexes, plus RESTRICT source/notification references. Existing requests, audits, employees and Identity fields are untouched; no backfill. Down drops the notification tables and therefore loses new inbox/read/outbox data; leave and audit history remain unchanged. Take backups and stop writers during migration as in existing deployment guidance.

Design was recorded before implementation. See [executed verification and limits](../verification/in-app-leave-notifications.md). Independent review remains outstanding; no deployment claim.
