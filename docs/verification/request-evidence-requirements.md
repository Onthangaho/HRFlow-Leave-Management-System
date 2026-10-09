# Request evidence requirements: executed verification and limits

Checked 9 October 2026 on feat/request-evidence-requirements, based on fetched origin/main
b7d2990c1c3e6a03a20b64bf89beab8258c4b847. GitHub confirms PR #109 MERGED
at 2026-10-09T17:52:29Z. Initial working tree was clean; original stores were not altered.
Issues #84/#22/#75 and the legal requirements distinction were read. #84 remains open.

## Reproducible local approach

Temporary tooling, synthetic database copies, private TLS/Identity configuration and synthetic
PDFs remain outside the review files. Real compiled API plus Vite client and headless installed
Chrome/Playwright were used; interaction checks are not static rendering. Real ClamAV INSTREAM
scanned uploaded synthetic PDFs. Production-mode local API configuration uses the existing
single-host/private-store safeguards; this is not a deployment or legal-compliance claim.
Self-contained API sequences log in as the disposable roles, configure types through management
APIs, submit using selector versions, and inspect SQLite request/document/audit rows. Injected
failures and lifecycle seeds affect only the disposable database. Reproduce the scenarios below
with a private fixture and the contracts/runbooks; no passwords/tokens are included here.

## Executed HTTP and persisted-row checks

- Optional description/evidence: blank description becomes null; no evidence required; snapshot
  persists and exactly one Submit audit is present.
- Required description rejects whitespace-only input. Required Ordinary evidence rejects no
  documents. Oversized 1001-character description fails without request/audit writes. Exact
  1000 characters persist with clean evidence and one audit. Trimmed HTML-looking text persists
  as text; rendering does not create HTML elements.
- Required evidence binds a real clean, owned Ordinary upload. Wrong-owner evidence returns safe
  404; already-bound evidence returns 409. Quarantined, Rejected, Missing and ScanUnavailable
  seeded metadata each return 409 with no request/binding. These latter cases are lifecycle
  validation checks, not newly executed scanner fault/detection experiments.
- NotRequested rejects retained nonblank text and documents; drafts remain unbound. Explicit
  empty submission succeeds. Stale type and shared-policy versions return 409.
- Medical + Required configuration returns 400 without changing the type version. Ordinary
  evidence cannot bind to a Medical type. Optional Medical absence submits without proof.
  Current assigned Manager receives null description, null MIME and cannot download Medical
  content; authorised HR receives the submitted text. This does not prove legal absence/payment
  eligibility: existing fixed entitlement/reporting rules still apply.
- Type configuration changed after submission does not rewrite stored snapshot JSON. Approval of
  the earlier request without documents still returns 204 after evidence becomes Required;
  current balance/overlap validation is preserved.
- A temporary SQLite trigger fails late audit persistence on an otherwise eligible submission:
  HTTP 500 in this deliberately injected failure, unchanged request/audit counts, and reusable
  unbound draft. No request, snapshot, binding or Submit audit partially survives.
- Legacy timeline returns null snapshot. Employee management-route denial and HR-only approval
  denial remain 403. Missing replacement requirement fields return 400 without version changes.
- A separate Node/SQLite writer process holds BEGIN IMMEDIATE, changes type version, then commits.
  The queued real API submission returns 409 with unchanged request/audit counts. This explicitly
  checks fresh authoritative reads across processes; it is not a two-API approval/configuration
  stress test.

## Executed real-browser checks

- HR creates a policy/type and edits description/evidence modes through actual forms. Medical
  Required validation retains form input. Current edit forms and requirement summaries were
  checked on desktop/mobile without page overflow.
- Employee selects the new type, enters dates/description, uploads a PDF through the real chooser,
  waits for Clean, and explicitly selects it. Medical upload class is locked for a Medical type.
- HR changes configuration while Employee's form is open. Actual submission receives 409 and
  retains dates, text and selected draft. Explicit reload updates requirements without clearing
  them. NotRequested hides new upload controls, retains the checkbox for explicit deselection,
  and prevents submission with the retained attachment. Another explicit review returns to
  Optional evidence; submission then succeeds.
- Persisted request has the submitted description, Required-description/Optional-Medical snapshot,
  one bound Medical document and exactly one Submit audit. Personal history links to the actual
  authorised timeline. Literal angle-bracket text does not become a bold/HTML element.
- Manager uses mobile navigation and the actual personal form to submit without Optional Medical
  evidence; snapshot and one audit inspected. Keyboard textarea focus, reduced-motion context and
  mobile no-overflow checks were exercised. Employee/HR use dark theme and Manager uses light.
- Real delayed upload cancellation keeps Cancel reachable; type selection and submission remain disabled while processing. Refresh/recovery remains explicit, with no automatic binding.
- Delayed real submission responses across same-account relogin and different-account switch
  produce exactly one server request/audit, no automatic replay, and no old draft or success
  notice in the replacement session. Submit is disabled while the response is outstanding.

Browser verification caught a missing dynamic textarea and an ambiguous evidence-select label;
both were fixed and the complete HR/Employee/Manager sequence subsequently passed. Temporary
harness assertions/selectors were also corrected (approval is 204, hard navigation reload loses in-memory login,
and login after sign-out can return to the original route); they are not shipped tooling.

## Migration and recovery executed

Final migration applied to another independent pre-change copy. Every original column/value across
19 tables and 15 legacy requests matched; defaults were NotRequested/Optional/Ordinary and all
legacy descriptions/snapshots null. SQLite integrity_check returned ok; foreign_key_check empty.
No employee, role, lifecycle, credential, request, attachment, audit, settings or inbox history was
invented or rewritten. Model check: No changes have been made to the model since the last migration.

With fixture API writers stopped, the existing encrypted operator tools backed up and restored
into an isolated destination with required security invalidation. All LeaveTypes, LeaveRequests
(including descriptions/snapshots), SupportingDocuments and DocumentAccessEntries matched exactly.
Actual clean/quarantine blobs matched byte-for-byte; unavailable/quarantine states were retained.
A missing required blob refused backup without publishing a successful package. This repeats the
attachment-aware drill, not future scanning/retention/legal-hold administration or a production drill.

Rollback drops the new configuration/description/snapshot columns. Newly recorded facts would be
lost; re-upgrade cannot reconstruct them. Rollback/re-upgrade was not executed in this slice.

## Build/security checks executed

- dotnet build HRFlow.sln --no-restore: Build succeeded, 0 warnings, 0 errors.
- dotnet ef migrations has-pending-model-changes --project src/HRFlow.Infrastructure --no-build
  with explicit disposable connection: no pending model changes.
- npm run build: passed, 823 modules; main JS 541.79 kB (162.41 kB gzip). Existing >500 kB
  Vite warning remains; no unrelated optimisation.
- npm run lint: exit 0; existing useAuth.tsx:223 Fast Refresh export warning remains.
- dotnet list HRFlow.sln package --vulnerable --include-transitive: no vulnerable packages in
  Domain, Application, Infrastructure, API or Operations given the current sources.
- npm audit: found 0 vulnerabilities. No package/lockfile changes.
- Standard git diff --check, new-text whitespace/secret review and instruction byte comparison
  are recorded after final documentation review. No test framework or committed test files added.

## Synthetic screenshots

All screenshots show synthetic accounts/records, no credentials, tokens, private file contents or
local machine paths. Browser PNGs, each under 300 kB; full-page mobile views are intentionally tall.

- [HR desktop](screenshots/request-evidence-requirements/hr-type-desktop.png)
- [HR mobile](screenshots/request-evidence-requirements/hr-type-mobile.png)
- [Request desktop](screenshots/request-evidence-requirements/request-desktop.png)
- [Request mobile](screenshots/request-evidence-requirements/request-mobile.png)
- [Snapshot desktop](screenshots/request-evidence-requirements/snapshot-desktop.png)
- [Snapshot mobile](screenshots/request-evidence-requirements/snapshot-mobile.png)
- [Manager mobile, light theme](screenshots/request-evidence-requirements/manager-request-mobile.png)

## Explicit outstanding limits

#22/#75-dependent statutory cycles/accrual, work schedules, conditional medical proof/payment,
legal review and unrestricted absence recording are not implemented. HR must classify medical
purpose correctly; names cannot establish it. Required Medical submission gates are unsupported.
#84 remains open for these dependencies; no compliance certification or production readiness.

Not rerun: migration rollback/re-upgrade; full two-API approval-versus-configuration/binding-removal
races; scanner outage/timeout/malware corpus and publication/cleanup fault matrix; all inactive,
revoked-role and credential race combinations; delayed uploads/downloads (delayed submission was
checked); exhaustive screen-reader/contrast/keyboard-dialog checks; Linux/load/power-loss checks;
full HR monitoring/report/employee lifecycle/cancellation and notification regressions. Existing
#83 evidence remains [separate historical verification](private-supporting-documents.md), not new
passes for this slice. No automatic bound-document deletion, new retention policy, payment engine,
profile images or deployment were performed during implementation verification.

## Finalisation review (9 October 2026)

Reviewed the complete source, migration, documentation and synthetic screenshot scope for #84.
No additional code correction was required. Finalisation reran working/staged whitespace and
sensitive-content checks, inspected the staged file inventory, and confirmed the instruction
files are byte-identical. All seven screenshots are below 300 kB and contain synthetic data.
The browser, persisted-row, writer-contention, migration, recovery, build and vulnerability
results above were executed during implementation; they were not rerun during this documentation
and staging review. The outstanding checks above remain unexecuted. #84 is referenced without
closure because the #22/#75-dependent statutory and conditional-proof work remains outstanding.
