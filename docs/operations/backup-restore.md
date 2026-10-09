# Operator backup and verified recovery (#82)

This is an offline operator tool, not an HR API or dashboard control. It supports one organisation
on one Windows/Linux host with persistent local SQLite and local private storage. Network shares,
multi-host writers, container mount aliases and mixed application versions are unsupported. Windows
was rehearsed; Linux process inventory and permissions require a separate drill before use.

## Preconditions and custody

Publish/build the matching application and tool together:

```powershell
dotnet build HRFlow.sln
dotnet publish tools/HRFlow.Operations -c Release -o $ToolDirectory
```

All variables below are operator-supplied **absolute paths**, not committed configuration. Create
existing parent directories outside the checkout, application/binary roots and every public/static
root. On Windows restrict inherited ACLs to the operator, SYSTEM and Administrators; on Linux use
owner-only directories (`0700`) and files (`0600`). The tool rejects broad access, network locations,
reparse points/symlinks, collisions and existing outputs. The supplied `$ApplicationRoot` must be
the actual application root, not an unrelated directory. Operators must also exclude any separately
configured public root. Do not run the application against a staging directory.

Keep encryption keys separately secured from packages, with independently tested recovery access.
Protect the original database, private roots, temporary plaintext staging, password files and logs.
Do not print/decrypt manifests into reports: filenames themselves may contain personnel information.
The tool emits only outcome categories, UTC times and aggregate sizes/counts.

```powershell
dotnet $ToolDll keygen --application-root $ApplicationRoot --output $KeyFile
```

This generates 32 random bytes. Never use a human password as the key. Each package uses the
established .NET `AesGcm` AES-256-GCM implementation, random 96-bit nonce, 128-bit authentication tag
and authenticated format header. Rotate keys under an operator custody policy and retain the key
for every retained package. Losing it makes recovery impossible. Application signing keys, delivery
secrets, certificates and configuration are **not** bundled; custody and rotation are separate.

## Stop and prove quiescence

1. Disable service/task/container autorestarts and stop **every** API/notification worker on the host.
   Workers currently run inside the API. Verify the OS process inventory and service supervisor.
2. Stop external private-file writers too. An in-process lock cannot establish this condition.
3. Run the backup. Named API process inventory fails closed. Updated API processes hold shared OS
   leases on `<database>.operations-lock`; the operator takes an exclusive lease and holds it until
   capture completes. Do not delete/replace that sidecar or access the database through aliases.
4. SQLite `BEGIN IMMEDIATE` also reserves the database writer before the snapshot cutoff and file
   capture. An independent database writer causes bounded failure (three-second SQLite timeout);
   there is no transaction callback replay. Private-file consistency still requires step 2.
5. Only restart after the command exits. Old/renamed binaries, inaccessible process namespaces and
   noncooperating file writers are unsupported; the lease is not a substitute for stopping services.

```powershell
dotnet $ToolDll backup --application-root $ApplicationRoot --database $Database `
  --private-root $PrivateRoot --output $NewPackage --key-file $KeyFile
```

The database is captured with Microsoft.Data.Sqlite `BackupDatabase`, never by copying a live main
file without its WAL. A manifest inside the encrypted package records format version, application/tool
identifier, exact EF migration identifiers, UTC cutoff/creation, file inventory, sizes and SHA-256
checksums. Private files are included under their relative paths. No secret configuration is copied.

Limits: 100 MiB plaintext archive, 1,000 inventory files; archive overhead counts toward the byte
limit. This small deployment slice uses whole-buffer authenticated encryption and needs several
times the package size in memory plus private staging space. It is not streaming/large-store tooling.
Private uploads are not implemented. Synthetic existing-file and empty-inventory checks **do not**
prove medical-document, quarantine or attachment recovery. Repeat this drill after #83.

## Restore order and security invalidation

Stop writers and disable delivery while rehearsing. Restore only to a **new isolated directory**:

```powershell
dotnet $ToolDll restore --application-root $ApplicationRoot --package $Package `
  --destination $NewDestination --key-file $KeyFile --invalidate-security
```

Authentication happens before extraction. The tool validates archive limits/paths/duplicates/links,
manifest and inventory, checksums, exact migration compatibility, physical model table/column types,
nullability, primary/foreign keys and index definitions,
SQLite integrity and foreign keys. It does not migrate, repair roles, reset lifecycle, invent audits
or replace any existing database/files. Validated data is published by a sibling directory move;
`database.sqlite`, `private/` and a safe `recovery-summary.json` are the resulting layout. Use the
matching application build, not an older/newer migration set. Matching column metadata is not proof
against every possible hostile SQLite schema alteration; packages and keys need trusted custody.

A backup may resurrect old passwords, sessions, invitations, roles or deactivations. Historical
revocations **do not automatically survive**. Therefore restoration always clears all password hashes,
rotates every credential version/security stamp, revokes outstanding refresh tokens and clears
activation token hashes/expiry. Pending activation remains pending (delivery state becomes Expired);
inactive remains inactive. Profiles, roles, versions, employment lifecycle, requests, audits, settings,
notification events/inbox and historical decisions otherwise remain unchanged. Security-field changes
are deliberate recovery deltas, not an exact byte-for-byte database restore.

Before granting access, reconcile post-cutoff lifecycle, roles and reporting changes from separately
secured operational records. **Do not trust restored eligibility without review.** Start no real
notification/delivery worker until that reconciliation is complete. Consider rotating separately held
JWT signing material as defence in depth; credential-version checks already deny old access tokens.

Notification recovery keeps event-time recipient IDs, acknowledgement state and event/recipient
uniqueness. Restarting the worker does not retarget events to different accounts; existing inbox
projection rechecks current request scope and hides unavailable details. Delivery is in-app only;
no email/SMS provider is invoked. The rehearsal used synthetic recipients. A restore does not provide
exactly-once delivery across divergent database histories: an event acknowledged after the cutoff
may be pending again in the recovered lineage. Never run the original and recovered stores as parallel
production systems, and reconcile post-cutoff permissions before starting recovered workers.

For an explicitly reviewed active, activated account with no established password, while services
remain stopped, prepare an owner-only password file and use Identity's configured validators:

```powershell
dotnet $ToolDll recover-password --application-root $ApplicationRoot --database $RestoredDatabase `
  --identity $ReviewedIdentityId --password-file $PrivatePasswordFile --confirm-current-permissions
```

The identity argument is an offline operator selection, not a public API. This command cannot change
roles, reactivate an employee, activate a pending account or overwrite an established password. It
sets the reviewed new password atomically and rotates credentials again. Destroy the temporary
password file safely under the custody policy; do not put it in shell history or reports. Pending
invitation recovery requires a separately approved delivery flow; Production delivery is still absent.

## Interruptions, retention and rollback

Ordinary failures clean only tool-owned staging. A killed process/power interruption may leave
private plaintext under `hrflow-backup-<GUID>` or `hrflow-restore-<GUID>`. Stop any process using it,
verify the directory is a staging artifact, then explicitly clean it:

```powershell
dotnet $ToolDll cleanup-staging --application-root $ApplicationRoot --staging $InterruptedStage
```

Only the strict GUID staging name and ownership marker are accepted; published restore destinations
cannot use staging prefixes. This is not a general deletion tool. It does not securely erase storage
media. Never manually remove sidecars while processes are running. Interrupted publication durability
after sudden power loss has not been verified; retain the authenticated source package and retry into
another new directory. Existing sources/destinations remain untouched.

No scheduled backup, retention cleanup, legal-hold automation or cloud service is provided. Define
retention/access/legal holds with HR/privacy advisers; separately manage keys and protected logs.
Keep verified backups before migrations. Rolling back to an earlier package loses post-cutoff work
and requires security reconciliation/invalidation again; never silently combine divergent databases.

Proposed planning targets (not guarantees): daily backups for a maximum 24-hour scheduling RPO and
four-hour operator recovery RTO, subject to realistic rehearsals, monitoring and custody availability.
See [measured local results and outstanding checks](../verification/verified-backup-restore.md).
First-admin provisioning, Production invitations, trusted-proxy integration, hosting and load testing
remain independent deployment gates.

References checked 9 October 2026: [Microsoft SQLite backup API](https://learn.microsoft.com/dotnet/standard/data/sqlite/backup),
[SQLite online backup semantics](https://www.sqlite.org/c3ref/backup_finish.html),
[.NET authenticated AES-GCM](https://learn.microsoft.com/dotnet/api/system.security.cryptography.aesgcm.encrypt?view=net-8.0).
