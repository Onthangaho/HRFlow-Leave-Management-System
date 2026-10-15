# Offline first administrator — issue #81 slice

This is an operator command, not public registration, account recovery or a second-admin tool.
Use the reviewed build on the supported single-host, local SQLite filesystem. Stop **every**
HRFlow API/worker process first. Process inventory and the cross-process exclusive maintenance
lease fail closed; a SQLite immediate transaction then reserves the writer before schema,
empty-store, role and department reads. There is no callback replay. Other software that ignores
the maintenance lease must also be stopped. The command does not kill services for you.

## Prepare the store and credential

Use an existing, explicitly selected database with exactly the build's migrations applied.
The command never creates/migrates a database. Review [configuration](single-host-configuration.md)
and [backup/recovery](backup-restore.md) before operating on a store. Provision directories and
files outside source, application, static and publish roots, with owner-only access (Unix 0700
directories / 0600 files; Windows owner, SYSTEM and Administrators only). No network paths,
symlinks/reparse points or permissive immediate parent are accepted. Do not rely on umask.

From the repository, using privately assigned absolute paths (the variable names below contain
no secret values):

```powershell
dotnet build HRFlow.sln
$env:ConnectionStrings__DefaultConnection = "Data Source=$Database;Mode=ReadWriteCreate"
dotnet ef database update --project src/HRFlow.Infrastructure --startup-project src/HRFlow.Api
Remove-Item Env:ConnectionStrings__DefaultConnection
```

Create the empty database file with restricted permissions **before** migration; inspect database,
parent and any SQLite sidecar permissions afterward. Use OS administration tools (`icacls` on
Windows; `install -d -m 700`, `install -m 600 /dev/null` and `chmod` on Unix) appropriate to the
service/operator identity. Never broaden permissions to make validation pass.

Supply a privately generated/password-manager-held initial password in an owner-only UTF-8 file,
1–1024 bytes and at most 256 characters after trailing CR/LF removal. Embedded newlines are
rejected. Identity's configured validators are authoritative. Do not put the password in command
arguments, environment variables, shell history, terminal output, logs or repository files.
Choose a unique credential and deliver it to the authorised administrator through a separate
approved private channel. This establishes a credential directly; it is not an invitation or
proof that the email address was verified.

## Provision exactly once

```powershell
dotnet tools/HRFlow.Operations/bin/Debug/net8.0/HRFlow.Operations.dll first-admin `
  --application-root $RepositoryRoot --database $Database --password-file $PasswordFile `
  --name $ConfirmedName --email $ConfirmedEmail --employee-number $ConfirmedNumber `
  --employment-start-date $ConfirmedIsoDate --create-department $ExplicitDepartmentName
```

Instead of `--create-department`, use `--department-id $ExistingDepartmentGuid` to select one
existing department by its unique ID. Exactly one option is required. Creation trims a 1–100
character name, rejects control characters and existing trim/case-insensitive name collisions;
it never silently selects a seeded department. Name/email and employment facts use the existing
domain and Identity validation. Numbers normalize to uppercase ASCII, 1–32 characters with
letters/digits/underscore/hyphen and an alphanumeric first character. Dates use `yyyy-MM-dd`,
1900-01-01 through 2100-12-31. A future employment date **does not defer activated-account access**.

Any Identity user or employee profile prevents provisioning, including unlinked, inactive or
pending records. Existing roles must be exact canonical Employee, Manager or HR Administrator
records with matching Identity-normalized names and no duplicates; unknown/conflicting records
are refused, never repaired. Missing canonical roles are created, but membership is **HR
Administrator only**. Department, roles, Identity password/account, membership and active profile
commit together. The account is activated, email remains unconfirmed, manager and schedule remain
Unknown, and no leave audit is invented. Identity/profile failures roll everything back.

Exit 0 with `completed` means commit returned successfully; exit 1 has a safe refusal category,
never input/password/path/Identity error payloads. Do not automatically retry contention or an
interrupted command. An uncertain completion may already have committed: keep services stopped,
inspect the selected store privately with trusted SQLite tooling (user/profile/member counts and
IDs), and verify the expected linked HR-only account before normal login. Never delete records
to force a second run. A repeated invocation refuses without changing them.

## Login, custody and recovery

Configure Production using the [placeholder template](production-environment.example.ps1),
independent strong secrets, trusted HTTPS origin/certificate, allowed hosts and persistent private
paths; do **not** set Development seeding or activation pickup. Runtime connection uses
`Mode=ReadWrite`. After privately configuring the template variables and certificate:

```powershell
$env:ASPNETCORE_ENVIRONMENT = 'Production'
dotnet src/HRFlow.Api/bin/Debug/net8.0/HRFlow.Api.dll
```

Use the existing same-origin SPA/TLS configuration from the configuration runbook; separate origins
require explicit trusted HTTPS CORS entries, not a relaxed wildcard. Use normal Login with the privately
held credential. HR-only can administer employees but has no personal leave or decision capability.
Change password through Account when appropriate; its existing all-session revocation applies.
Local retained verification uses HTTPS HTTP requests; target-host/browser/proxy deployment is a
separate gate. Ordinary Production employee creation/resend still requires an approved delivery
provider; this explicit offline credential does not enable pickup or bypass that gate.

Delete the temporary password file when custody is transferred and login is verified:

```powershell
Remove-Item -LiteralPath $PasswordFile
```

Deletion is **not** a claim of secure media erasure; consider filesystem snapshots, backups, SSDs
and endpoint policies. For loss of access use the documented [isolated recovery procedure](backup-restore.md),
including security invalidation and reviewed current permissions. `first-admin` cannot reset or
repair an existing account, restore roles, reactivate staff, or create another administrator.
For an eligible password-cleared identity in an isolated restored store, only after reviewing the
recovery runbook and current roles/reporting, the separate command is:

```powershell
dotnet tools/HRFlow.Operations/bin/Debug/net8.0/HRFlow.Operations.dll recover-password `
  --application-root $RepositoryRoot --database $RestoredDatabase --identity $ReviewedIdentityGuid `
  --password-file $NewPrivatePasswordFile --confirm-current-permissions
```

No schema changes or migration rollback exist for this slice; removing its code does not remove
the created account. A pre-provision backup restores the prior empty store only through the normal
isolated restore procedure, never an automatic destructive undo.

Production invitation delivery, target-host TLS/proxy/storage, backup drills/load and qualified
legal/calculation review remain outstanding. This is a locally verified bootstrap capability,
not a production-readiness claim.
