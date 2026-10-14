# Private profile images

Use the supported private local root and real ClamAV setup from
[supporting documents](supporting-documents.md). Clamd must be loopback-only with real definitions.
`Documents:ClamAvPort` and `Documents:ScanTimeoutSeconds` are shared scanner transport settings;
image permissions and lifecycle are separate. Scanner absence fails closed. Never install a
dummy clean-verdict server. Maintain definitions through official FreshClam procedures.

## Contracts

- `GET /api/v1/me/image`: `{ version, hasImage }`; initial version is the empty GUID.
- `PUT /api/v1/me/image`: multipart `file`, required `expectedVersion`, invariant decimal strings
  `x`, `y`, `size`; successful response is updated metadata. No target identity/upload URL.
- `DELETE /api/v1/me/image?expectedVersion=...`: 204; removal advances the absence version.
- `GET /api/v1/me/image/content`: own processed PNG.
- `GET /api/v1/employees/{employeeId}/image`: own or current HR-only employee visibility.

Use 400 for invalid image/crop or missing version, 401 for unusable credentials/lifecycle,
403 for lost capability, indistinguishable 404 for unavailable/out-of-scope image, and 409 for
stale processing/version/scanner/storage contention. Unsupported cross-user mutations have no
route (405 for an existing read-only route). No raw original/storage identifier is returned.

Square crop fractions and exact limits are defined in [ADR 0025](../adr/0025-private-profile-images.md).
Uploading/removal changes only image rows/files, never employment roles or leave/audit history.

## Retained verification commands

Standard scanner-free gates exercise real scanner outage and controlled adverse protocol replies:

```sh
dotnet build HRFlow.sln --no-restore
dotnet test tests/HRFlow.RegressionTests --no-build
```

With an actual configured local ClamAV scanner, run the same retained image HTTP/row assertions
through its clean-verdict path, then real browser crop/replacement/removal/session barriers:

```powershell
$env:HRFLOW_TEST_CLAMAV_PORT='3310'
dotnet test tests/HRFlow.RegressionTests --no-build --filter FullyQualifiedName~ProfileImage
$env:HRFLOW_IMAGE_SCANNER_TESTS='1'
$env:HRFLOW_BROWSER_CHANNEL='chrome' # omit for installed Playwright Chromium
npm.cmd --prefix tests/browser test
Remove-Item Env:HRFLOW_TEST_CLAMAV_PORT,Env:HRFLOW_IMAGE_SCANNER_TESTS,Env:HRFLOW_BROWSER_CHANNEL
```

The scanner-specific browser file is a separate explicit suite, not a skipped/retried test.
Default hosted gates remain scanner-free; real scanner positives are local evidence until a
reviewed hosted scanner fixture exists. Hosted CI remains blocked by billing; no billing or
workflow protections are changed.

Screenshot capture is explicit (`HRFLOW_IMAGE_SCREENSHOTS=1`) and limited to known synthetic
profile/directory screens. Review before publishing; routine screenshots/traces/video remain off.
Never use real portraits/personnel records or enable failure-artifact uploads.

## Cleanup and recovery

The image worker runs bounded batches each minute. Do not delete `locks` or `removed` while any
publisher can exist. Interrupted Processing expires after one hour; failed removal remains
inaccessible and is retried. Removed metadata/markers are currently retained indefinitely.
No cleanup applies to bound supporting documents. Operators should monitor private-root growth.

Stop and verify all writers using [backup/restore operations](backup-restore.md), then take the
database and entire private namespace together. Missing current images or missing removal markers
fail backup/restore verification. Never restore a database alone, silently omit image blobs or
claim success on missing content. Resume workers only after required security invalidation and
authorised access recovery; restored Processing stays unavailable.

For one combined local browser invocation with real ClamAV, set
`HRFLOW_IMAGE_SCANNER_TESTS=all` instead of `1`; this selects both the existing nine
gates and the three image gates. It does not change default scanner-free hosted selection.
