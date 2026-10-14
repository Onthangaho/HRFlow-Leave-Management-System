# Current-feature pitch rehearsal: executed evidence

14 October 2026. Branch **feat/repeatable-pitch-demo**, base fetched origin/main
**9ec7724adaaedea80e6082427f28ef8f9c9d0307**. Initial tree was clean. PR #117 was confirmed
merged at **19:03:33 UTC**. #97 remains open for its full dependencies. No product, schema or
dependency changes. Prepared for independent PR review; no merge/deployment/issue closure.

## Implementation-stage fresh-store browser workflow (prior evidence)

Windows, Node 24.12.0, .NET SDK 9.0.305/runtime 8.0.23, Playwright with installed Chrome.
Real API and Vite client on Development loopback, generated private credentials, fresh file-backed
SQLite/current startup migrations and actual Identity seeding. No existing database copied or reset.
Real ClamAV **1.5.4**, daemon VERSION **28148 / 9 October 2026 08:25:40**; existing real signature
databases from the earlier scanner setup were reused read-only. FreshClam was **not rerun during implementation**; finalisation updated definitions as recorded below.
Operators must maintain/update definitions before a presentation; this benign-file check is not
comprehensive malware-detection or production-scanner evidence.

Exact operator commands are in [the runbook](../operations/pitch-rehearsal.md). The actual timed command
used `HRFLOW_BROWSER_CHANNEL=chrome` and `node --disable-warning=ExperimentalWarning
tools/pitch/rehearse.mjs <private-marked-destination>`; no private path/descriptor is published.

Two complete fresh-store browser rehearsals passed: **23.935 seconds**, then **19.950 seconds**
after screenshot settling improvements. The latter was the implementation-stage latest complete walkthrough run, not an
aggregate of partial runs. These are automated timings, **not** a measured 8–10 minute human pitch.
The runbook's nine-minute speaking schedule remains proposed/unrehearsed by a human.

- Actual HR upload → column mapping → server-held Ready preview → explicit confirmation created
  Avery Example, `avery@example.invalid`, without a password CSV. Real private Development pickup →
  anonymous activation password form succeeded; no automatic login. The new account then logged in.
- Actual HR forms created a 20-calendar-day company policy and Optional/Required **Ordinary** types.
  The Employee selector immediately exposed the new type after role/account switching without reload.
- Avery used the real form, today's South African date (14 October 2026) at both inclusive endpoints,
  harmless description and generated structurally valid PDF. Real scanner produced **Clean**;
  explicit attachment selection/submission succeeded with one request.
- HR Pending monitoring showed the request with no decision controls. Direct HR approval returned
  **403**. Assigned Manager approved using the real confirmation dialog and optional plain-text note.
  Team calendar/list then showed Avery's Approved absence and one active person absent that date.
- Manager submitted their own Optional-evidence personal request through the real form. Direct
  self-approval returned **403**; it remained Pending, for their assigned eligible Manager.
- Avery saw Approved personal history, the immutable timeline/note and a delivered approval inbox
  notification. Inbox refresh was explicit and bounded while waiting for asynchronous delivery.
- HR Overview/trends and the actual department report/export action worked. A real CSV download
  contained Approved metrics and excluded the decision note. No personnel download is committed.
- Desktop **1366×900** and mobile **390×844** protected screens were captured, waiting for responsive
  layout/chart settling. No page-level horizontal overflow in the checked captures. Timeline captures
  include the full lifecycle. Two mobile frames were subsequently recaptured against the same
  authenticated real store to show the chart and accessible team list rather than a scrolled footer.
  That focused capture rerun passed; it is not claimed as another full workflow run.

## Persisted outcomes and restart

The runner used independent **read-only SQLite** inspection after UI actions:

- Avery request: **Approved**, expected owner and processing Manager IDs; exactly **two** audits:
  Submit (owner) and Approve (Manager), with the matching plain-text decision note.
- Manager self-request: **Pending**, exactly **one** Submit audit; neither forbidden API attempt
  persisted a decision/status/audit change.
- Exactly one **Clean Ordinary** document bound to Avery's request. Clean blob bytes equalled the
  generated PDF and SHA-256 matched persisted metadata. No raw filenames/paths in DTO artifacts.
- Exactly **two** delivered notifications for Avery's request: Submit to the assigned Manager and
  Approve to Avery, each with the expected recipient. No note/document contents copied to payloads.
- A real stop/restart compared private fingerprints of **all rows in seven tables**: AspNetUsers,
  AspNetUserRoles, Employees, LeaveRequests, AuditEntries, SupportingDocuments and OnboardingBatches.
  All matched; no duplicate EventId/RecipientId notification rows after worker restart. Hashes/security
  values were not printed or published. This proves preservation for this fixture, not all lifecycle
  or recovery matrices; no deliberate deactivation/role-removal restart scenario was added here.

## Fixture refusals and cleanup

Executed: setup over an existing destination refused without replacing it; cleanup during live owned
processes refused. A Windows junction destination was rejected and removing only the test junction
left its target store intact. The final cleanup walked owned directories for reparse objects before
deletion. Setup is limited to a new direct child of the OS temp directory with owner-only permissions.

Typed `stop` in the owned interactive start terminal: API/Vite stopped, with data retained for restart.
Marked destination/pickup cleanup succeeded. After stopping only the scanner process launched for
this rehearsal, setup failed with real ClamAV guidance **before creating any destination/database**.
All owned prototype/final stores and unique pickup invitations were cleaned; owned scanner extraction/
private output was removed. Pre-existing scanner definitions and unrelated stores were preserved.
Process inspection found no remaining demo Node/API processes. Abrupt OS kill, forced cleanup failure
and Linux permission/process behaviour remain unexecuted.

## Checks executed during implementation (not finalisation reruns)

- `dotnet build HRFlow.sln --no-restore`: **passed, 0 warnings/errors, 2.77s** after stopping the owned
  pitch API. An earlier attempt while it held Windows DLLs failed with **30 copy warnings / 6 errors,
  26.37s**; the guarded command did not run tests after that failed build. It is not a passing run.
- `dotnet test tests/HRFlow.RegressionTests --no-build --logger 'console;verbosity=normal'`:
  **35 passed, 0 failed/skipped, 2.7729 minutes (166.374s)**, fresh separate fixtures.
- `HRFLOW_BROWSER_CHANNEL=chrome npm.cmd --prefix tests/browser test`: **9 passed, 0 failures/retries,
  31.830s**. This followed the backend suite in the same guarded sequential command. The retained
  suite's three real HTTP workflows and six controlled-transport session tests remain distinct.
- `npm.cmd --prefix src/HRFlow.Client run build`: **passed, 831 modules, 1.50s**. Main bundle unchanged:
  **597.95 kB / 175.88 kB gzip**; existing Vite >500 kB warning remains.
- `npm.cmd --prefix src/HRFlow.Client run lint`: **exit 0**, existing useAuth.tsx:223 Fast Refresh warning.
- `dotnet list HRFlow.sln package --vulnerable --include-transitive`: **no vulnerable packages** across
  all six projects in configured feeds. Client and browser npm audits: **0 vulnerabilities** each.
- `dotnet ef migrations has-pending-model-changes ... --no-build`, using a fresh explicit disposable
  connection: **no model changes since the last migration**. No migration/schema change is needed.
- Both new Node files passed `node --check`; final working/new-file whitespace, scope, sensitive-content
  and instruction byte comparison are recorded in the final review output. No test assertion/source
  was weakened; production source and retained tests match the branch base.

Earlier #76/#83/etc. reports remain prior evidence, not reruns in this slice. Initial pitch prototypes
stopped for a guessed seed department, exact-label automation, the initial tiny PNG upload, unrefreshed
inbox delivery and transient/screenshot framing issues. These were corrected only in fixture/automation:
API-derived department, real form labels, a structurally valid PDF, bounded explicit inbox refresh,
responsive settling and targeted frames. No product authorization/content-validation rule changed.

Before independent-review finalisation, fixture-only refinements added explicit fixed-origin/running-owner/completed-rehearsal guards,
restricted destination validation to a direct temp child, and marked safe operator errors so library
exceptions cannot expose private payloads. Syntax checks and scanner-absence/unsafe-path refusals were
rerun afterward; mobile framing was recaptured as described above. **The full walkthrough was not
rerun after these final guard-only additions**. No separate historical runs are combined into a final
aggregate pass. Product source and retained regression tests were unchanged.

## Independent-review finalisation: newly executed evidence

FreshClam 1.5.4 updated a private copy of real definitions using its supported update mechanism,
**exit 0**. Daily **28148 -> 28153**, main **63** and bytecode **339** already current.
Sigtool reported daily build time **14 October 2026 06:24 UTC**. The actual daemon loaded
**ClamAV 1.5.4/28153/Wed Oct 14 08:24:15 2026** (local SAST representation).
No original installation/definitions were overwritten; temporary scanner files/logs were excluded.

Concrete fixture review fixes: check both recorded parent and API PIDs before start; accumulate
bounded lifetime output across chunks; bound client shutdown to five seconds and still attempt API
shutdown if client closure fails. A failed closure retains ownership rather than permitting cleanup.
No product changes, bypasses, schema changes or dependencies.

**The entire fresh-store real browser walkthrough was rerun against this final fixture code**:
**passed, 29.258 seconds**, Windows/installed Chrome, updated real ClamAV. Automated duration is
not human presentation time; the nine-minute human walkthrough remains untimed.

All final UI and persisted assertions passed: actual CSV mapping/preview/confirmation and secure
activation; Optional/Required Ordinary type forms; Clean PDF upload and explicit request binding;
assigned Manager decision/note; HR approval and Manager self-approval **403**; owner history,
timeline/inbox; team calendar; HR trends and actual CSV download. Exactly Submit+Approve audits
with correct actors/note, one matching Clean Ordinary bound blob/checksum, two intended event
recipients, and the Manager self-request Pending with only its Submit audit.

Newly rerun restart comparison: all rows of the seven listed tables match; no duplicate event/recipient
notifications. Existing-destination setup and live-writer cleanup refused. Actual final stop/cleanup
removed only owned database/files/pickup; an unrelated sibling sentinel survived. Owned scanner
process/files were removed after validating identity/containment; original definitions remained.
No fixture API process remained. All **15 regenerated screenshots (1,157,127 bytes total)** were
visually reviewed: no passwords/tokens/invitation URLs/local paths/real data/document contents.

Node syntax checks, complete/staged whitespace/sensitive-content/scope checks and byte-identical
instruction comparison passed. Production source/schema/dependencies/tests match the branch base.
The **35 backend (166.374s) / 9 browser (31.830s)** results above, build/lint/model and vulnerability
checks are **prior implementation evidence**, not rerun in finalisation; final changes affected only
fixture tooling/docs/screenshots and their affected checks were rerun. No aggregate historical pass.
Forced client shutdown failure, abrupt OS kill and Linux process/permission behaviour remain unexecuted.

## Actual hosted limitation

[Current main workflow run 38620456715](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/38620456715)
reports failure with **no job steps**. Its annotation says: **“The job was not started because your
account is locked due to a billing issue.”** Hosted Linux/bundled Chromium/build/lint/tests are
unexecuted. No billing changes, spending, disabled workflow or branch-protection changes.

## Reviewed synthetic screenshots

15 PNG files, **1,157,127 bytes total** after finalisation regeneration, each below 120 kB.
All were visually inspected; only synthetic names/addresses, harmless Ordinary evidence status and
company sample values. No passwords, JWTs, invitation URLs, private phone/medical content or local
machine paths; login/activation/browser chrome deliberately excluded. Screenshot sizes are local
artifacts, not an accessibility certification.

- [Onboarding preview](screenshots/pitch-rehearsal/onboarding-desktop.png)
- [Requirements](screenshots/pitch-rehearsal/requirements-desktop.png)
- [Evidence desktop](screenshots/pitch-rehearsal/evidence-desktop.png), [mobile](screenshots/pitch-rehearsal/evidence-mobile.png)
- [Decision dialog](screenshots/pitch-rehearsal/decision-desktop.png)
- [Pending monitoring](screenshots/pitch-rehearsal/monitoring-desktop.png)
- [Team calendar desktop](screenshots/pitch-rehearsal/team-desktop.png), [mobile list](screenshots/pitch-rehearsal/team-mobile.png)
- [Timeline desktop](screenshots/pitch-rehearsal/timeline-desktop.png), [mobile](screenshots/pitch-rehearsal/timeline-mobile.png)
- [Notifications](screenshots/pitch-rehearsal/notifications-desktop.png)
- [HR desktop](screenshots/pitch-rehearsal/hr-desktop.png), [mobile](screenshots/pitch-rehearsal/hr-mobile.png)
- [HR trends desktop](screenshots/pitch-rehearsal/hr-trends-desktop.png), [mobile](screenshots/pitch-rehearsal/hr-trends-mobile.png)

## Remaining limitations

Not executed here: hosted/Linux/bundled Chromium; a timed human presentation; comprehensive keyboard,
screen-reader/device/tablet/dark-theme matrix; packet/network cancellation, expired-invitation/resend
and stale-form recovery drills (runbook guidance only); new malicious-document/scanner timeout/recovery
or medical privacy matrices; Excel/LibreOffice; deployment/load/backup recovery; complete lifecycle/
credential adversarial coverage. Scanner definition freshness must be maintained operationally.

Current leave remains inclusive calendar days, not statutory cycles/accrual/workdays/paid entitlement.
#75/#22 qualified review/calculations, production invitation delivery/deployment and Required Medical
absence/payment workflows remain outstanding. #97 stays open. No production-readiness/compliance claim.

[Design ADR](../adr/0024-isolated-current-feature-pitch.md) · [Practical runbook](../operations/pitch-rehearsal.md).
