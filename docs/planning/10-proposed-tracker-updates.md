# Proposed tracker updates — not published

Prepared **21 October 2026**, fetched main after merged PR #133.
No GitHub issue was edited/closed, no project-board item moved and no reviewer/provider contacted.
The [criterion-by-criterion reconciliation](09-issue-reconciliation.md) is the evidence source.
These bodies are proposals for owner approval, not authorisation to implement or publish them.

## 1. Replacement #98 body

Copy only the text between the boundary markers when later publication is authorised.
It replaces the duplicated 8 October roadmap, leading encoding marks and obsolete test-deferral text.

<!-- Start proposed #98 body -->

# HRFlow workplace roadmap: shipped current workflows and remaining gates

Baseline: merged PR #133, 21 October 2026. This umbrella stays open.
[Current roadmap](https://github.com/Onthangaho/HRFlow-Leave-Management-System/blob/main/docs/planning/05-product-roadmap.md),
[issue reconciliation](https://github.com/Onthangaho/HRFlow-Leave-Management-System/blob/main/docs/planning/09-issue-reconciliation.md)
and [readiness review](https://github.com/Onthangaho/HRFlow-Leave-Management-System/blob/main/docs/planning/08-current-readiness-review.md)
separate implementation, verification and owner decisions. A merged slice or green job does not
complete every parent criterion.

## Shipped

- Current JWT/credential/Identity role checks, bounded session-safe refresh and own password change.
- Responsive additive-role shell, deferred feature routes, real personal/Manager/HR workspaces.
- Manager decisions with optional immutable notes, audit timelines and no self/HR decisions.
- Versioned HR employees/policies, confirmed employment facts/fixed weekly history, paginated
  directory/Manager lookup and visible private-image loading.
- Passwordless create/CSV/activation/resend, generation/recipient-aware coordination and safe HR
  feedback. Production transport is still unavailable; private SMTP verification is test-only.
- Private scanned evidence and processed images, company requirements/submission snapshots,
  permitted profiles/theme/preferences, durable in-app notifications and safe scoped exports.
- Offline first-admin, strict single-host configuration, encrypted isolated recovery and reviewed
  existing-account recovery. Restored credentials/invitations are invalidated; startup/workers/CSV
  replay cannot automatically dispatch or recreate accounts.
- Maintained xUnit/Playwright are authorised and retained. Final #133 Ubuntu
  [run 39676852702](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/39676852702)
  passed 115 backend and 51 Chromium cases. This is not comprehensive target-host/scanner/load proof.

## Current disposition — recommendations, not closure actions

- Complete functional slices, closure review recommended: #21, #36, #74, #77, #80, #82, #91,
  #93, #94, #95, #97. Caveats/wording reconciliations remain in the linked issue review.
- Partial: #17, #22, #76, #78, #79, #81, #83, #84, #89, #90, #92, #96 and this umbrella.
- Blocked: #75, #85, #86, #87, #88. Statutory category implementation is not started.
- #78 is already CLOSED but its versioned holiday-source criterion remains missing. Owner must
  explicitly allocate it to #22/#85 or correct scope; closure is not evidence it exists.

## Remaining order and decisions

1. Proposed actionable next slice, #83/#84: owner may add clean supplemental evidence to an existing
   Pending request using its captured requirements, current privacy and atomic binding. No medical
   pay decision, retroactive demand or bound removal. This is a proposed bounded extension.
2. #79 production transport: ADR 0029 D1-D5 remain Pending/Unassigned; D6 release approval is absent.
   Choose/approve provider/account, sender/domain, authentication/custody, privacy/operator ownership
   and uncertain-resend policy before one adapter. Test SMTP and Development pickup are not production.
   #89 production onboarding remains dependent; optional nonblank Gender remains explicitly unsupported.
3. #75: 16 unsigned decisions and 16 Pending review examples; resolve missing authoritative sources
   and qualified labour-law/company/privacy review. Do not treat research as legal approval.
4. Existing #22: first approved calculation/history category; preserve the #78 holiday-source
   remainder. Then #85 and applicable #86/#87/#88, and dependent #84/#90/#92/#96 units.
5. #83 privacy-approved bound retention/holds and #81 target-host TLS/proxy/local persistent
   storage/recovery/load/monitoring acceptance. #82 local tooling is delivered, not host readiness.

Current balance remains configured entitlement minus Approved inclusive calendar days; Pending
reserves nothing. No statutory cycles/accrual, holiday deductions, immutable approval charges,
no-deduction unpaid mode, payment adjudication or UIF benefit calculator is shipped.
Never infer employment/history or relabel request-days as unique absence or statutory charged units.

## Invariants and completion gates

Employee owns personal leave. Manager adds eligible current same-department direct-report decisions,
never self. HR administers/monitors, never approves/rejects. Combined capabilities are additive.
Current active/activated Identity membership/credentials, deferred read snapshots, SQLite writer
reservation, original versions, atomic audits and account/session isolation remain authoritative.
Managers get Medical status only; owner/authorised HR content access remains restricted.

- [ ] Owner accepts exact issue/scope reconciliations before closure or publication; no duplicate issues.
- [ ] Qualified applicable rule decisions and cutover approved before statutory implementation.
- [ ] Missing charge/category/conditional workflows delivered without invented legacy facts.
- [ ] Real employee-data pilot has approved delivery, privacy/retention/holds, custody, operator,
  target-host storage/TLS/proxy/recovery and representative accessibility/volume acceptance.
- [ ] Local, actual hosted and target-host evidence remain separately dated; historical failures stay visible.

Earlier billing blocks are historical. Original #120 was 50 backend pass / 4 fail; corrected #121
was 55 backend / 9 Chromium pass. #133's earlier local 111/112 failure and bootstrap timeout remain
unexplained; cleanup corrections are not proven root causes. Positive real-ClamAV remains separate
local evidence. No production/compliance certification, capacity guarantee or guaranteed RPO/RTO.

Payroll, multi-tenancy, full department CRUD, hard deletion/reactivation and persistent browser
sessions remain excluded. No provider purchase, deployment or email sending is authorised here.

<!-- End proposed #98 body -->

## 2. Evidence-backed closure recommendations

These are recommendations only. Keep exact functional acceptance and release obligations separate.
Link each issue's source, PRs, dated reports and criterion disposition from
[the reconciliation](09-issue-reconciliation.md); do not close merely because the title resembles a feature.

| Issue | Evidence supporting closure | Caveat to include with any approved closure |
|---|---|---|
| #21 | #102 dialogs/500-character notes and atomic audit; #117 retained decisions | Notes optional; no HR decisions; replace obsolete test deferral |
| #36 | Guarded seeder/config and #107 Production gate; #122 first-admin separate | Accept renamed method and backed-up disposable-store remediation, not broad database deletion |
| #74 | #99 endpoint inventory/lifetime/current roles, #104 credential version, retained sessions | Boundary-test debt remains #76; no persistent sessions or instantaneous invalidation of completed reads |
| #77 | #101 responsive shell, #127 stable lazy-route recovery, retained role/session navigation | Full AT/device review is a release check, not a new shell rewrite |
| #80 | #104 Identity validation/version/refresh atomicity and recorded races/browser checks | No forgotten-password/email recovery or HR password editing implied |
| #82 | #108 actual encrypted/cutoff drill, #109/#119 file/image recovery, #133 restored HTTP/SMTP safety | Close local operator criteria only after operator evidence review; retain target-host/custody acceptance under #81/#98 |
| #91 | #114 current-scope/count/date workspace plus #132 keyboard/range/focus evidence | Browser-clock today and client receipt time remain limitations; no WCAG certification |
| #93 | #105 outbox/recipient inbox and #106 delivery-time preferences, #133 restart check | Indefinite outbox/inbox storage remains; no cleanup/email/SMS claim |
| #94 | #106 allowlisted profile, versions, real themes and optional categories | Clarify no mandatory security channel exists; company privacy acceptance is not supplied by code |
| #95 | #119 processing/scanning/publication/recovery; #124 removed-image hints | Positive ClamAV is separately dated local evidence, not inferred from scanner-free CI |
| #97 | #118 fresh isolated browser/persisted rehearsal and safe fixture/runbook | Current-feature pitch only, human delivery untimed; no statutory or production pitch claim |

#17 becomes a closure candidate only after accepting the owner-derived GET/actual POST contract.
For #36/#94/#97, obtain the specified wording/scope acceptance rather than silently rewriting history.
For #82, if the owner intended a broader operational contract, retain that explicit remainder instead.
No recommendation authorises closing an issue in this phase.

## 3. Concise partial/blocked issue updates proposed for approval

### #17

Replace route description with authenticated Employee/Manager GET /api/v1/leave-requests/balances
and owner POST /api/v1/leave-requests/{id}/cancel. Preserve non-owner 403, Pending-only conflict,
current-role protection and one atomic audit. No arbitrary-ID balance alias. Then review closure.

### #22

Suggested title: Implement the reviewed central calculation and historical-charge foundation.
Keep entity/CRUD/calendar behaviour as delivered. Leave accrual, effective inputs/results, holiday
sources, cycle/fraction units, approval charges, shared consumers and truthful legacy cutover unchecked.
Use [the proposal](13-calculation-foundation-review.md), not a duplicate engine issue.
#75 decisions and owner allocation of #78's holiday-source remainder precede category implementation.

### #75 and #85-#88

Record the #125 reviewer pack, 16 unsigned decisions and 16 Pending review examples. No reviewer or
qualified approval is supplied. Preserve missing source retrievals and Van Wyk/legislative/UIF questions.
Each category retains all unimplemented rule/eligibility/payment/history criteria. Generic leave type
names and optional evidence do not complete a category. No statutory calculation activation.

### #76

Replace blanket deferral with the already-authorised maintained xUnit/Playwright convention.
Retain concrete missing real token expiry/not-before/refresh fixtures; controlled 401 refresh tests
are labelled transport tests. Migration/process/session/recovery gates exist. Future signed examples
arrive with categories; do not encode Pending review examples as approved tests.
Record final #133 115/51, distinct earlier runs/failures, historical billing and separate local ClamAV.
Broader AT/load/host/scanner work is verification/operations, not proof the retained suite is absent.

### #78 (already CLOSED)

Preserve the missing fourth-criterion holiday-source revision requirement explicitly. Owner chooses
a recorded transfer to #22/#85 or scope correction; do not auto-reopen or declare it delivered.
Confirmed facts/fixed weekly history are shipped; future starters currently receive normal access
once activated. No account-created hire dates or fabricated historical schedules.

### #79

Mark passwordless lifecycle, hash-only activation, versioned resend, guarded generation/recipient
coordination, safe HR/CSV status and #133 restored-store no-dispatch safety as delivered.
Leave approved production transport/authentication/provider/domain and real delivery verification
unchecked. Accepted is transport acceptance, not mailbox delivery. Uncertain outcomes use explicit
rotated resend, not automatic creation/CSV/resend replay. ADR 0029 D1-D6 remain owner gates.

### #81

Mark strict configuration, health/storage/migration tooling and #122 offline first-admin delivered.
Keep approved target-host selection/cost/operator, real proxy/TLS/persistent-volume/staging
restart/recovery/load/monitoring acceptance open. Do not recommend building first-admin again.
recover-password is reviewed existing-account recovery, not bootstrap; #79 delivery remains separate.

### #83

Mark scanning/quarantine/privacy, draft lifecycle/publication fencing, initial binding and file-aware
recovery delivered. Keep approved bound retention/holds/removal administration open; no auto-purge.
Propose the Pending supplemental-evidence slice below as an explicit scope addition, not a criterion
already present or completed. Legal/payment decisions and retention controls are not bundled into it.

### #84

Mark company modes/type authority, immutable snapshots, version conflicts and ordinary Required
validation delivered. Keep supported conditional rules and reviewed absence/proof/payment workflow
open with #75/#22/#86. Required Medical remains rejected server-side. Later configuration cannot
retroactively invalidate Pending or legacy requests.

### #89

Mark mapping/server preview/per-row atomicity/idempotency, duplicate/manager checks, schedule copying,
formula-safe outcomes and preview PII cleanup delivered. #130 improves delivery feedback; #133
invalidates restored unfinished previews and preserves Created outcomes.
Keep production onboarding dependent on #79. Explicitly document blank-only optional Gender:
nonblank input is rejected. Owner may accept that limit or separately approve minimal storage
purpose/values/visibility/retention; never silently accept/discard or infer eligibility.

### #90, #92 and #96

Keep current-data UI/API/query/export slices delivered, including #131 workforce aggregation.
Preserved charge units, paid/unpaid/cycle semantics and reviewed category outputs remain blocked by
#75/#22/#85. #96 still lacks actual Excel/LibreOffice verification. Do not rename summed clipped
calendar request-days as statutory units or unique absence. No dashboard redesign is needed.

### Common stale wording

The fetched bodies of **every #74-#98 issue**, plus #21's verification paragraph, still contain test
deferral/pending-convention language. Propose replacing it with the current narrow authorisation.
Historical catalogues/reports keep dates and failures; current tracker descriptions should not say
JWT lifetime is disabled, notes/CSV/dashboard are unimplemented, billing currently blocks CI or
first-admin still needs implementation. Update scope notes, not acceptance history by implication.

## 4. Ranked next three implementation slices

### 1. #83/#84 — owner supplemental evidence on existing Pending requests

**Can proceed next:** after normal owner approval of this bounded scope; no unresolved statutory
calculation or external-provider decision is needed. **Value:** an employee can report a request now
and voluntarily supply available evidence later, rather than cancelling/recreating it. This does not
make every absence recordable under the current balance model or implement proof/payment eligibility.
**Effort:** Medium. **Priority:** useful core workflow, ahead of extra dashboard decoration.

**Scope/dependencies:** existing private upload/scanner/publication primitives and captured requirements.
Only current owner Employee/Manager may bind to their own still-Pending request. No HR attach-on-behalf,
request editing, terminal-state changes, new evidence demands, bound deletion or pay review.
Keep legacy Unknown and NotRequested snapshots read-only for this slice with clear explanations.

**Acceptance:**

- A working history/detail action lets only the current active/activated owner Employee/Manager
  upload/select clean drafts for their still-Pending request's captured Optional/Required evidence.
  Draft uploads do not reserve request slots. Atomically count existing bound plus distinct new
  documents inside writer protection: concurrent uploads/binds competing for the last slot cannot
  exceed five, silently truncate the selection or bind an incompatible hidden draft.
- Verify bytes outside writer protection; inside it recheck current actor/credential, ownership,
  Pending state, captured class/mode, clean document versions and idempotency. Concurrent last-slot
  binding cannot exceed the total. Persist actor/request-scoped idempotency and its binding outcome
  in that same transaction. After a lost response, an explicitly retried identical key returns the
  authorised committed outcome without duplicate binding/action records, even if the request has
  since transitioned; it never performs a new terminal-state bind. A changed payload conflicts.
- Configured Medical cannot downgrade. Owner/authorised HR may see content; Manager-role-only DTO/UI
  remains status-only, including errors/filenames. A Manager still has owner access to their own
  documents. Real-data purpose/retention approval remains a pilot gate; no new private payload enters notifications/logs.
- Binding metadata and an append-only document-action history entry commit atomically, recording safe
  actor/request/document identifiers and UTC time for later additions. Authorised history distinguishes
  later additions without rewriting immutable submission requirements or existing leave-transition
  audits, and without creating a fake transition or logging sensitive filenames/content.
- Serialize binding against approval, rejection, owner cancellation and HR deactivation. If a
  transition/deactivation commits first, reject the late bind and preserve eligible owned drafts;
  if binding commits first, retain its trace and enforce the subsequent lifecycle/access rules.
  A changed account/session cannot replay the operation or expose its recovery result.
- Working progress/cancel/retry/error, retained drafts, duplicate protection, refresh and responsive
  light/dark keyboard states use the existing components and scoped caches.

**Verification:** retained API/SQLite/browser tests for snapshot modes/legacy/Medical scope, wrong owner,
quarantine/rejection/missing/changed versions, concurrent last-slot binds, approval/rejection/
cancellation/deactivation interleavings and lost-response recovery after commit. Check identical-key
recovery versus changed-payload conflict and current access denial. Late DB failure leaves zero
partial bindings/action records and no extra transition audit. Actual synthetic clean
ClamAV upload/download where available, separately label controlled scanner/failure fixtures.
Inspect persisted rows/checksums/counts, session barriers and attachment-aware encrypted recovery.
Run affected and complete retained gates sequentially; keep missing platform/AT/scanner evidence explicit.

### 2. #79 — one approved production invitation transport

**Requires owner decisions first:** ADR 0029 D1-D5, actual account/domain/authentication/custody/privacy
and operational owner; D6 controls release. No provider is selected by this recommendation.
**Value:** real HR-created/CSV accounts can receive their own invitation without private operator pickup.
**Effort:** Medium once transport/authentication is specified. It is not a new activation framework.

**Scope:** one approved adapter behind IActivationDelivery and reviewed configuration, preserving
generation/recipient/eligibility guards and delivery outside writer reservations. Keep private pickup
Development-only and tests external-network-free. Default Production gate must stay fail-closed until
approved configuration/enablement; no service connection/sending implied by an implementation prompt.

**Acceptance:** approved TLS/authentication and secret custody; trusted HTTPS origin/sender validation;
bounded one-attempt transport; accurate Accepted/Failed/Unknown states and superseded acknowledgement;
explicit rotated resend after uncertainty; committed accounts/CSV successes cannot replay; token hash
only, no secret/body outbox or routine logs; no automatic GET redemption or exactly-once promise.

**Verification:** extend private loopback MIME/failure/race/canary tests without paid service access;
real provider/domain acceptance/deliverability and target-host egress are separately authorised later.
Re-run creation/CSV/resend/restore regressions and actual HR browser feedback. Local sink success is
not production mailbox delivery. If approvals are absent, do not substitute an insecure adapter.

### 3. #22 with #85 — first approved calculation/history category end to end

**Blocked pending decisions:** qualified applicable #75 rules, company C02/C03 cutover/reversal policy,
privacy decisions and explicit #78 holiday-source allocation. Not authorised to implement now.
**Value:** dependable explainable entitlement and historical charges shared across requests, approvals,
balances and reports. **Effort:** Large; narrow to one reviewed annual method/category, not all statutes.

**Scope:** effective-dated approved inputs and units, shared result/explanations, immutable approval
debit, safe legacy mode/cutover, matching owner/Manager/HR workflow. No generic scripting, payroll/UIF,
silent historical recomputation, account-derived dates or automatic category conversion.

**Acceptance:** signed example boundaries and input provenance; fresh protected approval calculation;
status/one audit/charge/outbox atomic with unique charge identity; future policy/schedule changes cannot
rewrite posted charges; explicit legacy unavailable basis; balances/dashboard/report/export reconcile
with one result and separate calendar spans. Preserve current no Pending reservation until C02 approved.

**Verification:** approved examples only; file-backed migration/rollback, two-process competing approvals,
late-failure atomicity, preserved legacy rows and encrypted recovery; browser explanations/stale versions/
role scope; independently reconcile charge and report totals. No formula code before the review gate.

## 5. Recommended next implementation prompt

> Implement only the owner supplemental-evidence slice of existing #83, respecting #84 snapshots.
> Start from clean fetched main after inspecting both instruction files, current supporting-document
> storage/publication safeguards, submission/history/timeline, snapshots, security and recovery tests.
> Create feat/pending-request-evidence and preserve unrelated work.
>
> Add a working owner action to upload/bind clean drafts to an existing owned Pending request.
> Support captured Optional/Required evidence only; keep Unknown/NotRequested and terminal requests
> unchanged with useful messages. Do not infer legacy requirements or use later type edits to add
> demands. Preserve Medical classification and status-only Manager privacy. No HR upload-on-behalf,
> pay eligibility, statutory rules, bound removal/retention or request-date edits.
>
> Reuse upload/scan/cancel/retry and checksum/publication fences. Verify files outside SQLite writer
> protection, then recheck current credentials/role/owner/Pending/snapshot and document versions
> inside the existing reservation. Atomically count existing bound plus distinct new documents against
> five across concurrent uploads/binds; drafts do not reserve slots. Serialize with approval, rejection,
> owner cancellation and HR deactivation: reject a late bind when those win, preserve the trace when
> binding wins, and retain current access rules. Persist actor/request-scoped idempotency with the bind;
> an explicit identical-key recovery after a lost response returns its committed authorised outcome
> without duplicate binding/action records or a new terminal-state bind. Changed payloads conflict.
> Do not assume LeaveRequest already has
> a dedicated evidence version; justify the minimum contract if one is needed. No automatic callback replay.
>
> Persist append-only binding/action history atomically, with safe actor/request/document IDs and UTC
> time distinguishing later additions. Never rewrite immutable submission requirements or existing
> leave-transition audits, or create a fake transition. Keep eligible drafts and UI input on recoverable failure, invalidate
> only the initiating session and guard late callbacks across same-account relogin/account switching.
> Integrate existing authorised detail/history views, themes, mobile/keyboard states and safe errors.
>
> Retain API/browser/SQLite concurrency/failure/privacy tests and actual clean scanner evidence where
> available; inspect checksums, document/action rows, unchanged request/audit facts and encrypted
> recovery. Separate real ClamAV from controlled transport fixtures and unexecuted checks. Run applicable
> builds/lint/model/audit/diff and affected/complete gates sequentially. Keep instructions identical.
> Return the reviewable diff, evidence and limitations. Do not commit/push/PR, edit/close issues,
> change legal/payment rules, delete bound evidence, connect providers or send external email.

This prompt is a recommendation for a later explicit instruction, not current authorisation.

## Publication guard

Before later publication, refetch main/issues, check intervening changes and obtain approval for
each scope/closure recommendation. Do not publish the proposed #98 body, close issues or move boards
from this document. Do not purchase services, enable delivery, contact legal reviewers or deploy.
