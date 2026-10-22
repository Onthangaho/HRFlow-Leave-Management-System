# 0032 — Owner supplemental evidence for captured Pending requirements

Status: implemented locally for review, 22 October 2026. Existing #83/#84 slice authorised after
merged PR #134. No legal, retention, payment or production-delivery approval is implied.

## Contract

`GET /api/v1/documents/requests/{requestId}/additions` returns current eligibility/explanation,
captured evidence class, total bound count and separately ordered addition history. Existing owner,
eligible same-department Manager and HR read scope applies. Records expose safe IDs/counts/UTC times,
never filenames, diagnoses, private paths or document content. Medical content remains owner/current
authorised HR only; Manager-only access remains status-only. Combined owner capabilities are additive.

`POST` on the same resource accepts `{ key, documents: [{ id, version }] }`. The server derives the
actor. One to five distinct nonempty document IDs/versions and a nonempty GUID key are required.
The JSON body is capped at 4 KiB. The existing exception filter now maps a demonstrated Kestrel
request-too-large rejection to fixed safe HTTP 413 rather than mislabelling it HTTP 500.
Only a current active/activated Employee or Manager owner may mutate. HR-only cannot attach, and HR
cannot act for another owner. Normal API and transaction credential-version checks remain in force.

Only captured Optional/Required requirements permit additions while Pending. Unknown legacy,
NotRequested, unsupported class and Required Medical snapshots refuse new additions with explanations.
Later type edits are irrelevant. This does not remove the existing submission-time ordinary Required
gate or implement late medical proof/payment adjudication. No dates, descriptions or snapshots change.

## Atomicity, concurrency and recovery

The existing verified-content reader checks size/checksum/publication outside SQLite writer protection.
Within the existing cross-process writer reservation the service rechecks current credentials, personal
role, ownership, Pending state, captured requirements, clean/unbound metadata and original versions.
Existing bound plus distinct new files must total at most five. Draft upload does not reserve a slot;
an excessive selection is rejected in full. Nothing silently truncates.

The writer protocol serializes against approval, rejection, owner cancellation and HR deactivation.
If a transition wins, a new addition is refused without binding; if the addition wins, later transitions
retain its trace and apply their normal lifecycle/access rules. Recoverable failures retain drafts.
Transfers/scanning never run in this reservation. Existing publication/removal fences remain unchanged.

`EvidenceAdditions` stores a unique `(ActorId, RequestId, Key)` plus a canonical, sorted bounded
ID/version selection and UTC timestamp. Binding, one addition outcome and per-document
`SupplementalBind` access entries commit together. No leave-state audit or notification is invented.
Authorised history labels these as later additions, separate from the original submission/lifecycle.

An explicit identical-key recovery rechecks current access and returns the committed outcome even
after a later terminal transition. It never rebinds. A changed ID/version selection conflicts; order
alone is immaterial. A concurrent identical request that observes already-bound bytes checks its durable
outcome again under the writer gate. Revocation/inactivity still denies recovery. There is no automatic
transaction callback, authentication, network or session-replacement replay.

## Client

Personal history links lead to the existing detail page. Current server eligibility controls the
owner action there; HR/Manager history does not acquire mutation authority. Existing draft upload,
scan retry/cancel, download and selection components are reused. The original selected versions are
retained. Once attempted, a selection/key is frozen for explicit recovery; errors retain drafts.
Reviewing another selection is explicit. Success is server-confirmed, not optimistic.
Recoverable eligibility/history refresh errors hide and disable the editor without unmounting its
reviewed selection; explicit retry restores the same visible selection, never hidden bindings.

Reads and invalidation use initiating account/session keys. Unmount aborts binding, and mounted/session
generation checks reject delayed callbacks across logout, another account and same-account relogin.
No storage of drafts/idempotency payload in browser persistence. Refresh eligibility after a conflict;
never change immutable requirements to match current configuration. Existing request summary and
transition history remain read-only.

## Migration and operations

`AddEvidenceAdditions` adds only the new table, foreign keys and unique/indexed lookup. Existing
requests/documents/audits are not backfilled or rewritten. No fake initial/addition history.
Downgrade drops supplemental outcomes/history, losing recovery keys and addition provenance; bound
document metadata and separate access entries survive. A downgrade was not rehearsed. Back up/stop
writers before schema changes and use matching application/operator builds. Re-upgrade cannot
reconstruct lost outcomes; it must not invent history.

Encrypted backup includes the new relational table automatically and continues validating bound
blob checksums/publication markers. Recovery invalidates credentials/invitations as before, not document
history. Stored keys grant no authority without current own-account access. No automatic dispatch,
rebind or schema migration occurs on restoration. Bound deletion/expiry/legal holds remain #83 policy
work; this slice does not delete bound evidence or implement a retention period.

See [verification](../verification/pending-request-evidence.md). Positive ClamAV scanning, controlled
clean fixtures, real HTTP/browser bindings and target-host recovery must be reported separately.
