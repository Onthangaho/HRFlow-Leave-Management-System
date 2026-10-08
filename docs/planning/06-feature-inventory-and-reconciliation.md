> 9 October 2026: PR #104 is merged. Issue #93 durable in-app notifications is locally implemented for independent review: see [design](../adr/0013-durable-in-app-notifications.md) and [verification/limitations](../verification/in-app-leave-notifications.md). Preferences remain #94; no email/SMS or deployment. Earlier entries below are historical status.

> 9 October 2026 update: PR #103 is merged. #80 password change/session revocation is locally implemented for independent review on feat/password-change-session-revocation, prepared for PR review and not merged or deployed. See [ADR 0012](../adr/0012-password-change-session-revocation.md) and [executed verification/limitations](../verification/password-change-session-revocation.md). Own-account change revokes prior JWT/refresh generations atomically; broader profile/preferences remain planned. Earlier implementation notes below are dated historical status.

# Feature inventory and backlog reconciliation

> Current-state update: PR #99 merged on 8 October 2026. Its JWT lifetime validation, explicit 30-second skew, current active Identity permission checks and session-safe bounded refresh supersede the authentication gaps recorded at the PR #73 baseline below. See ../security/endpoint-authorization-inventory.md and ../verification/jwt-current-permissions.md. #74 remains open; historical verification is not rerun or replaced here.

> Current workflow update: PR #101 merged the shell #77. Optional Manager decision notes (#21) are now locally implemented for review, extending the existing queue and AuditEntry, with authorised personal/timeline display and server-confirmed outcomes. See [decision contract](../adr/0010-manager-decision-notes.md) and [verification](../verification/manager-decision-notes.md). #21 was renamed/reworded to its remaining scope and stays open. The review baseline below remains historical; no legal/test gate is bypassed.

Reviewed 8 October 2026 against fetched `origin/main`, **f47e20c4245ae71384a4a7b0fcbc80a9274f750f**.
The starting working tree was clean. Documentation branch: `docs/hrflow-south-africa-product-roadmap`.
[PR #73](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/73) was verified
**MERGED**, 8 October 2026 at 17:21:34 UTC. This review does not rerun earlier product verification.

## Historical baseline capabilities confirmed in source (PR #73)

The following evidence describes the original review, not current-main regressions. See
the implementation updates above for JWT validation, shell, notes and local activation.

- Identity login/rotating hashed refresh tokens; in-memory client sessions; logout clears protected
  caches; Axios can refresh an expired access token and retry once in the same session; invalid refresh
  ends that session, and explicitly non-replayable operations opt out. Combined roles provide additive navigation.
  **Important qualification:** `src/HRFlow.Api/Program.cs` sets `ValidateLifetime = false`.
  JWT expiry is therefore not enforced as the older authentication plan implies. Every protected
  request checks persisted active status, but that is not a replacement for expiry/current roles.
- HR employee create/edit, full role collections, explicit Preserve/Assign/Clear manager assignment,
  version conflicts, relationship/cycle/direct-report/last-active-HR safeguards; profile and Identity
  saves share one transaction. Initial creation currently takes a password; secure invitations
  are **not** implemented. Department selectors exist; full department CRUD does not.
- Deactivation blocks login/refresh/old-token protected access, rotates the version, cancels Pending
  leave atomically with acting-HR reason audits, preserves terminal history and reporting links.
  HR search/filter, deactivation confirmation and inactive read-only details shipped in PR #70.
- Shared policy/type CRUD with versions, normalized type uniqueness, reference-restricted deletion,
  explanatory HR forms and a real Employee/Manager type/date submission form (PRs #67/#68).
  No general request description, configurable proof or document upload exists.
- Personal balances/history/Pending cancellation; eligible Manager queue/approve/reject; HR's
  organisation Pending monitoring is read-only. Self-decision and HR-only decision remain forbidden.
  Approval revalidates current policy, history and reporting under writer protection (PR #64).
- Approved current direct-report month view; inactive history labelled, distinct active people
  counted once per period, not daily absence. HR period/department reports clip calendar request-days,
  distinguish inactive contributions and use CURRENT department; no historical department snapshots.
- AuditEntry remains the sole domain transition/persistence mechanism. New submission null→Pending
  events, correlation IDs, truthful legacy gaps, UTC timelines, current actor names, HR-only sensitive
  deactivation context and owner/Manager/HR detail scopes shipped in PR #73.
- Shared accessible controls/dialogs and responsive feature pages exist. HomePage is still a
  role-link landing page with account details, not API-backed role dashboards or a shared app shell.

Evidence: Domain `Employee`, `LeavePolicy`, `LeaveRequest`, `AuditEntry`; Application
`Features/LeaveRequests` and management/reporting features; Infrastructure `EmployeeManagementService`,
`AuthService`, `LeaveApprovalAuthorizationService`, `SqliteWriteTransaction`,
`SqliteTeamLeaveReadTransaction`; API controllers/Program; client feature pages/auth/query-key code.
See [ADRs 0002](../adr/0002-consistent-leave-decisions.md), [0003](../adr/0003-hr-employee-management.md),
[0004](../adr/0004-leave-policy-management.md), [0005](../adr/0005-employee-deactivation.md),
[0006](../adr/0006-manager-team-leave-summary.md), [0007](../adr/0007-hr-department-leave-reporting.md)
and [0008](../adr/0008-leave-request-audit-timeline.md).

## Incomplete foundations and risks

1. **Current-role checks are uneven.** New management writes, decisions, team summary, department
   reporting and timelines recheck persisted permissions inside protection/snapshots. Older queue,
   HR monitoring, HR employee reads, personal history/balances/selector and submission/cancellation
   paths must be reviewed end-to-end: controller JWT claims alone can outlive role removal.
   `GetPendingLeaveRequestsQueryHandler` trusts controller scope, and submission/cancel check active
   ownership without rechecking the owner's current Employee/Manager membership. Do not claim
   uniform live permission enforcement. Proposed security work must cover **all protected routes**.
2. **Current balance is not a statutory engine.** `LeaveBalanceCalculator` subtracts all Approved
   inclusive calendar durations from current `DefaultBalance` for each type. No employment start,
   schedules, cycles, accrual, holiday exclusions, effective dates or historical charges exist.
   Pending reserves nothing; Rejected/Cancelled consume nothing. Current-policy reductions can
   produce a negative displayed balance while preserving historical approvals. Zero entitlement
   blocks positive requests, including the nominal Unpaid seed; it does not mean unlimited unpaid leave.
3. **Reasons are not manager notes.** AuditEntry.Reason currently carries HR deactivation context;
   approve/reject commands and UI accept no decision note. Do not repurpose that sensitive field
   indiscriminately or add a competing audit system.
4. **No production claim:** no newly executed load/accessibility audit, deployment or restore drill.
   SQLite-specific writer reservation and read snapshots are deliberate, not portable by configuration.
   Automated tests are deferred; earlier disposable checks are evidence, not a retained regression suite.
5. Missing: CSV onboarding, secure activation/password change, request descriptions/evidence,
   private files/profile images, preferences, durable notifications, calendar UI, safe exports and
   real role dashboards. These are planned, not partially advertised as working settings.

## Existing issue recommendations: no closures or edits performed

### #17 — GET balance + PATCH cancel endpoints

[Verified issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/17).
Implemented: owner-derived GET `/api/v1/leave-requests/balances`, POST
`/api/v1/leave-requests/{id}/cancel`, protected owner validation, 403 for another owner's cancellation,
409 for terminal requests, atomic status/audit persistence. See `LeaveRequestsController`,
`GetLeaveBalancesQuery`, `CancelLeaveRequestCommandHandler`, PRs #59/#60/#64 and ADR 0002's
real HTTP/two-process evidence. Existing issue describes an arbitrary employee-ID balance route
and PATCH, which are **not** the shipped contract.

Recommendation: update the issue description to acknowledge the accepted safer owner-derived GET
and actual POST contract, then close after reviewer confirmation of that contract and its stated
non-owner 403 criterion. Do not add duplicate routes just to resemble old wording. Security follow-up
is tracked separately, not hidden by calling this original contract unfinished. No closure now.

### #21 — Manager approval queue UI

[Verified issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/21).
Implemented: eligible Pending queue, inline decisions, safe errors, disabled duplicates and
authoritative query invalidation without a page reload (PRs #58/#64). Missing: the description's
decision note; the original “optimistic mutation” wording also differs from intentional server-confirmed
updates. Closed #18's note wording does not prove notes exist.

Recommendation: **keep and reuse #21**, rename/update to the remaining complete decision-note workflow;
do not create a duplicate. Proposed scope: optional trimmed note up to 500 characters, no diagnosis
prompt, Application validation + existing domain/audit persistence in the decision transaction,
nullable metadata for legacy decisions, queue confirmation form and authorised timeline display.
Make rejection-note requirement an explicit product decision rather than silently imposing it.
Notes are visible only to owner/current eligible Manager/HR; never log their text. One terminal event
and note persist together; forbidden/stale/policy-failed/rolled-back decisions persist neither.
Use current rules/no self/no HR override, retain draft on conflict, no automatic replay; invalidate
same-session queue/history/timeline/team/report data. Verify two-process decision races and all
role/privacy/UI boundaries. Essential for pitch; Medium. Depends on uniform current authorization;
does not require statutory engine work. Replace obsolete optimistic AC with confirmed invalidation.

### #22 — LeaveType and LeavePolicy entities

[Verified issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/22).
Implemented: entities/mappings/migrations, current configurable entitlement and overlap, shared
policies, API/UI versions and CRUD. `LeavePolicy.DefaultBalance` is read by the calculator rather
than a hardcoded allowance. **Missing:** actual accrual rule in the issue's description/AC; a fixed
integer balance is not accrual. PRs #50/#67/#68 and ADR 0004 establish the existing simpler model.

Recommendation: keep #22, update its remaining scope to the **shared effective-dated calculation
foundation**, then deliver leave-category slices through dependent issues. Proposed foundation:
reviewed rule kinds/units/cycle anchors, effective-dated employment/schedule/policy references,
one calculation result shared by submission, approval, balances and reporting, approved charged-unit
snapshots and explained results. Retain legacy calendar basis/unknown historical rule truth;
never backfill fabricated employment dates or silently reinterpret old approvals. Schema upgrade
must produce a dry-run legacy exception report and preserve requests/audits. HR configuration/forms
remain versioned. Pending validation uses reviewed current applicable rules; submission snapshots
are historical facts, not a guarantee of later approval. Explicit unpaid mode, company enhancements,
precision and cycle-spanning allocation are designed here; category rules need separate legal sign-off.
Essential for pitch; Large. Depends on legal review and employment/schedule foundation. Verify
boundary examples, migration/rollback, cross-process balance races and unchanged role/audit/session
behaviour. No annual reset/carry-forward guess. No parallel new “accrual engine” duplicate issue.

### #36 — Harden dev admin seeding per CodeRabbit review

[Verified issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/36).
Implemented: PR #37, self-guarded `DevelopmentIdentitySeeder.SeedDevelopmentAdministratorAsync`,
configurable `Seeding:HrAdministratorPassword`, README configuration/troubleshooting. Current startup
`DatabaseSeeder.SeedAsync` is itself Development-guarded, migrates before protected seeding, and
does not reprovision existing accounts/roles or reactivate inactive profiles (later PRs #67/#69).

Recommendation: reconcile exact method criterion against both guarded implementations, retain
historical PR #37 evidence, then close after review. No remaining product implementation found.
This documentation phase makes the README's old delete-and-restart note safe: reset only a known
disposable local store; preserve/back up any store with needed data and review migration preflight.
No automatic legacy repair is introduced. Do not delete real data to satisfy old troubleshooting text.

## Other historical tracker discrepancies

Closed #19 described HR deciding leave; current rules correctly prohibit it. Closed #20 mentions
PATCH; actual decision routes are POST. Closed #31 proposed AuditLog/interceptor; the accepted
implementation deliberately reuses AuditEntry instead. Closed #5 described Axios 401 replay;
current authentication supports valid same-session refresh and one bounded retry, with explicit non-replayable exceptions. Closed #15's general reason
is not implemented in the minimal form; planned configurable descriptions address that gap.
These are documentation/reconciliation notes, not authority to reopen/close or rewrite issues now.

## Evidence quality and limitations of this review

Read the eight existing verification reports: dependency security, deactivation backend/UI,
policy backend/UI, team summary, department reporting and audit timeline. Earlier reports contain
real HTTP/persisted-row and Chromium checks; initial API-only/static/adapter checks remain separately
labelled. Later browser addenda supersede earlier *unexecuted* UI items, not their original evidence.
Do not rewrite those reports as newly executed checks. Their explicit limits include physical devices,
other engines, formal accessibility, production load, forced read interleavings and audit migration Down.

This phase executed source/document/tracker inspection, fetch/branch creation and documentation
checks only. No real application/database/browser workflow, build, npm audit, new migration or product
test is claimed. Qualified labour-law review and a complete up-to-date legal consolidation remain
external gates. [Requirements](04-south-african-leave-requirements.md) and
[roadmap](05-product-roadmap.md) distinguish proposed capabilities from shipped behaviour.

## Planning-phase verification record

- `git status` was clean on main before fetching/branching. Base `f47e20c` is the PR #73 merge;
  `gh pr view 73` returned MERGED with the recorded UTC merge time.
- Open/relevant closed issue and merged-PR inspection found only #17/#21/#22/#36 open before
  expansion. Their titles/bodies were read; none was edited or closed.
- Created **24 focused issues #74–#97**, then **umbrella #98**. Retrieved all open issue bodies
  again and compared each new title/body with the local catalogue: **24 matched**. Dependency
  creation order was checked; existing #21/#22 reuse is explicit. Umbrella existence/linked
  issue list was checked. No unrelated project-board action was taken.
- Both instruction files were compared byte-for-byte: **identical**. The eleven intended
  documentation files were checked for local link targets, balanced fences, final newlines and
  trailing whitespace; new documents were scanned for credential/token/machine-path patterns.
- `git diff --check` passed. Git emitted normal LF→CRLF working-copy notices, not whitespace
  errors. New-file whitespace was checked separately because an ordinary diff omits untracked files.
- Temporary issue tooling initially could not run through the Windows Python alias; Node tooling
  succeeded instead. A credential scan initially falsely matched the words “bearer JWTs”; its
  token pattern was corrected and the complete check rerun. Neither is a product check/pass.
- New-file Git checking caught an extra blank line at the catalogue's end; it was removed and
  checks rerun. No-index diff exit 1 indicates new content, not a whitespace failure.
- No builds, npm audit, application HTTP/SQLite/browser checks, automated tests, migration or legal
  sign-off were executed in this phase. Earlier product evidence remains linked and dated separately.

Only documentation changes remain for review; temporary issue-creation/inspection tooling is not
part of the deliverable. No commit, push, PR, merge, deployment or issue closure was performed.
