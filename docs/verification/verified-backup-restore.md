# Verified backup/restore: executed local evidence

9 October 2026. Branch `feat/verified-backup-restore`, base
`68c0bd70d07b87944e92c0b4531b7308e3770ddf` (merged PR #107).
Disposable synthetic databases, restricted private temporary storage and real Production-mode
loopback TLS API processes were used. No real invitations, recipients or deployment. Temporary
verification code/data/logs are excluded from the review diff. No frontend source changed.

## Completed HTTP and persisted-row drill

- Backup refused with a running named API while a concurrent real submission succeeded normally.
- After stopping writers, SQLite-aware database/private-file backup and isolated restore succeeded.
- Compared every persisted table at the cutoff, including Identity/roles, profiles/lifecycle/versions,
  requests/audits, account settings, notification events and inbox. All matched except documented
  password/version/stamp/invitation/refresh invalidation. The synthetic private file matched exactly.
- One real owner cancellation committed after the cutoff. Restored request remained Pending rather
  than fabricating the lost cancellation or audit.
- With the original signing configuration, old access/refresh tokens, invitation and restored old
  passwords failed. Inactive and pending accounts remained denied.
- Explicit offline password recovery required permission-review confirmation, used Identity validators
  and worked for active activated Employee, Manager, HR-only and combined-role accounts. Inactive
  and pending accounts could not be recovered this way.
- Real restored API checks: Employee HR-directory denial; HR-only personal/approval denial;
  unrelated Manager decision denial; assigned combined-role manager approval succeeded. History,
  balances and timeline remained accessible in their authorised scopes.
- Persisted approval and notifications were inspected. Worker startup and a second restart produced
  no duplicate notifications; record preservation survived restart.

Measured final small synthetic rehearsal: backup **1.470 seconds**, isolated validated/security-invalidated
restore **0.922 seconds**; inventory 2 files / 274,489 bytes. Snapshot cutoff
`2026-10-09T08:48:21.9407093Z`; post-cutoff cancellation completed at
`2026-10-09T08:48:24.520Z`. Actual unrecovered window **2.580 seconds**, one committed cancellation
(and its associated transition effects) lost from the restored cutoff. These command timings are not
end-to-end outage RTO, do not include permission reconciliation/operator response, and are not load
benchmarks or a guarantee of zero RPO.

## Negative and interruption checks

Wrong/missing keys, corrupt/truncated ciphertext, omitted security-invalidation acknowledgement,
existing restore/key output and repository-root output were rejected without replacing existing data.
Authenticated malformed-package checks separately exercise missing inventory files, checksum mismatch,
incompatible migration list, traversal/device-alias paths, physical schema mismatch, missing index,
foreign-key violation, invalid SQLite data and duplicate entries. Failed restores do not publish a destination.

- Killed backup and restore processes during a 64 MiB synthetic-file rehearsal. No destination/package
  was published; marked restricted plaintext staging remained and explicit cleanup succeeded.
  Cleanup refused a published restore. Existing databases remained available.
- Empty private inventory backup succeeded. An independent process holding SQLite `BEGIN IMMEDIATE`
  caused backup refusal in **4,198 ms** (includes OS process inventory); no package was published.
- A separate process holding the exclusive maintenance lease prevented actual Production-mode API
  startup. A junction-based private root was rejected before creating an output package.
- Solution build passed with **0 warnings / 0 errors**. Temporary Node verification emitted its
  experimental built-in SQLite warning; no runtime dependency was added to HRFlow.
  These are HTTP/operator checks, not static rendering or interactive browser verification.
- EF model check: `No changes have been made to the model since the last migration.` No migration
  is required. Git whitespace checks include new source/documentation files; instruction copies match.

## Limitations and unexecuted checks

Finalisation review: source, manifest/crypto boundaries, notification recipient persistence and
current-scope inbox projection were inspected. Earlier HTTP/recovery measurements above were not
rerun for finalisation. Staged whitespace, instruction-byte equality and sensitive-artifact review
are finalisation checks, not new recovery/browser evidence. Notification idempotency applies within
one restored database lineage, not across a discarded post-cutoff history.

- Future uploads/quarantine/documents are absent; repeat coordinated file recovery after #83.
- Linux deployment, alternate process supervisors/containers, real service-stop integration, symlink
  race resistance against a malicious local administrator and sudden-power-loss durability unverified.
- No real recipient delivery, Production activation provider, first-admin provisioning, trusted-proxy
  integration, hosting, automatic schedules/retention, legal-hold workflow or realistic load benchmark.
- Exact self/cross-department decision regression matrix, post-recovery password-change race matrix
  and interactive browser workflows were not rerun. Existing authorization was not redesigned.
- Frontend build/lint not executed: no frontend files changed. No dependency versions/packages added;
  dependency audit not required/rerun. No new automated test files/framework.
- Restored post-cutoff roles/deactivations must be reconciled externally before recovery access.
  Removing old password hashes intentionally requires reviewed recovery; current active eligibility
  alone is insufficient evidence of post-cutoff permissions. Original backup remains encrypted and
  contains cutoff security state; never bypass the mandatory invalidation by extracting it manually.
