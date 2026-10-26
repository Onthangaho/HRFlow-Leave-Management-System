# Supporting documents: local scanner and operations

This is a local single-host demonstration, not production-readiness or legal-compliance evidence.
Read [ADR 0015](../adr/0015-private-supporting-documents.md) for access and retention decisions.

## Real ClamAV setup

Install an official supported ClamAV distribution from [the vendor downloads](https://www.clamav.net/downloads).
The Windows rehearsal used ClamAV 1.5.4 and real main/daily/bytecode databases downloaded successfully
by FreshClam. Use private operator-controlled configuration/database/log locations outside the
checkout and every served root. Keep daemon access loopback-only; the protocol itself is not an
authenticated remote service. Never expose port 3310 externally. Review database freshness and
licence/distribution requirements; signature update failure must be operationally visible.

Example `freshclam.conf` (replace the placeholder with the provisioned absolute private directory):

```text
DatabaseDirectory <PRIVATE_DEFINITION_DIRECTORY>
DatabaseMirror database.clamav.net
ConnectTimeout 10
ReceiveTimeout 30
MaxAttempts 1
```

Example `clamd.conf`:

```text
DatabaseDirectory <PRIVATE_DEFINITION_DIRECTORY>
TemporaryDirectory <PRIVATE_SCANNER_TEMP_DIRECTORY>
TCPSocket 3310
TCPAddr 127.0.0.1
Foreground yes
StreamMaxLength 10M
MaxFileSize 10M
MaxScanSize 50M
MaxScanTime 10000
MaxThreads 2
MaxQueue 4
AlertExceedsMax yes
```

Provision that temporary directory with the same private ACL/mode as the definitions. The opt-in
drill below creates its own per-daemon scratch instead; ordinary OS temp inheritance is not sufficient.

Run the real tools, with private logs and service supervision appropriate to the host:

```powershell
& $FreshClamExecutable "--config-file=$FreshClamConfiguration"
& $ClamdExecutable "--config-file=$ClamdConfiguration"
$env:Storage__PrivateFilesRoot = $ProvisionedPrivateRoot
$env:Documents__MaxBytes = '10485760'
$env:Documents__ClamAvPort = '3310'
$env:Documents__ScanTimeoutSeconds = '30'
```

Maximum file limit is configurable downward only (positive, capped at 10 MiB); scanner timeout is
1–60 seconds. Upload/rescan has a 20-per-minute account partition and no queued requests. Five
documents per request and twenty unfinished owner drafts are fixed limits. Multipart transport is
bounded at 11 MiB including overhead. Maintain local disk/memory capacity, daemon queue limits and
updated parsers; no realistic hostile-load claim. Scanner absence, outage, ambiguous/error reply or
timeout yields inaccessible ScanUnavailable, never Clean. Retry explicitly after repairing scanner
health. The adapter uses [official INSTREAM framing](https://docs.clamav.net/manual/Usage/ClamdProtocol.html)
with bounded response length and only exact positive verdicts. No external delivery is performed.

## API contract

- `POST /api/v1/documents`: multipart file, uploadKey GUID and Medical/Ordinary classification; current
  personal account is derived server-side. Returns generated ID, status, version and scoped content
  information. Never supply employee identity/storage path. Same owner/key cannot overwrite content.
- `GET /api/v1/documents`: owned drafts. `?requestId=...`: currently authorised request evidence;
  Manager medical content fields are redacted.
- `POST /api/v1/documents/{id}/scan`: explicit owned quarantine/unavailable retry.
- `GET /api/v1/documents/{id}/download`: current scoped authenticated download only. Quarantine,
  rejected, removed and missing content cannot download. No filenames/paths from users are served.
- `DELETE /api/v1/documents/{id}?expectedVersion=...`: owned unbound draft only, original version;
  stale/bound conflicts return 409. Repeated removal does not fabricate another transition.
- Existing `POST /api/v1/leave-requests` additionally accepts documentIds, at most five distinct clean
  owned unbound drafts. Identity, policy, balance, reporting and audits retain existing protection.

Expected errors use 400 validation, safe 404 missing/out-of-scope, 403 lost permission, 409 lifecycle/
contention and 429 throttling. Scanner failure is a document status, not permission to bind. Transfer
failure may leave an unfinished receiving draft: refresh/remove it before choosing the file again.
Do not automatically replay a mutation after an uncertain outcome or replacement session.

Cancellation stops the client transfer/request; it cannot undo a server commit that already happened.
After an uncertain response, refresh the owned drafts and explicitly select or remove a confirmed
Clean draft. Interrupted Receiving/Scanning drafts cannot bind; no cancelled upload is automatically
attached or replayed. Original upload keys never overwrite existing bytes or change classification.

Stored size/SHA-256 is checked against a bounded immutable byte snapshot before scanning, binding
and download. The scanner and clean promotion consume the same snapshot. Binding/download recheck
the original metadata version and current scope inside database protection; changed/unavailable
content fails safely. No large hash/transfer/scanner work runs while holding the writer reservation.

## Storage, reconciliation and recovery

Private objects live under documents/staging, documents/quarantine and documents/clean using generated
GUID filenames. Persistent documents/locks and documents/removed contain empty per-document
coordination files. Never purge these lock identities or removal markers: late publishers rely on
their permanence. Original filenames are not persisted. Bound clean objects retain their quarantine
copy, increasing storage use. No HTTP/static server must expose any private root. Provision restricted
ACLs (owner/service identity, SYSTEM/Administrators as appropriate), owner-only Unix modes and local
locking-compatible storage. The application rejects reparse points and application/repository roots.

Draft expiry/removal and idempotent cleanup preserve metadata/access records. Bound evidence is
retained indefinitely until an approved retention/legal-hold workflow exists; never infer statutory
periods from this technical policy. Reconcile missing blobs without inventing scan/decision history.
Do not manually change quarantine status to Clean. Do not purge held/bound material to make a backup
fit. Follow the [operator backup runbook](backup-restore.md); stop all writers and maintain keys separately.
The package must include metadata plus actual clean/quarantined objects; absent or mismatched required
blobs fail validation. Restored quarantine/rejected statuses stay inaccessible; recovery credential
invalidation still applies. Repeat drills as formats/storage/retention change, especially future images
and quarantine-policy extensions. Rehearsal evidence is not a guarantee of future recovery.

## Publication, cleanup and upgrade procedure

The OS per-document publication lock and persistent removal marker serialize file writes against
physical cleanup across processes. Receive/validate before acquiring the lock; scan before clean
publication. Removal commits terminal metadata, then writes the tombstone and deletes all content
copies under the same lock. A three-second lock timeout defers physical work; durable metadata stays
inaccessible and the worker retries. No transfer/scan/file-lock wait holds SQLite's writer reservation.

The worker runs every five minutes: up to twenty draft expirations plus twenty Removed records in a
rotating page. Old BlobCleanup entries are history, not exclusions. Every removed record is revisited
as pages cycle; failures can delay cleanup but do not permanently suppress it or starve subsequent
rows in that page. Large backlogs may take multiple cycles; scheduling/load limits remain unverified.
No bound evidence is automatically removed. If final metadata persistence fails after publication,
Receiving/Scanning content stays inaccessible; age-based expiry compensates after restart.

Stop **all** API/worker writers before installing the correction; old writers bypass the new file
protocol. Back up and upgrade using the existing offline procedure. No new schema migration is needed
for this correction. Use only the documented single-host, local locking-compatible storage, never a
network/shared SQLite/private root. Preserve locks/removal markers in backup and isolated restore.
Reconciliation, not manual status changes, should repair interrupted drafts. Never set quarantine to
Clean or delete a marker merely to retry a removed upload: choose a new owned upload key/document.

## Opt-in real-scanner supplemental-evidence drill

The [retained browser drill](../../tests/browser/evidence.real-scanner.ts) is deliberately outside
the default `*.spec.ts` suite. It needs Node 24 (including `node:sqlite`), an official ClamAV
installation and official definitions successfully refreshed with FreshClam. It creates a new migrated
synthetic store, never reads an employee database, and does not seed controlled Clean documents.
Do not run another fixture suite concurrently. See the [dated execution report](../verification/real-scanner-supplemental-evidence.md).

Provision owner-restricted configuration/definition directories outside the checkout and public roots,
using the configuration above. Use an unused unprivileged loopback port in `clamd.conf`, with
`Foreground yes`. Do **not** start that daemon yourself: this test owns its startup, outage and restart.
An occupied port fails rather than stopping an existing service. Supply these operator-selected
variables privately; none is a credential or invitation.

Use the small template above without `Include`, `LogFile` or `PidFile`; the opt-in rejects those
directives so scanner output/PID artifacts cannot escape its owned private scratch configuration.
Keep the configuration directory separate from definitions, logs and the browser/application temp
root. Create `$PrivateHarnessTemp` as a **new, exclusively owned** owner-only directory outside the
checkout/public roots, never an existing shared temp root. Record its expected parent before use;
it must not contain or overlap the installation, definitions, configuration or operator logs.
All four temporary-directory variables below must match it; the opt-in refuses a missing or
inconsistent setting rather than falling back to system temp. Preserve/restore any existing shell
environment values when integrating these commands into an operator script.

```powershell
# From the repository root; variables refer to provisioned private operator paths.
$temporaryNames = @('TMPDIR', 'TMP', 'TEMP', 'ASPNETCORE_TEMP')
$managedNames = $temporaryNames + @('HRFLOW_EVIDENCE_REAL_SCANNER', 'HRFLOW_TEST_CLAMAV_PORT',
    'HRFLOW_REAL_CLAMD', 'HRFLOW_REAL_CLAMD_CONFIG', 'HRFLOW_DIRECTORY_FIXTURE_ROWS',
    'HRFLOW_EVIDENCE_PRIVATE_TEMP', 'HRFLOW_BROWSER_CHANNEL')
$previous = @{}
foreach ($name in $managedNames) {
    $previous[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}
try {
    foreach ($name in $temporaryNames) {
        [Environment]::SetEnvironmentVariable($name, $PrivateHarnessTemp, 'Process')
    }
    & $FreshClamExecutable "--config-file=$FreshClamConfiguration" *> $PrivateUpdateLog
    if ($LASTEXITCODE -ne 0) { throw 'Definition update failed; stop the drill.' }
    & $ClamScanExecutable --version
    foreach ($database in @('daily', 'main', 'bytecode')) {
        $details = & $SigToolExecutable --info (Join-Path $PrivateDefinitions "$database.cvd") 2>&1
        if ($LASTEXITCODE -ne 0 -or !($details | Where-Object { "$_" -match '^Verification OK' })) {
            throw 'Definition verification failed; stop the drill.'
        }
        $details | Where-Object { "$_" -match '^(Version:|Build time:|Signatures:|Verification OK)' }
    }
    dotnet build tests/HRFlow.RegressionTests --no-restore -v quiet
    if ($LASTEXITCODE -ne 0) { throw 'Fixture build failed.' }
    $env:HRFLOW_EVIDENCE_REAL_SCANNER = '1'
    $env:HRFLOW_TEST_CLAMAV_PORT = $UnusedLoopbackPort
    $env:HRFLOW_REAL_CLAMD = $ClamdExecutable
    $env:HRFLOW_REAL_CLAMD_CONFIG = $ClamdConfiguration
    $env:HRFLOW_DIRECTORY_FIXTURE_ROWS = '0'
    $env:HRFLOW_EVIDENCE_PRIVATE_TEMP = $PrivateHarnessTemp
    # Optional on Windows with installed Edge; otherwise use installed Playwright Chromium.
    $env:HRFLOW_BROWSER_CHANNEL = 'msedge'
    Push-Location tests/browser
    try {
        npm.cmd run typecheck
        if ($LASTEXITCODE -ne 0) { throw 'Browser typecheck failed.' }
        npm.cmd test
        if ($LASTEXITCODE -ne 0) { throw 'Real-scanner drill failed; inspect safe diagnostics.' }
    } finally { Pop-Location }
} finally {
    foreach ($name in $managedNames) {
        [Environment]::SetEnvironmentVariable($name, $previous[$name], 'Process')
    }
}
```

Initial dependency installation and Playwright browser prerequisites follow [tests/README](../../tests/README.md).
Normal CI needs no ClamAV installation; setting the opt-in explicitly selects the workflow and
exception-cleanup checks. Neither runs in default CI.
The test uses real HTTP upload, scan, bind and downloads, browser supplemental actions, and read-only
SQLite inspection. Its only policy setup changes are synthetic fixture configuration; document status
is never forced to Clean. Scanner stdout/stderr, raw failures, page content, screenshots and traces
are not published. Only fixed source coordinates and bounded numeric SQLite codes may diagnose failure.
The harness requires the configuration's parent directory to be restricted. It creates private
per-daemon scratch/configuration there, explicitly sets `TemporaryDirectory` and temporary-directory
environment variables, and checks permissions before startup and after exit. It deletes only that
owned scratch after the daemon stops; it does not rely on default Windows temp permissions.
Before/after checks require the original daemon executable, configuration and all three definition
databases to retain their SHA-256 hashes. The exception-cleanup test throws a controlled error inside
`try/finally`, proves cleanup occurs and leaves these operator artifacts intact. The browser fixture
stops its owned API process trees before removing the synthetic database/profile/temp tree.

The embedded harmless EICAR file is generated in memory inside a valid PDF; it is never executed.
A bare `.com` or renamed text file is not an accepted document. EICAR proves this test-signature path,
not general malware detection. The daemon is stopped in `finally`; existing fixture teardown stops
API process trees before deleting only its disposable store. Operator-owned engine/definitions are
separate and are not deleted by the browser runner. No bound-document removal endpoint is exercised.

FreshClam 1.5.4 selects its own scratch below the configured private `DatabaseDirectory`; do not put
clamd's `TemporaryDirectory` directive in `freshclam.conf`. Keep the official installation's verifier
certificates intact. Diagnose the FreshClam process status separately from any libcurl/HTTP code.
If the supported updater reports blocking/rate limits, respect its guidance and stop dependent checks.
Retain the verified definitions and private update logs through review rather than deleting the
installation and forcing another full download. Reuse still requires a supported freshness check.

### Post-run operator cleanup

The tests remove their scanner scratch and fixture stores; runtime/browser tooling may leave cache
entries in `$PrivateHarnessTemp`. After **both** successful and failed invocations, inspect that new
disposable directory. First confirm the runner/fixture API trees and owned scanner/browser processes
have exited; never kill an unrelated service or delete files while ownership/shutdown is uncertain.
Check for remaining port listeners. If a cleanup check fails, retain the directory privately for
investigation rather than broadening deletion.

For the Windows setup verified here, the target was only the newly provisioned `harness-temp` direct
child of the generated private operator root. The guarded removal checked that absolute parent/name
boundary, rejected reparse points throughout the target, and refused while any process referenced
the owned root or ran the fixture API assembly. Only then was `Remove-Item -LiteralPath $target
-Recurse -Force` applied. Engine, original configuration, definitions and logs were siblings outside
the target; SHA-256 checks proved all three definitions remained unchanged. Never substitute the
operator root, a general temp directory or the definitions directory as that target. Windows cleanup
is recorded in the [report](../verification/real-scanner-supplemental-evidence.md); Linux process/ACL
and real-scanner execution remain unverified.
