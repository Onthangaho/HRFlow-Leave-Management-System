# Real ClamAV supplemental-evidence verification

**Corrected-harness local verification completed: two tests passed in one new invocation.**
The subsequent completion section records the new successful FreshClam update, explicit private
browser/application/scanner temp, persisted-row checks and exception cleanup. Earlier passes do not
verify the corrected harness; the earlier Partial result, FreshClam failures, unexplained SQLite
inspection failure and premature cleanup remain preserved below. No hosted/Linux or production claim.

26 October 2026. Branch `test/real-scanner-supplemental-evidence`, fetched main
`fbd27a753e5c30713f372432903250c695d01b94`. PR [#135](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/135)
was confirmed merged at 12:58:44 UTC before branching. This is new local evidence, not a rewrite of
the [implementation/hosted report](pending-request-evidence.md). No product, schema, dependency,
CI or permission-rule changes; no external delivery, deployment or legal/payment change.

## Environment and prerequisite evidence

Windows x64, Node **24.12.0**, installed Edge through Playwright, existing migrated SQLite/Identity
fixture with generated private credentials. Two real API processes and one foreground scanner are
owned by the fixture/drill. The opt-in disables the five default controlled-Clean documents and the
unrelated 75-row directory population. Request creation uses actual HTTP with current selector
versions. Synthetic setup changes the type from Optional Ordinary to Optional Medical between
the two submissions; each captured snapshot remains authoritative thereafter.

Official **ClamAV 1.5.4** Windows x64 ZIP was obtained from the official upstream release over HTTPS.
`clamscan.exe --version` reported `ClamAV 1.5.4`. No independently executed detached-signature
verification is claimed. Extraction, configuration, definitions and private updater logs were in a
new owner-only temporary directory outside the checkout/public roots. The daemon configuration
uses `TCPAddr 127.0.0.1`, unprivileged port 33319, `Foreground yes`, 10 MiB stream/file limits,
50 MiB aggregate scan limit, bounded scan time/queue and `AlertExceedsMax yes`.

FreshClam ran against a new empty private definitions directory, using `DatabaseMirror
database.clamav.net`, `ConnectTimeout 10`, `ReceiveTimeout 30`, `MaxAttempts 1`. It completed with
**exit 0**. Official `sigtool.exe --info` reported verification OK for each installed database:

- **daily 28165**, 355,788 signatures, built **26 October 2026 06:27 UTC**.
- **main 63**, 3,287,027 signatures, built **16 December 2025 23:18 UTC**.
- **bytecode 339**, 80 signatures, built **11 September 2025 08:29 −04:00**.

These are actual database build dates, not a claim all databases were built today. FreshClam
downloaded/verified the official databases; no custom always-clean or EICAR-only signatures.

Installation attempts are retained as limitations, not hidden successes: the vendor URL HEAD
returned HTTP 403; GET returned 200 but was slow. PowerShell GET and the slower vendor continuation
were cancelled. A 60-second curl attempt ended incomplete; upstream 180-second and resumed
300-second transfers also timed out (the latter explicitly exit 28). A final 180-second continuation
completed HTTP 206 / exit 0, producing a complete **225,774,978-byte** ZIP. No partial archive was
executed. An initial `freshclam --version` without configuration emitted a missing-config error;
engine version was instead obtained with `clamscan --version`. A sandboxed private-log inspection
was denied; the restricted log was subsequently inspected in the authorised operator context.
Neither event is an application/scanner workflow result.

## Retained command and test boundary

Exact setup/update and opt-in environment/commands are in the
[operations runbook](../operations/supporting-documents.md#opt-in-real-scanner-supplemental-evidence-drill).
Executed fixture build: `dotnet build tests/HRFlow.RegressionTests --no-restore -v quiet`:
**succeeded, zero warnings/errors, 16.05 seconds**. Browser typecheck:
`npm.cmd run typecheck` from `tests/browser`: passed. Initial attempts with the blocked `npm.ps1`
shim and from the repository root failed before typechecking; the corrected `npm.cmd` command ran
in the browser project. These command-invocation errors are not omitted or counted as test passes.

Each opt-in execution used `HRFLOW_EVIDENCE_REAL_SCANNER=1`, `HRFLOW_TEST_CLAMAV_PORT=33319`,
private `HRFLOW_REAL_CLAMD` / `HRFLOW_REAL_CLAMD_CONFIG`, `HRFLOW_DIRECTORY_FIXTURE_ROWS=0`,
`HRFLOW_BROWSER_CHANNEL=msedge`, then `npm.cmd test` in `tests/browser`. Retries zero; one test
owns the complete workflow. Its 180-second deadline includes two real database-loaded scanner
startups; readiness is bounded PING, not an arbitrary workflow delay. Port occupancy is refused.
Normal CI does not select this file and needs no scanner. No CI or dependency changes.

## Separate executions, including failures

1. **Failed**, test **65.154 s**, suite **78.735950 s**. Ordinary/Medical upload, binding,
   byte downloads, scope checks and outage/retry reached their assertions. The final EICAR
   assertion failed: a string placed in a PDF comment was not reported Rejected. This is not
   evidence that arbitrary strings in otherwise benign files must be detected.
2. **Passed**, test **66.727 s**, suite **79.403341 s**. Changed only the test probe to a valid
   PDF containing the canonical harmless 68-byte EICAR file as an embedded file. Real scanner
   rejected it; binding/download denial and aggregate clean-file checks passed. This execution
   preceded the explicit recursive permission assertions.
3. **Failed**, test **55.548 s**, suite **67.721098 s**. Source-only diagnostics located a direct
   SQLite read after the retry addition. Raw error details were deliberately not retained; the
   original SQLite error code/cause is **unresolved**, not proven contention. No product fix claimed.
4. **Passed**, test **60.819 s**, suite **72.442564 s**. Same assertions with numeric-only SQLite
   failure diagnostics and bounded owned-daemon teardown. No SQLite error code emitted; recursive
   private-file permission assertions passed. This does not reconstruct execution 3's error.
5. **Passed**, test **63.684 s**, suite **76.155862 s**. Pre-scratch-correction version, including recursive
   permissions and exactly three aggregate bindings/addition actions. The inspection connection uses
   `PRAGMA busy_timeout=3000`, matching the existing fixture's bounded SQLite lock budget rather
   than immediately failing beside legitimate API writers. Assertions and test retries unchanged.

Affected gates ran **sequentially after** execution 5's real-scanner fixture stopped:

- `dotnet test tests/HRFlow.RegressionTests --no-build --filter 'FullyQualifiedName~EvidenceAdditionTests' -v minimal`:
  **40 passed / 0 failed / 0 skipped**, **4m06s**. This includes controlled clean/transaction/race
  fixtures; it is not another positive real-scanner execution.
- `HRFLOW_TEST_CLAMAV_PORT=0`, `HRFLOW_BROWSER_CHANNEL=msedge`, `npm.cmd test -- evidence.spec.ts`
  from `tests/browser`: **8 passed / 0 failed**, suite **59.171050 s**. Real HTTP binding uses
  controlled-Clean documents, with separate transport/session barriers and actual scanner-unavailable
  rejection. Both replacement-account cases passed. No test retries.
- Final browser `npm.cmd run typecheck`: passed. Node emitted its existing experimental
  `node:sqlite` warning in the real-scanner executions; no dependency was added to suppress it.

These executions are not a single aggregate suite. Full backend/default-browser gates, frontend
product build/lint, vulnerability audits and model checks were **not rerun**: no product, model,
dependency or CI files changed. The existing fixture project build and affected tests above are the
checks executed here; previous full-suite/hosted results remain historical evidence.

## Verified workflow and persisted invariants

The [opt-in test](../../tests/browser/evidence.real-scanner.ts) uploads harmless complete PDFs through
the real UI and production INSTREAM scanner adapter. Clean metadata is never injected or edited.
Persisted Clean status, scan time, original SHA-256 and size are inspected through a read-only SQLite
connection. Selection and binding use the actual personal-history supplemental-evidence UI.

Each initial request has exactly one bound document, one `EvidenceAddition`, and one
`SupplementalBind` action. The complete request row (including snapshot/description/status/dates)
and all original transition audit rows remain byte-equivalent in the read projection before/after.
The ordinary request was captured before the type became Medical; it keeps its original requirements.

Owner and authorised HR downloads return exact original bytes/checksums, `no-store` and `nosniff`.
The browser's actual downloaded stream also matches SHA-256. The eligible Manager downloads
Ordinary evidence; Medical download returns safe 404, and Manager metadata has no media type or
download capability. No filenames or medical payloads are introduced into reports.

Stopping the owned daemon yields `ScanUnavailable`, safe 404 download and 409 bind refusal.
After real PING readiness on restart, clicking **Retry scan** succeeds; explicit selection/binding
creates one additional addition, not a replay. Final totals are three Clean/bound documents,
three additions and three supplemental-binding actions; original requests/audits unchanged.
The valid PDF with embedded EICAR is rejected and remains unbound/inaccessible. A bare `.com`
or renamed text file was not admitted and format validation was not weakened. This harmless
signature exercise is not a claim of comprehensive malware detection or legal/production readiness.

Private clean objects match persisted size/hash. Recursive Windows ACL checks cover database,
private root, quarantine, clean and coordination objects; only current owner/System/Administrators
allow entries are permitted, with reparse points refused. Unix owner-only checks are retained but
not locally executed. Test credentials/paths live only in the private runtime descriptor. Scanner
stdout/stderr and API output are not published; screenshots/traces/video remain off. Safe reporting
outputs fixed test names/source coordinates and bounded numeric SQLite codes, never raw errors,
HTTP bodies, credentials, activation links or document content.

Owned scanner exit is awaited before teardown; fixture teardown stops API process trees before
deleting only its synthetic store. Browser temporary downloads are deleted. This is disposable-store
cleanup, not deletion of bound evidence through product APIs. Operator engine/definition cleanup is
separate and must never target another installation or any existing employee store.

Post-execution-5 checks found **zero owned scanner processes** and **no listener on port 33319**.
The separate operator cleanup checked **228 scanner directory/file ACLs**, verified the resolved
destination was the newly generated direct temporary child with the expected random identifier,
and removed only that root after confirming no executable from it was running. Temporary complete/
partial download archives and the ignored scanner descriptor were removed by exact filenames.
No employee store, existing installation or product bound-document record was targeted.

Final storage review then found the daemon had not explicitly overridden its temporary extraction
directory. The Windows default temp root had **three non-owner/System/Administrators allow entries**.
The earlier artifact checks therefore did **not** establish that every possible scanner scratch file
was private. The scanner installation had already been cleaned up; that cleanup was premature for
the final review. No claim that those five runs verified explicit scratch isolation is made.

The retained harness now creates an owner-restricted per-daemon scratch directory, writes a private
configuration with `TemporaryDirectory`, overrides `TMPDIR`/`TMP`/`TEMP`, verifies permissions before
startup and after exit, and removes only that owned scratch directory after the daemon stops. The
default scanner configuration and all product code remain unchanged. Installation was repeated for
the affected final verification: eight 180-second HTTP-range transfers all returned partial HTTP 206
and curl exit 28. A subsequent bounded selective extraction reads the official ZIP directory and only
required runtime entries, verifies ZIP lengths/CRC, and omits optional symbols. No alternate scanner,
custom verdict, security-check bypass or suppressed test assertion is introduced.
That extraction initially omitted the mandatory `certs/clamav.crt`: FreshClam refused initialization
with **exit 2**, reporting an invalid certs directory. The genuine certificate was then extracted
from the same official archive with CRC/size checking. Signature verification was never disabled.

With the official certificate present, the repeated supported FreshClam invocation used private
`TMPDIR`/`TMP`/`TEMP`, the same bounded configuration, and a new empty definitions directory. It
updated daily **28165**, then failed with **exit 5**, native download error **56**:
**“Failure when receiving data from the peer.”** The complete main/bytecode set was unavailable.
This is an actual signature-retrieval failure, not an inferred scanner/API defect. The dependent
final private-scratch workflow was **not run**; no incomplete definitions, mock verdicts or manually
changed Clean metadata substituted for it. Earlier successful full-definition runs remain valid
evidence of those versions only. The final harness is typechecked, but its new scratch guards and
cleanup still require an actual real-scanner rerun after FreshClam can complete.

Before final cleanup, inspection confirmed **main.cvd absent**, **bytecode.cvd absent**, and **zero
owned scanner/updater processes**. All **41** remaining second-installation scanner objects passed
the restricted ACL check. Only that verified new temporary root, its private descriptor and the
eight exact partial range-download files were removed. No dependent real-scanner run followed the
failed update. Final TypeScript typecheck passed; test discovery separately confirms the one-test
opt-in and unchanged 59-test default selection. Discovery is not execution evidence.

## Completion continuation: corrected harness, 26 October 2026

This section is new execution evidence, not a relabelling of executions 1–5. No existing verified
database survived the earlier cleanup, so no historical CVD was silently reused. A new private
operator installation was provisioned from the same official 1.5.4 ZIP runtime/certificate entries;
definitions, private updater logs, configuration, updater scratch and application/browser temp were
separate locations. Required definitions and the engine are now deliberately retained as operator
assets; the drill cannot remove them.

### Failure diagnosis and supported retry

The original failed-update private log had already been deleted in the earlier cleanup. Its recorded
`exit 5` and `Download failed (56): Failure when receiving data from the peer` are preserved; no claim
of freshly rereading that deleted log. Version-matched primary sources distinguish the two codes:

- ClamAV 1.5.4's [`fc_error_t`](https://github.com/Cisco-Talos/clamav/blob/clamav-1.5.4/libfreshclam/libfreshclam.h#L62-L83)
  enumerates **5 = FC_ECONNECTION**. [`freshclam.c`](https://github.com/Cisco-Talos/clamav/blob/clamav-1.5.4/freshclam/freshclam.c#L1777-L1802)
  propagates the update status and [returns it](https://github.com/Cisco-Talos/clamav/blob/clamav-1.5.4/freshclam/freshclam.c#L2038-L2042).
  This is the process result, not libcurl code 5 and not an HTTP status.
- libcurl documents **56 = CURLE_RECV_ERROR**, a network-data receive failure in its
  [error reference](https://curl.se/libcurl/c/libcurl-errors.html). The failing network component is
  **not established**: these codes alone do not prove DNS, TLS, CDN, proxy or local contention.
- The [official FAQ](https://docs.clamav.net/faq/faq-freshclam.html#update-failed-your-network-may-be-down-or-the-clamav-database-content-delivery-network-is-experiencing-an-outage)
  advises waiting several minutes and using verbose diagnostics for a network/update failure.
  More than that interval elapsed before the supported retry. FreshClam retained its normal CDN,
  TLS and CVD signature verification; no custom mirror, manual CVD download or cooldown reset.
  The newly retained logs contain no 403, 429, Retry-After or cooldown indication.

One new configuration attempt incorrectly supplied clamd's `TemporaryDirectory` to FreshClam.
Its private log explicitly rejected that option; the initial `Start-Process` collector did not
capture a reliable exit code, so none is inferred. The unsupported line was removed. In 1.5.4,
[`freshclam.c` lines 861–869](https://github.com/Cisco-Talos/clamav/blob/clamav-1.5.4/freshclam/freshclam.c#L861-L869)
selects its work directory below `DatabaseDirectory`; that whole directory is private. The private
environment also sets TMPDIR/TMP/TEMP. This correction changes test/operator configuration only.

The corrected invocation used:

```text
freshclam.exe --config-file=<PRIVATE_FRESHCLAM_CONFIGURATION> --verbose
```

Configuration: private `DatabaseDirectory`, official `DatabaseMirror database.clamav.net`,
`ConnectTimeout 10`, `ReceiveTimeout 30`, `MaxAttempts 1`. The official source identifies
ReceiveTimeout as libcurl's low-speed interval, not an overall operation deadline. The operator
process wrapper separately bounded execution to 900 seconds and both redirected output drains
to five seconds. It recorded the real .NET process exit code and retained stdout/stderr privately.

**16:42:27.768–16:46:21.645 UTC: exit 0**, completed within the deadline. FreshClam installed and
tested **daily 28165 / main 63 / bytecode 339**. Official SigTool separately reported verification OK
and the same actual build dates listed above. `clamscan --database=<PRIVATE_DEFINITIONS> --version`
reported **ClamAV 1.5.4/28165**, local signature timestamp **26 October 2026 08:27:07 SAST**;
the daily header reports **06:27 UTC**. This is a new successful update after the preserved failure.

### Final source and execution

The opt-in now refuses missing/mismatched `HRFLOW_EVIDENCE_PRIVATE_TEMP`, TMPDIR, TMP, TEMP and
ASPNETCORE_TEMP. The test-only C# host validates the private temp ACL before creating its migrated
store. Browser profiles/downloads, application multipart buffers, API fixture databases and runtime
files therefore use explicit private locations, not a shared/system fallback. Normal CI/default
behaviour is unchanged. Scanner configuration lives separately; private per-daemon configuration
overrides `TemporaryDirectory` and all three temp environment variables. Log/PID/include directives
are refused. Daemon output is discarded, not written to an uncontrolled location.

FreshClam finished before the actual drill; build/typecheck did not access fixture stores.
There were no overlapping offline fixture writers. Newly run:

- `dotnet build tests/HRFlow.RegressionTests --no-restore -v quiet`: **passed, zero warnings/errors,
  8.59s**.
- `npm.cmd run typecheck` in `tests/browser`: **passed**.
- Negative guard check: `npm.cmd test -- --list --reporter=list` with real-scanner opt-in but no
  `HRFLOW_EVIDENCE_PRIVATE_TEMP` returned nonzero with the fixed private-temp refusal. Only the
  allowlisted boolean `MissingPrivateTemporaryRootRefused=True` was reported. This is an expected
  configuration denial before any fixture creation, not an executed/failed workflow test.
- `npm.cmd test` in `tests/browser`, with the explicit real-scanner/private-temp environment in the
  [runbook](../operations/supporting-documents.md#opt-in-real-scanner-supplemental-evidence-drill):
  **2 passed / 0 failed**, **99.523885s**, one invocation, retries zero.
  The full real HTTP/UI workflow took **69.262s**. The separate real-daemon controlled-exception
  cleanup test took **16.118s**. The latter catches only its exact synthetic exception after
  `finally`; unexpected startup/cleanup failures fail the test rather than masquerading as success.

The real workflow checked clean PDF upload, UI supplemental binding and browser/API checksum
downloads; Ordinary and captured Optional Medical requirements; owner/HR Medical access and
Manager content denial/redaction; scanner-stop inaccessibility/unbindability; healthy restart and
explicit Retry scan; valid PDF embedded EICAR rejection, safe download denial and failed binding.
No document scan status was injected. The normal scanner adapter and production API performed
all status changes. Current owner identifiers were checked on the stored documents/addition actors.

**Independent persistence inspection:** the test opens a separate read-only `node:sqlite` connection,
not an API projection or EF tracking cache. Parameterised SELECTs inspect SupportingDocuments,
EvidenceAdditions, DocumentAccessEntries, LeaveRequests and AuditEntries. Each initial request had
one binding/addition/action; the explicit retry created a third binding/addition/action across the
two requests. Totals were **3 Clean documents, 3 additions, 3 SupplementalBind actions**. EICAR
remained unbound and inaccessible. Both complete original request-row projections and their ordered
transition-audit rows compared unchanged before/after. All three clean stored blobs matched their
persisted sizes/SHA-256, and actual browser download streams matched the original input checksum.

The earlier SQLite inspection failure did **not recur** and emitted no numeric SQLite error in this
invocation. It remains **unexplained**. The retained bounded 3-second busy budget is not presented
as a proven root-cause fix; there are no assertion/test retries.

### Success, failure and independent cleanup inspection

Both the success workflow (including outage/restart) and controlled-exception path verified private
scratch permissions before startup and after exit. Stop awaits the owned foreground daemon before
removing its generated scratch child, validates the absolute parent/name/link boundary, then checks
that the engine, original configuration and all three definitions retain their SHA-256 hashes.
It never recursively removes the operator installation or definition directory.

After Playwright and the API fixture teardown, independent operator inspection reported:

- **0 owned process references**, **0 current-checkout API processes**, no listener on port 33319.
- **0 scanner scratch directories**. **178** remaining operator/runtime objects passed private ACL
  and non-reparse checks. The fixture's own migrated database/profile tree had been removed after
  its process teardown.
- Five runtime/cache entries remained in the dedicated private browser/application temp parent.
  This is not hidden as automatic complete cleanup. Once the independent process checks passed,
  operator cleanup verified the absolute generated-parent/direct-child boundary and rejected linked
  objects, then removed **only** that disposable `harness-temp` child. It did not target definitions,
  logs, configuration or engine files. Required definition hashes were checked unchanged afterward.

The private updater logs/verified definition set are retained for operator review/reuse, outside the
repository and excluded from artifacts. There are no owned processes, sensitive scanner scratch,
application stores or browser temp files left. Reuse requires another supported freshness check;
the retained definitions' provenance/build dates are those recorded here, not a perpetual freshness
claim. No bound evidence in an existing employee store was deleted.

This continuation did not rerun the historical 40-backend/8-default-browser executions as new
results. Product source, schema, dependencies and CI remain unchanged; no broader gate was justified
by these opt-in fixture changes. Hosted/Linux and target-host evidence remain outstanding.

## Scope and limitations

No production defect demonstrated or product source changed. This is local Windows/Edge evidence;
no hosted real-scanner/Linux, physical-device, screen-reader, hostile-load or exhaustive-format claim.
Real PDFs were exercised, not every supported JPEG/PNG variant. Existing controlled fixtures remain
separately labelled. No new backup/recovery rehearsal, statutory/payment/retention decision, external
service or production invitation delivery. Broader suites are not implicitly rerun by this drill.

Official references retrieved 26 October 2026: [downloads/support choice](https://www.clamav.net/downloads),
[official Windows installation](https://docs.clamav.net/manual/Installing.html),
[FreshClam database updates](https://docs.clamav.net/manual/Usage/SignatureManagement.html),
[daemon and TCP exposure](https://docs.clamav.net/manual/Usage/Scanning.html).
[EICAR's official test-file page](https://www.eicar.org/download-anti-malware-testfile/) timed out on
two direct retrievals; its official-domain search extract described the canonical 68-byte starting
sequence. Full page retrieval is not claimed. The successful probe is a locally constructed PDF
container, not an official downloadable PDF test product.

## Review checks

Seven intended test/documentation files, including both new files, were inspected. `git diff --check`,
strict UTF-8/BOM/replacement-character checks, Markdown fence/relative-link checks (**32 links**),
the new runbook anchor, and targeted token/key/activation-URL/private-path scans passed. Both instruction
files remain byte-identical and unchanged. Product source, migrations, manifests/locks and CI have no
diff. The complete review patch includes untracked new files; binaries, definitions, stores, documents,
private logs/descriptors and temporary verification scripts are excluded. Nothing committed or pushed.
