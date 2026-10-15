# Proposed tracker updates — not published

Prepared 15 October 2026 against merged main through PR #121. **No GitHub issue was edited or closed.**
The [reconciliation](09-issue-reconciliation.md) contains the fetched criteria and evidence for all
29 items. These are proposed contracts for owner review, not authorisation to start implementation.

## #98 — proposed replacement body

### HRFlow South African workplace roadmap: shipped slices and remaining release gates

**Baseline:** merged PR #121, 15 October 2026 SAST. This umbrella remains open. It coordinates
focused existing issues; a merged partial slice does not close a statutory/operational parent.
See the repository's current readiness review and issue reconciliation in docs/planning.

**Shipped current functionality:** current-role/JWT/credential-version enforcement; responsive
additive-role shell; immutable decision notes/audit timelines; secure local activation and password
change; employee lifecycle and employment facts/fixed schedule history; CSV preview/confirmation;
private scanned evidence with medical restrictions; immutable company requirement snapshots;
recipient-scoped notifications/preferences; private processed profile images; personal/Manager/HR
current-data workspaces; safe scoped CSV exports; retained xUnit/Playwright gates; isolated synthetic
pitch rehearsal. PRs #99–#119 provide slice-specific evidence. These are not statutory calculations
or proof of target-host production readiness.

**Permissions:** Employee owns personal leave; Manager adds only eligible current same-department
direct-report decisions, never self. HR administers/monitors, never approves/rejects. Combined
roles add capabilities. Current active/activated Identity membership and credential versions remain
authoritative. Preserve snapshots, writer reservations, original versions, audits and session isolation.

**Remaining dependency order:**

1. #81 offline first-admin provisioning (actionable without paid hosting); #75 qualified requirements
   decision register in parallel as an external review activity.
2. #76 incremental regression: merged #121 final hosted Ubuntu/Chromium gates passed 55 backend /
   9 browser cases. Earlier billing blocks are historical; do not weaken protection or generalise
   retained gates to all Linux deployment/scanner/recovery scenarios.
3. #22 reviewed central calculation/history contract; #78 remaining holiday/cycle inputs; #85 annual,
   then applicable #86/#87/#88 categories. Do not infer entitlement from Gender or account creation.
4. #79 approved production delivery; #89 production onboarding depends on it. No SMTP or service
   purchase is implied. Local private pickup is Development-only.
5. #83 privacy-approved bound-evidence retention/holds, #82 target-host recovery/access rehearsal,
   #81 TLS/proxy/persistent-volume/load/monitoring acceptance.
6. #84 remaining conditional proof/payment versus absence, and #90/#92/#96 charged-unit completion
   follow reviewed #75/#22 contracts. Current-data slices remain usable and honestly labelled.

**Closure candidates after evidence review:** #21, #36 (safe migration wording), #74, #77, #80,
#91, #93, #94 (no invented mandatory security channel), #95, and #97's current-feature pitch scope.
#17 first needs owner-derived GET/POST transport reconciliation. #82's local operator tooling is
delivered; target-host operator acceptance remains a release gate independent of new functionality.
Do not auto-close any item from this umbrella body.

**Acceptance:**

- [ ] Owner accepts issue-by-issue functional versus verification reconciliation; tracker reflects
  accepted scopes without duplicate implementation issues.
- [ ] Qualified applicable requirements/decisions approved before statutory code; remaining legal
  questions stay visible, with no compliance certification from coding-agent research.
- [ ] Applicable calculations/history migrations are delivered and evidenced; legacy Unknowns remain honest.
- [ ] Real employee-data pilot has approved privacy/custody/retention, access recovery, supported
  host/TLS/storage/monitoring and operational owners; invitation/provisioning gates resolved.
- [ ] Retained local and actual hosted results remain separately dated; synthetic pitch limitations
  are explicit. No invented readiness percentage, capacity, guaranteed RPO/RTO or production claim.

**Exclusions:** payroll, multi-tenancy, full department CRUD, hard deletion/reactivation and persistent
browser sessions remain out. No new dependency framework collection or generic rules engine.

## Specific existing-item edits proposed for approval

### #17 — transport-only reconciliation

Replace route description with: “Authenticated Employee/Manager GET
`/api/v1/leave-requests/balances`; owner POST `/api/v1/leave-requests/{id}/cancel`.
Identity is server-derived. Current-role check and writer reservation precede cancellation reads.
Non-owner cancellation returns 403, non-Pending returns 409, and successful cancellation records
one transition audit atomically.” Preserve the original 403 criterion. After acceptance, closure is
supported by current handler and retained audit evidence; no arbitrary-ID balance alias needed.

### #21, #36, #74, #77, #80, #91, #93, #94, #95, #97 — completed scope summaries

Add the respective evidence links from the reconciliation and propose closure review rather than
leaving old unchecked implementation checklists unexplained. For #21 remove the old automated-test
deferral; optional note workflow shipped in #102. For #36 name guarded `SeedIdentityAsync` and
replace unrestricted database deletion with backed-up, disposable-store-only remediation. For #94
state that only optional implemented in-app categories exist; no mandatory security channel is claimed.
For #95 retain scanner definition date and explicit remaining matrix gaps. For #97 keep local pitch
separate from statutory/production readiness and human presentation time separate from automation.
Keep #93's retention/storage-growth decision visible in operations even if the messaging slice closes.

### #22 — proposed revised title and remaining scope

Title: “Implement the reviewed central leave calculation and historical charge foundation”.

Entities, policy CRUD and a shared current calendar-day calculator already exist. Remaining scope:
versioned/effective rule results and explicit units shared by submit/approve/balances/reporting;
reviewed cycle/schedule/holiday contracts; immutable approved charged facts; truthful legacy Unknown
handling and migration; concurrent approval invariants. Prerequisite #75 sign-off and #78 inputs.
No hardcoded calendar-month day counts or generic scripting. Acceptance requires reviewed worked
examples, preserved historical decisions, atomic charges/audits, consistent views and migration tests.
Keep open; do not replace it with a duplicate accrual-engine issue.

### #75/#85/#86/#87/#88 — external gates and blocked implementation

Record “No qualified sign-off found as of 15 October 2026.” Name a reviewer/decision owner when
appointed. Preserve source checked dates; do not tick legal criteria from merged company-settings
work. Categories remain blocked pending relevant reviewed examples, not abandoned/superseded.

### #76 — narrow the obsolete deferral

Replace deferral language with: “The focused maintained xUnit/Playwright convention is authorised
and shipped. Extend existing disposable migrated SQLite/process/session fixtures. No new framework
collection.” Link tests/README.md and dated #117/#119 results. Outstanding: full expiry/refresh
boundary coverage and broader platform/AT/load/scanner matrices. #121 final hosted Ubuntu execution
passed 55 backend / 9 Chromium cases after fixing #120's original 50/4. Historical billing failures
remain separate. Positive real-ClamAV checks are still local evidence.

### #78/#84/#90/#92/#96 — preserve remaining parent contracts

Note that #78 is already CLOSED (9 October 2026); its original holiday-source criterion remains absent.
Propose explicitly transferring that remainder to #22/#85 or an owner-reviewed scope correction, not
automatic reopening. Mark confirmed employment/fixed schedules, company requirement snapshots, current-data dashboards
and current-calendar-day exports as shipped with their PR/report links. Keep holiday/cycle/conditional
medical and preserved charged-unit criteria unchecked. State #75/#22 dependencies precisely.
Future start dates currently do not postpone activated access. Legacy snapshots remain Unknown.
Do not convert these parents to “complete” simply by renaming calendar days statutory charged days.

### #79/#81/#82/#83/#89 — local delivery versus company operation

Record local activation/CSV, fail-safe configuration, encrypted recovery and scanned evidence as
shipped. First-admin provisioning is absent (#81); `recover-password` is an existing reviewed-identity
recovery command, not a bootstrap shortcut. Production delivery remains absent (#79/#89).
For #82, replace obsolete “repeat after files exist” with actual document/image recovery links,
while retaining target-host/custody/access drill as operator acceptance. For #83 record approved
retention/hold administration as missing functionality/decision, separate from unexecuted scanner
permutations. Proposed narrower next #81 slice is specified in the readiness review; no new issue.

## Publication guard

Before any later authorised publication, reread each issue and current main, compare intervening
changes, and obtain acceptance of revised scopes. This file is not an instruction to close issues,
change project boards, purchase delivery/hosting, resolve billing or implement the next slice.
