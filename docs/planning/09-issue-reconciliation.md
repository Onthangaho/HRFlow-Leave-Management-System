# Issue reconciliation after merged PR #133

**Reviewed 21 October 2026**, clean fetched origin/main 59d20de0deb34ae20abf5b00fd43090edb2813a3; PR #133 merged at 21:25:12 UTC. Branch: docs/post-133-backlog-reconciliation.
All 58 current issues and 75 merged PR records were fetched read-only; the 29 roadmap contracts below were examined individually. GitHub states: 28 OPEN, #78 CLOSED. No tracker edits/closures or board moves.

## Method and evidence boundaries

- Classifications are **Complete**, **Partial**, **Blocked**, **Not started**. Complete means the stated functional slice has supporting source and criterion-specific evidence, not every platform/load permutation or a compliance claim. Partial preserves real parent or contract gaps. Blocked names unmet decisions/prerequisites. Category code for #85-#88 is not started, but classified Blocked rather than hiding its prerequisites.
- 11 Complete, 13 Partial, 5 Blocked, 0 Not started. These are issue counts, not readiness percentages. Closure recommendations require owner review; actual GitHub states are unchanged.
- Acceptance text is from the current fetched bodies, checkbox formatting removed and numbered A1 onward. It is not reconstructed issue edit history. Stale deferral and route wording remain visible as contracts to reconcile, not instructions to reproduce unsafe behaviour.
- Source/ADRs, actual merged PRs, retained assertions and dated reports were compared. A report or a green job alone does not prove every criterion. Tests, builds, benchmarks, scanners and legal research were **not rerun** here; historical reports stay unchanged.
- Final #133 head 8fc1fa0 [hosted Ubuntu run 39676852702](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/39676852702) passed 115 backend and 51 Chromium tests. Earlier local 111/112 failure and startup timeout remain unexplained; cleanup corrections are not proven root causes. Earlier billing blocks are historical, not the current CI state. Positive real-ClamAV remains separately dated local evidence.

## Current classification index

| Issue | Fetched state | Classification | Principal remainder / scope boundary |
|---|---|---|---|
| [#17](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/17) | OPEN | Partial | No missing cancellation behaviour found |
| [#21](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/21) | OPEN | Complete | None found against A1-A8 |
| [#22](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/22) | OPEN | Partial | A1 accrual rules are absent: DefaultBalance is a fixed configured quantity |
| [#36](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/36) | OPEN | Complete | None for safe seeding |
| [#74](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/74) | OPEN | Complete | None found against the functional contract; the issue problem statement ValidateLifetime=false is obsolete. |
| [#75](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/75) | OPEN | Blocked | A1-A4 qualified decisions/sign-off and missing-authority reconciliation remain absent |
| [#76](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/76) | OPEN | Partial | Focused real-bearer/refresh regression extension prepared locally; exact clock equality, broader categories and qualified future calculation fixtures remain outside this slice |
| [#77](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/77) | OPEN | Complete | None found against the shell contract |
| [#78](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/78) | CLOSED | Partial | A4 versioned holiday sources are not implemented |
| [#79](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/79) | OPEN | Partial | No approved production SMTP/API adapter or authentication/provider integration |
| [#80](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/80) | OPEN | Complete | None found against A1-A4 |
| [#81](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/81) | OPEN | Partial | Owner-approved target topology/hosting and operational ownership/costs still unsettled |
| [#82](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/82) | OPEN | Complete | None found against A1-A4 local tooling/drill criteria |
| [#83](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/83) | OPEN | Partial | A4 approved bound-evidence retention/legal-hold/expiry administration remains absent; bound removal rejects without policy |
| [#84](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/84) | OPEN | Partial | Supported conditional rules and reviewed proof/payment-versus-absence workflow remain absent |
| [#85](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/85) | OPEN | Blocked | A1-A5 annual cycles/methods, earned units, holidays/fractions/spanning allocation, immutable charges, safe cutover and competing approved allocations are not implemented. |
| [#86](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/86) | OPEN | Blocked | A1-A5 actual-work provenance, sick cycles/first-six-month deduction, episodes/employer proof requests/exceptions, independent absence/evidence/pay statuses and historical charge snapshots are missing. |
| [#87](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/87) | OPEN | Blocked | A1-A4 service/schedule eligibility, qualifying versus enhanced events, distinct proof/payment/absence, part-day/cycle charges and shared historical results absent. |
| [#88](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/88) | OPEN | Blocked | A1-A5 reviewed allocation/recovery/notice/transitions, minimised external-parent declarations, historical cases/charges and time-off/pay/UIF separation are missing. |
| [#89](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/89) | OPEN | Partial | Production invitation delivery depends on #79 |
| [#90](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/90) | OPEN | Partial | A1 cycle/paid-unpaid/reviewed units cannot be delivered until #85/#22 |
| [#91](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/91) | OPEN | Complete | None found against A1-A4 current-data scope |
| [#92](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/92) | OPEN | Partial | A1 preserved charged units remain unavailable until #22/#85 |
| [#93](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/93) | OPEN | Complete | None against A1-A4 in-app criteria |
| [#94](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/94) | OPEN | Complete | None for implemented optional categories |
| [#95](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/95) | OPEN | Complete | None against A1-A4; owner/HR visibility is not medical-document access |
| [#96](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/96) | OPEN | Partial | A2 preserved charged-unit columns absent pending #22 |
| [#97](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/97) | OPEN | Complete | None against A1-A4 current-feature demo |
| [#98](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/98) | OPEN | Partial | Qualified decisions, calculation/history/category completion, approved production transport, privacy/retention and target-host operational acceptance remain |

## #17 — GET balance + PATCH cancel endpoints

**GitHub: OPEN. Classification: Partial.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/17)

### Fetched acceptance criteria

- **A1:** Given a non-owner attempts to cancel another employee's request, When called, Then the API returns 403.

- **Criterion disposition:** A1 implemented; description-level route mismatch requires explicit reconciliation.
- **Implemented:** Owner-derived balances and Pending-only POST cancellation; another owner receives 403, and cancellation/audit commit atomically.
- **Missing functionality / contract:** No missing cancellation behaviour found. The description still requests employee-ID GET and PATCH routes; actual owner-scoped GET /api/v1/leave-requests/balances and POST /api/v1/leave-requests/{id}/cancel deliberately differ. This is an unresolved tracker contract, not a request to add unsafe aliases.
- **Verification only:** Retained cancellation/audit and owner-scope checks exist; no new execution here.
- **Dependency / recommendation:** Owner accepts the safer transport wording, then closure is supportable.
- **Inspected source:** [CancelLeaveRequestCommandHandler.cs](../../src/HRFlow.Application/Features/LeaveRequests/Commands/CancelLeaveRequest/CancelLeaveRequestCommandHandler.cs).
- **Dated verification:** [leave-request-audit-timeline](../verification/leave-request-audit-timeline.md), [jwt-current-permissions](../verification/jwt-current-permissions.md). **Merged PR evidence:** [#59](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/59), [#60](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/60), [#64](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/64), [#99](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/99).

## #21 — Manager approval queue and optional decision notes

**GitHub: OPEN. Classification: Complete.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/21)

### Fetched acceptance criteria

- **A1:** Accessible approve/reject confirmation dialogs identify the employee, type and inclusive calendar dates, offer an optional labelled note, and retain input after recoverable errors.
- **A2:** Current active Manager membership, current same-department direct-report scope and Pending status are rechecked inside the existing SQLite writer reservation. No self-decisions or HR-only override; combined capabilities remain additive.
- **A3:** Approval retains current-policy balance and same-type overlap validation.
- **A4:** Status, audit and note commit together. Failed, repeated, forbidden or rolled-back attempts add no decision audit.
- **A5:** Owner personal history and authorized owner/eligible Manager/HR timelines show notes as plain text. Existing HR-only sensitive-reason/correlation filtering remains intact; no audit-edit endpoint.
- **A6:** The queue updates after a server-confirmed successful response, without full-page reload. No optimistic approval/rejection is displayed.
- **A7:** Duplicate submissions are blocked; stale decisions require explicit refresh; late callbacks cannot affect a replacement account/session, including same-account relogin.
- **A8:** Logs contain identifiers/action/correlation, never note contents. Document limits, visibility, migration and legacy behavior.

- **Criterion disposition:** A1-A8 implemented; audit Reason remains separate from DecisionNote.
- **Implemented:** Existing queue/dialogs, optional 500-trimmed-character notes, immutable decision audit, atomic status/outbox, current Manager/reporting guards, plain-text scoped history and guarded server-confirmed UI.
- **Missing functionality / contract:** None found against A1-A8. Mandatory rejection reasons are not part of this contract.
- **Verification only:** Dated real browser/rows/rollback evidence and retained decision/audit gates exist. Comprehensive assistive-technology/device review is a release check, not missing note functionality.
- **Dependency / recommendation:** Recommend closure after evidence review; replace the obsolete test deferral. No HR decision authority.
- **Inspected source:** [AuditEntry.cs](../../src/HRFlow.Domain/Entities/AuditEntry.cs), [ApproveLeaveRequestCommandHandler.cs](../../src/HRFlow.Application/Features/LeaveRequests/Commands/ApproveLeaveRequest/ApproveLeaveRequestCommandHandler.cs).
- **Dated verification:** [manager-decision-notes](../verification/manager-decision-notes.md), [critical-workflow-regressions](../verification/critical-workflow-regressions.md). **Merged PR evidence:** [#102](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/102), [#117](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/117).

## #22 — LeaveType and LeavePolicy entities

**GitHub: OPEN. Classification: Partial.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/22)

### Fetched acceptance criteria

- **A1:** Given a LeavePolicy is created, When a balance is calculated, Then the calculation reads accrual rules from this entity, not a hardcoded value.

- **Criterion disposition:** A1 only partly met: policy data is read, but not an accrual rule.
- **Implemented:** Versioned shared LeavePolicy/LeaveType, configured DefaultBalance/overlap, central current calendar-day calculator and protected approval revalidation.
- **Missing functionality / contract:** A1 accrual rules are absent: DefaultBalance is a fixed configured quantity. No effective-dated rule engine, cycle allocation, holiday-source revisions, fractional units or immutable approval-charge ledger. Requirements snapshots are not charge snapshots.
- **Verification only:** Existing CRUD/calendar-day tests do not verify future accrual. Approved-example calculation tests cannot be written as approved expectations before review.
- **Dependency / recommendation:** #75 L01/L02/L05 and C02/C03/P01/P02 decisions; remaining #78 inputs and #85 category contract. Keep existing #22; no duplicate engine issue.
- **Inspected source:** [LeavePolicy.cs](../../src/HRFlow.Domain/Entities/LeavePolicy.cs), [LeaveBalanceCalculator.cs](../../src/HRFlow.Application/Features/LeaveRequests/LeaveBalanceCalculator.cs).
- **Dated verification:** [leave-policy-management](../verification/leave-policy-management.md), [south-african-rule-review](../verification/south-african-rule-review.md). **Merged PR evidence:** [#67](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/67), [#68](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/68), [#125](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/125).

## #36 — Harden dev admin seeding per CodeRabbit review

**GitHub: OPEN. Classification: Complete.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/36)

### Fetched acceptance criteria

- **A1:** Move the Development-only environment guard inside SeedDevelopmentAdministratorAsync itself (resolve IHostEnvironment, return early if not IsDevelopment()), so the method is self-guarded rather than relying on the caller in Program.cs to remember to wrap it.
- **A2:** Make the seeded password configurable via IConfiguration["Seeding:HrAdministratorPassword"], with the current literal retained as the fallback default. Update the README to note it's a default, overridable via configuration.
- **A3:** Do NOT build automatic legacy-database migration/repair logic for the EnsureCreated-to-MigrateAsync transition - that's disproportionate for a project with no real deployment. Instead, add a one-line README troubleshooting note: if a local .db predates this change and throws a migration error, delete the file and restart.

- **Criterion disposition:** A1-A2 implemented by the renamed guarded seeder. A3 no repair implemented; safer recovery guidance supersedes unrestricted deletion.
- **Implemented:** DevelopmentIdentitySeeder and DatabaseSeeder self-guard by environment; configurable Development credentials; existing identities preserve credentials/activation/roles. Production rejects seeding configuration. Offline first-admin is a distinct empty-store tool.
- **Missing functionality / contract:** None for safe seeding. Literal unrestricted database deletion is deliberately not endorsed; reconcile the old method name and unsafe remediation wording before closure.
- **Verification only:** Production seed-denial and local Production first-admin evidence exist; target-host operation is separate.
- **Dependency / recommendation:** Recommend closure after owner accepts backed-up disposable-store-only troubleshooting instead of broad delete/restart guidance.
- **Inspected source:** [DevelopmentIdentitySeeder.cs](../../src/HRFlow.Infrastructure/Seeding/DevelopmentIdentitySeeder.cs), [DatabaseSeeder.cs](../../src/HRFlow.Infrastructure/Seeding/DatabaseSeeder.cs).
- **Dated verification:** [safe-deployment-configuration](../verification/safe-deployment-configuration.md), [offline-first-administrator](../verification/offline-first-administrator.md). **Merged PR evidence:** [#37](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/37), [#69](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/69), [#107](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/107), [#122](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/122).

## #74 — Enforce JWT expiry and current permissions across protected HRFlow APIs

**GitHub: OPEN. Classification: Complete.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/74)

### Fetched acceptance criteria

- **A1:** Expired JWTs fail with documented clock skew; valid active accounts still work; rotating refresh remains bounded and single-use.
- **A2:** Removed Employee/Manager/HR permissions deny all relevant old-token routes, including monitoring, employee reads, selectors, submit/cancel and queues.
- **A3:** Authorization and data use the same read snapshot; write authorization occurs after writer reservation. Current eligible managers retain additive personal capability; HR-only never decides.
- **A4:** Expired access tokens may use the existing valid same-session refresh and at most one retry; issuer, audience, signature, expiry and a documented small clock skew remain enforced.
- **A5:** Invalid, expired or revoked/replayed refresh tokens terminate only their initiating session; inactive accounts remain denied.
- **A6:** Current permission loss returns an appropriate denial, not a refresh loop. Combined accounts retain any other current valid capability.
- **A7:** Old-session requests cannot refresh, clear or retry against a new session, including logout/login as the same account; delayed success cannot expose old data.
- **A8:** Explicitly non-replayable operations remain non-replayable. Logs include identifiers/correlation, never credentials or tokens.

- **Criterion disposition:** A1-A8 delivered; current permissions and initiating-session retry boundaries remain authoritative.
- **Implemented:** Lifetime/issuer/audience/signature validation with explicit 30-second skew, current activated/active Identity capabilities and credential checks at API and protected read/write boundaries; bounded same-session refresh and non-replayable exceptions.
- **Missing functionality / contract:** None found against the functional contract; the issue problem statement ValidateLifetime=false is obsolete.
- **Verification only:** Dated real expiry/refresh and endpoint inventory evidence; retained role/session/credential regressions. Exhaustive not-before/missing-expiry/skew permutations are not all retained or rerun now.
- **Dependency / recommendation:** Recommend closure after security evidence review. Keep unretained boundary cases under #76, not as a fabricated auth feature gap.
- **Inspected source:** [Program.cs](../../src/HRFlow.Api/Program.cs), [CurrentAccountAuthorization.cs](../../src/HRFlow.Application/Services/CurrentAccountAuthorization.cs), [useAuth.tsx](../../src/HRFlow.Client/src/features/auth/hooks/useAuth.tsx).
- **Dated verification:** [jwt-current-permissions](../verification/jwt-current-permissions.md), [password-change-session-revocation](../verification/password-change-session-revocation.md), [restored-activation-safety](../verification/restored-activation-safety.md). **Merged PR evidence:** [#99](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/99), [#104](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/104), [#133](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/133).

## #75 — Approve the South African leave specification and calculation boundary cases

**GitHub: OPEN. Classification: Blocked.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/75)

### Fetched acceptance criteria

- **A1:** Resolve adoption-age retained wording, sole-employed/shared allocation, recovery/notice/deadline rules and transitions; do not use court media summary in place of the order.
- **A2:** Approve annual/sick/family cycle anchors, fractional units, leap boundaries, incomplete worked-day records and contractual variations.
- **A3:** Define certificate proof/payment separately from recording absence; parental eligibility is never solely Gender.
- **A4:** Approve minimum-data access/retention and company enhancements; record unresolved decisions as blocked, not assumed compliant.

- **Criterion disposition:** A1-A4 not approved; research preparation is not acceptance.
- **Implemented:** Updated canonical source register, 16 decision entries, 16 worked examples and a proposed #22 implementation contract, merged in PR #125.
- **Missing functionality / contract:** A1-A4 qualified decisions/sign-off and missing-authority reconciliation remain absent. Every reviewer is Unassigned; no decision is Approved, all examples remain Pending review. Van Wyk/legislative/UIF conclusions remain unresolved where recorded.
- **Verification only:** This review re-read records, not legislation. Failed authoritative retrievals and secondary reliance in the source pack remain disclosed; no fresh legal-source verification claimed.
- **Dependency / recommendation:** Qualified labour-law reviewer plus company policy and privacy owners. No statutory calculation authorisation from this reconciliation.
- **Inspected source:** [04-south-african-leave-requirements.md](../../docs/planning/04-south-african-leave-requirements.md), [11-south-african-rule-decisions.md](../../docs/planning/11-south-african-rule-decisions.md), [12-south-african-rule-examples.md](../../docs/planning/12-south-african-rule-examples.md).
- **Dated verification:** [south-african-rule-review](../verification/south-african-rule-review.md). **Merged PR evidence:** [#100](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/100), [#125](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/125).

## #76 — Introduce focused regression gates with an explicit test-convention decision

**GitHub: OPEN. Classification: Partial.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/76)

### Fetched acceptance criteria

- **A1:** Cover live role/lifecycle access, no self/HR decisions, policy balance/overlap, version conflicts, status/audit rollback and cross-process SQLite serialization.
- **A2:** Cover JWT expiry/refresh, account/same-account session epochs, stale forms and no mutation replay.
- **A3:** Retain migration preservation/legacy fixtures and add reviewed calculation examples as features land.
- **A4:** Document actual running gates versus manual accessibility/load/legal review; do not call an unexecuted checklist coverage.

- **Criterion disposition:** A1 and A4 delivered; A2 now has a focused locally prepared real-bearer/refresh extension alongside the retained controlled transport/session cases; exact clock equality and broader permutations remain limited. A3 legacy migration retained, reviewed future calculations dependent on #75.
- **Implemented:** Authorised maintained xUnit/Playwright foundation, real migrated SQLite, separate-process barriers, persisted-row/audit checks, migration fixtures, deliberate-defect checks, private diagnostics and hosted gates. #133 adds actual encrypted recovery/HTTP/SMTP coordination and failure cleanup.
- **Missing functionality / contract:** Continue retained cases as actual categories land, not unapproved statutory examples. The [real-token expiry/refresh report](../verification/real-token-expiry-refresh.md) distinguishes signed synthetic bearer probes, actual server rejection/refresh, explicit session barriers and unexecuted clock/platform cases. This local extension is not merged/hosted evidence or completion of #76; no second framework needed.
- **Verification only:** Final #133 hosted Ubuntu: 115 backend / 51 Chromium passed. Local earlier 111/112 complete failure and existing startup timeout remain unexplained; cleanup corrections are not proven root causes. Broader AT/load/target-host/scanner permutations remain verification debt.
- **Dependency / recommendation:** Test convention already authorised; remove deferral language. #75 gates future calculation fixtures. Keep open for focused missing retained cases, not indefinite platform permutations.
- **Inspected source:** [WorkflowTests.cs](../../tests/HRFlow.RegressionTests/WorkflowTests.cs), [ActivationRecoveryTests.cs](../../tests/HRFlow.RegressionTests/ActivationRecoveryTests.cs), [workflows.spec.ts](../../tests/browser/workflows.spec.ts).
- **Dated verification:** [critical-workflow-regressions](../verification/critical-workflow-regressions.md), [restored-activation-safety](../verification/restored-activation-safety.md), [directory-browser-regression](../verification/directory-browser-regression.md). **Merged PR evidence:** [#117](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/117), [#119](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/119), [#121](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/121), [#126](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/126), [#130](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/130), [#133](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/133).

## #77 — Add a responsive role-aware application shell around existing workflows

**GitHub: OPEN. Classification: Complete.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/77)

### Fetched acceptance criteria

- **A1:** Employee/Manager personal links and eligible Manager/HR sections are clear; HR never receives decision actions.
- **A2:** Desktop/mobile has no page-level overflow; keyboard/focus/contrast/reduced-motion and menu announcements work.
- **A3:** Loading/empty/error/retry and duplicate-action styling is consistent without replacing existing safeguards.
- **A4:** Logout/account switching removes protected content; direct URL denials remain authoritative.

- **Criterion disposition:** A1-A4 implemented; code splitting is not access control.
- **Implemented:** Stable protected shell, additive grouped navigation, responsive drawer/focus/Escape/skip link, consistent states, real Account/notification actions. Deferred routes/workspaces preserve providers and server guards.
- **Missing functionality / contract:** None found against the shell contract. Directory paging, client loading and calendar keyboard follow-ups are shipped, not reasons to rebuild the shell.
- **Verification only:** Dated desktop/mobile/keyboard/session evidence plus retained browser gates; comprehensive screen-reader/contrast/physical-device assessment remains separate.
- **Dependency / recommendation:** Recommend closure for the shell; no legal or provider dependency for this presentation slice.
- **Inspected source:** [ApplicationShell.tsx](../../src/HRFlow.Client/src/components/layout/ApplicationShell.tsx), [App.tsx](../../src/HRFlow.Client/src/App.tsx).
- **Dated verification:** [responsive-application-shell](../verification/responsive-application-shell.md), [client-route-loading](../verification/client-route-loading.md), [team-calendar-keyboard](../verification/team-calendar-keyboard.md). **Merged PR evidence:** [#101](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/101), [#123](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/123), [#124](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/124), [#127](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/127), [#132](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/132).

## #78 — Add versioned employment dates, employee numbers and work schedules

**GitHub: CLOSED (closed 2026-10-09T21:18:49Z). Classification: Partial.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/78)

### Fetched acceptance criteria

- **A1:** HR can configure required inputs with units/effective dates; employee numbers are normalized/unique under concurrent writes.
- **A2:** Legacy null/unknown dates and schedules remain visible as incomplete; account creation is never treated as hire date.
- **A3:** Department/role/report safeguards, Preserve/Assign/Clear and stale edit conflicts still work atomically with Identity.
- **A4:** Holiday sources and schedule revisions are versioned; a future schedule change does not rewrite approved charged days.

- **Criterion disposition:** A1-A3 and schedule-history half of A4 delivered; holiday-source half of A4 outstanding.
- **Implemented:** Normalized unique employee numbers including inactive records, confirmed nullable legacy start dates, append-only fixed weekly schedule revisions, HR editing and read-only own details. Future dates do not postpone activated-account access.
- **Missing functionality / contract:** A4 versioned holiday sources are not implemented. Remaining cycle/calculation inputs are absent. Closed state does not erase this criterion; schedules must not be described as a holiday engine.
- **Verification only:** Dated duplicates/stale writes, migration/browser/recovery evidence. These do not verify holiday calculations.
- **Dependency / recommendation:** Issue is already CLOSED; propose owner-approved transfer of the holiday-source criterion to #22/#85 or explicit scope correction. Do not reopen/edit it here. #75 decisions before holiday application.
- **Inspected source:** [Employee.cs](../../src/HRFlow.Domain/Entities/Employee.cs), [WeeklyScheduleRevision.cs](../../src/HRFlow.Domain/Entities/WeeklyScheduleRevision.cs), [0017-confirmed-employment-data.md](../../docs/adr/0017-confirmed-employment-data.md).
- **Dated verification:** [employment-data-foundation](../verification/employment-data-foundation.md). **Merged PR evidence:** [#111](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/111).

## #79 — Provision HR-created accounts through secure one-time activation

**GitHub: OPEN. Classification: Partial.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/79)

### Fetched acceptance criteria

- **A1:** Tokens are purpose/account-bound, expiring and single-use; repeated/concurrent redemption yields one safe outcome.
- **A2:** Account creation and invitation resend require current active HR. The intended employee anonymously redeems a valid account-bound activation token without HR membership or an existing login; activation never grants or restores roles. Activation cannot overwrite, reactivate or restore roles to an existing account.
- **A3:** No credential in preview/report/log; activation secrets are delivered only via the approved channel, not downloadable CSV.
- **A4:** Document delivery failure/retry and account state; initial credential flow forces password establishment before normal access.

- **Criterion disposition:** A1-A2 and failure/state portion of A4 delivered; A3 approved production channel incomplete. Existing local passwords preserved on normal migration, while explicit recovery invalidates authority deliberately.
- **Implemented:** Passwordless HR create/CSV, hash-only expiring single-use activation, explicit versioned resend, PickupReady/Accepted/DeliveryFailed/DeliveryUnknown/PendingDelivery feedback. Generation/recipient/eligibility guards surround transport outside writer protection. #133 verifies restored authority clearing, no automatic dispatch/recreation, existing-HR recovery and fresh resend only.
- **Missing functionality / contract:** No approved production SMTP/API adapter or authentication/provider integration. A3 approved real delivery channel and production recipient delivery are still unavailable; loopback MIME fixture is test-only, not a provider.
- **Verification only:** Dated local positive SMTP/redemption, races/failures and retained restored-store HTTP/transport tests; hosted 115/51 includes retained gates, not external provider/deliverability or all scanner/recovery environments.
- **Dependency / recommendation:** ADR 0029 D1-D5 Pending/Unassigned; D6 release approval also unsupplied. Sender/domain, credentials/custody/privacy/operator and uncertain-resend policy need owner approval. Production remains disabled.
- **Inspected source:** [AccountActivationService.cs](../../src/HRFlow.Infrastructure/Services/Auth/AccountActivationService.cs), [DevelopmentActivationDelivery.cs](../../src/HRFlow.Infrastructure/Services/Auth/DevelopmentActivationDelivery.cs), [DeploymentConfiguration.cs](../../src/HRFlow.Api/Services/DeploymentConfiguration.cs).
- **Dated verification:** [secure-account-activation](../verification/secure-account-activation.md), [activation-delivery-coordination](../verification/activation-delivery-coordination.md), [restored-activation-safety](../verification/restored-activation-safety.md). **Merged PR evidence:** [#103](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/103), [#128](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/128), [#130](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/130), [#133](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/133).

## #80 — Add Identity password change with verified session revocation

**GitHub: OPEN. Classification: Complete.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/80)

### Fetched acceptance criteria

- **A1:** Wrong current password, invalid new password and revoked/inactive actor fail without partial changes.
- **A2:** After commit, old JWTs and all prior refresh tokens cannot access/refresh; changing Identity SecurityStamp alone is not assumed to revoke bearer tokens.
- **A3:** Concurrent refresh/change follows a valid serial order; 401 does not replay writes.
- **A4:** No password logs; accessible confirmation/loading/error, duplicate prevention and retained safe form state.

- **Criterion disposition:** A1-A4 implemented; Identity SecurityStamp alone is not the bearer revocation mechanism.
- **Implemented:** Own-account Identity ChangePassword validation; password/version rotation and all refresh revocations atomically; credential generation checked at API and protected boundaries. Non-replayable form signs caller out only after confirmed success.
- **Missing functionality / contract:** None found against A1-A4. Forgotten-password/email recovery and HR editing others passwords are excluded.
- **Verification only:** Dated separate-process ordering, failure rollback, real local API/browser and retained credential/session checks. No claim already-authorized snapshots are retroactively undone.
- **Dependency / recommendation:** Recommend closure; rollout fresh-login and migration/rollback consequences remain documented.
- **Inspected source:** [AuthService.cs](../../src/HRFlow.Infrastructure/Services/Auth/AuthService.cs), [RequestCredentialValidator.cs](../../src/HRFlow.Infrastructure/Services/RequestCredentialValidator.cs).
- **Dated verification:** [password-change-session-revocation](../verification/password-change-session-revocation.md). **Merged PR evidence:** [#104](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/104).

## #81 — Define and verify a safe single-organisation deployment configuration

**GitHub: OPEN. Classification: Partial.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/81)

### Fetched acceptance criteria

- **A1:** Production starts without dev seed credentials/default keys and fails safely on missing security settings.
- **A2:** Persistent database/files survive restart; storage is private and outside public web roots.
- **A3:** Structured logs/correlation and safe errors are useful without credentials, document contents or medical context.
- **A4:** Document capacity/timeout/load limits and costs; no unsupported production/compliance claim or deployment performed by this issue's planning step.

- **Criterion disposition:** A1-A3 locally implemented/evidenced; A4 documented limits but realistic load/cost/host acceptance and staged verification remain.
- **Implemented:** Fail-safe signing/origin/proxy/private-storage configuration, health/readiness, offline schema tooling, single-host SQLite runbook. One-time offline first-admin shipped in #122: strict empty Identity/profile stores, lease/writer protection, atomic HR-only account/department and private password-file input. It is not recovery.
- **Missing functionality / contract:** Owner-approved target topology/hosting and operational ownership/costs still unsettled. No real staging deployment or approved production invitation delivery; target-host configuration/monitoring not completed.
- **Verification only:** Local Production TLS login/refresh/password change and first-admin login are recorded. Actual trusted-proxy chain, persistent-volume/restart/restore/load and real staging browser/operator exercises remain unexecuted; one green Ubuntu fixture job is not deployment evidence.
- **Dependency / recommendation:** Deployment separately authorised; owner host/budget/operator decisions, #79 provider approval and privacy/custody. Do not recommend implementing first-admin again.
- **Inspected source:** [DeploymentConfiguration.cs](../../src/HRFlow.Api/Services/DeploymentConfiguration.cs), [FirstAdministratorService.cs](../../src/HRFlow.Infrastructure/Services/Operations/FirstAdministratorService.cs), [Program.cs](../../tools/HRFlow.Operations/Program.cs).
- **Dated verification:** [safe-deployment-configuration](../verification/safe-deployment-configuration.md), [offline-first-administrator](../verification/offline-first-administrator.md). **Merged PR evidence:** [#107](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/107), [#122](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/122).

## #82 — Implement and rehearse consistent database and private-file backup/restore

**GitHub: OPEN. Classification: Complete.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/82)

### Fetched acceptance criteria

- **A1:** Restored profile/Identity/roles/versions/lifecycle/request/audit rows agree; inactive access stays disabled.
- **A2:** Files and metadata are coherent, quarantined objects remain inaccessible and missing blobs are reported safely.
- **A3:** Secrets/keys required for restore are secured separately; migration rollback losing lifecycle metadata is not routine.
- **A4:** Drill records measured recovery time and unrecovered window rather than promising zero loss.

- **Criterion disposition:** A1 rows/security and A3 custody/invalidation supported by #133; A2 actual clean/quarantined evidence and images by #109/#119 plus retained checks; A4 measured cutoff/recovery recorded in #108.
- **Implemented:** Operator-only SQLite-aware encrypted package, stopped-writer inventory/exclusive lease, manifests/checksums/schema/integrity, isolated no-overwrite restore, attachment/image validation, security invalidation and reviewed existing-identity recover-password. #133 joins real CLI, normal API, persisted/HTTP checks and transport observations.
- **Missing functionality / contract:** None found against A1-A4 local tooling/drill criteria. Scheduled backup/retention automation was not required by these criteria; operator custody/retention decisions remain release obligations.
- **Verification only:** Historical 1.470s backup/0.922s restore and 2.580s unrecovered post-cutoff cancellation are local measurements, not guarantees. Target-host volumes/TLS/proxy/large stores/operator practice and broad scanner/fault matrices remain unexecuted.
- **Dependency / recommendation:** Recommend closure of implemented operator criteria after operator evidence review, carrying target-host acceptance in #81 and approved retention/holds in #83/#98. If owner intends broader scope, retain that explicit remainder instead; no automatic closure.
- **Inspected source:** [BackupRestoreService.cs](../../src/HRFlow.Infrastructure/Services/Operations/BackupRestoreService.cs), [ActivationRecoveryTests.cs](../../tests/HRFlow.RegressionTests/ActivationRecoveryTests.cs), [ProfileImageTests.cs](../../tests/HRFlow.RegressionTests/ProfileImageTests.cs).
- **Dated verification:** [verified-backup-restore](../verification/verified-backup-restore.md), [private-supporting-documents](../verification/private-supporting-documents.md), [private-profile-images](../verification/private-profile-images.md), [restored-activation-safety](../verification/restored-activation-safety.md). **Merged PR evidence:** [#108](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/108), [#109](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/109), [#119](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/119), [#133](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/133).

## #83 — Add private supporting documents with quarantine and scoped access

**GitHub: OPEN. Classification: Partial.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/83)

### Fetched acceptance criteria

- **A1:** Manager sees medical evidence status only; owner and explicitly authorised current HR get medical content. Ordinary documents use documented owner/current-scope/HR permissions.
- **A2:** Cross-user upload binding/download, revoked/inactive access and quarantined content are denied without existence leakage.
- **A3:** Stage file before DB binding, commit metadata/status, compensate or schedule cleanup on failure; transactions do not pretend database/blob writes are atomic.
- **A4:** Retention/removal/legal hold and access auditing reuse existing conventions without competing transition audits; log actor/object/action/result, never content or diagnosis.
- **A5:** Cancelled/failed uploads cannot bind; retry uses scoped idempotency, scan timeout/failure remains quarantined, safe download headers and limited previews.

- **Criterion disposition:** A1-A3/A5 shipped; A4 access audit/draft removal shipped, approved bound retention/hold workflow incomplete.
- **Implemented:** Private PDF/JPEG/PNG, real fail-closed ClamAV adapter, quarantine, byte/checksum publication fencing, owned clean submission binding, medical status-only Managers, safe downloads, access audit, draft removal and durable reconciliation.
- **Missing functionality / contract:** A4 approved bound-evidence retention/legal-hold/expiry administration remains absent; bound removal rejects without policy. Also, binding is only during new submission: no owner supplemental-document workflow on an existing Pending request. This latter user-value slice is proposed, not already an accepted new requirement.
- **Verification only:** Dated real positive ClamAV/document recovery and separate process interleavings; #119 image checks are separate categories. No fresh scanner definitions, load, full AT or target-host recovery run in this review.
- **Dependency / recommendation:** No bound purge without approved P01/P02 privacy/retention decisions. Supplemental Pending attachment can reuse existing privacy rules without statutory/payment decisions, after owner accepts the bounded slice.
- **Inspected source:** [SupportingDocumentService.cs](../../src/HRFlow.Application/Services/SupportingDocumentService.cs), [DocumentsController.cs](../../src/HRFlow.Api/Controllers/DocumentsController.cs), [DocumentCleanupWorker.cs](../../src/HRFlow.Infrastructure/Services/DocumentCleanupWorker.cs).
- **Dated verification:** [private-supporting-documents](../verification/private-supporting-documents.md). **Merged PR evidence:** [#109](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/109).

## #84 — Configure request descriptions and evidence requirements with submission snapshots

**GitHub: OPEN. Classification: Partial.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/84)

### Fetched acceptance criteria

- **A1:** Type selection shows correct fields and explanations; server rejects missing required descriptions/clean evidence independently of client state.
- **A2:** Snapshot rule version/requirements at submission; later config changes affect future submissions but do not fabricate requirements/evidence on legacy records.
- **A3:** Approval rechecks reviewed applicable entitlement and only the explicitly defined evidence/payment workflow; no silent retroactive upload demand.
- **A4:** Preserve drafts on configuration/version conflicts, require explicit refresh, prevent duplicates and use session-scoped invalidation.
- **A5:** Descriptions are bounded plain text, never requested diagnoses; manager medical access remains status-only.

- **Criterion disposition:** A1-A2/A4-A5 delivered for company settings; A3 reviewed entitlement/conditional evidence/payment remains dependent.
- **Implemented:** Leave types are authoritative for description/evidence modes, class/instructions and versions; immutable submission snapshots, null legacy Unknown, 1,000 trimmed plain-text descriptions, ordinary Required evidence, Medical non-downgrade and stale-draft recovery.
- **Missing functionality / contract:** Supported conditional rules and reviewed proof/payment-versus-absence workflow remain absent. Required Medical submission is rejected server-side intentionally; it is not a hidden configuration waiting to be enabled.
- **Verification only:** Recorded HR configuration/owner submission/rows, stale forms, atomic audit/binding and recovery evidence; not all evidence/platform cases rerun now.
- **Dependency / recommendation:** #75/#22/#86 before medical conditional/payment automation. Preserve snapshots and do not invent legacy requirements or later demands.
- **Inspected source:** [RequestRequirements.cs](../../src/HRFlow.Domain/Entities/RequestRequirements.cs), [SubmitLeaveRequestCommandHandler.cs](../../src/HRFlow.Application/Features/LeaveRequests/Commands/SubmitLeaveRequest/SubmitLeaveRequestCommandHandler.cs).
- **Dated verification:** [request-evidence-requirements](../verification/request-evidence-requirements.md). **Merged PR evidence:** [#110](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/110).

## #85 — Deliver reviewed annual leave cycles and schedule-based accrual end to end

**GitHub: OPEN. Classification: Blocked.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/85)

### Fetched acceptance criteria

- **A1:** Employment-anniversary cycles, 21 consecutive-day baseline/schedule equivalents and agreed 1/17 day/hour methods use explicit units and real qualifying inputs.
- **A2:** Holiday and schedule boundaries, fractions/rounding and spanning-cycle allocation match approved examples; months never become constant day counts.
- **A3:** Approved charges and rule/schedule references are stored as historical facts; future edits cannot silently recalculate them.
- **A4:** Legacy incomplete inputs display unavailable/legacy basis with an explicit HR remediation path; no fabricated dates or audits.
- **A5:** Pending reserves nothing unless separately re-scoped; competing approvals cannot exceed the reviewed allocation.

- **Criterion disposition:** A1-A5 outstanding, not proven by a seeded Annual label.
- **Implemented:** Employment facts, schedule history and current calendar calculator exist as prerequisites only; no annual statutory category implementation.
- **Missing functionality / contract:** A1-A5 annual cycles/methods, earned units, holidays/fractions/spanning allocation, immutable charges, safe cutover and competing approved allocations are not implemented.
- **Verification only:** No approved category examples or statutory calculation tests. Current calendar tests are not annual-rule evidence.
- **Dependency / recommendation:** #75 signed applicable annual decisions, #22 engine/charge contract and #78 holiday-source remainder. Category implementation not started; Blocked records the active prerequisite.
- **Inspected source:** [LeaveBalanceCalculator.cs](../../src/HRFlow.Application/Features/LeaveRequests/LeaveBalanceCalculator.cs), [13-calculation-foundation-review.md](../../docs/planning/13-calculation-foundation-review.md).
- **Dated verification:** [south-african-rule-review](../verification/south-african-rule-review.md). **Merged PR evidence:** [#125](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/125).

## #86 — Deliver sick-leave cycles with conditional evidence and payment review

**GitHub: OPEN. Classification: Blocked.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/86)

### Fetched acceptance criteria

- **A1:** Six-week schedule entitlement and first-cycle deduction avoid double charge; distinguish actual worked inputs from scheduled estimates.
- **A2:** More-than-two consecutive days or more-than-two occasions/eight weeks trigger the reviewed employer-request workflow, including exceptions.
- **A3:** Absence can be recorded when proof/pay eligibility is unresolved; unavailable data is visible, not silently zero.
- **A4:** Medical access is owner/authorised HR only; safe notices and disputes keep managers out of raw content.
- **A5:** Historical charges, reasons/audits and pending current-rule validation remain truthful and atomic.

- **Criterion disposition:** A1-A5 outstanding; optional evidence privacy is a prerequisite only.
- **Implemented:** Optional Medical uploads and requirement snapshots exist; neither is a sick-cycle or pay-review workflow.
- **Missing functionality / contract:** A1-A5 actual-work provenance, sick cycles/first-six-month deduction, episodes/employer proof requests/exceptions, independent absence/evidence/pay statuses and historical charge snapshots are missing.
- **Verification only:** No signed sick worked examples; document privacy tests do not establish eligibility/payment correctness.
- **Dependency / recommendation:** #75 L03/L04/P01/P02, #22/#78/#84. No certificate-for-every-sick-request or automatic payment denial. Category code not started.
- **Inspected source:** [LeaveBalanceCalculator.cs](../../src/HRFlow.Application/Features/LeaveRequests/LeaveBalanceCalculator.cs), [SupportingDocumentService.cs](../../src/HRFlow.Application/Services/SupportingDocumentService.cs).
- **Dated verification:** [request-evidence-requirements](../verification/request-evidence-requirements.md), [south-african-rule-review](../verification/south-african-rule-review.md). **Merged PR evidence:** [#109](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/109), [#110](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/110), [#125](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/125).

## #87 — Deliver event-based family responsibility and company compassionate leave

**GitHub: OPEN. Classification: Blocked.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/87)

### Fetched acceptance criteria

- **A1:** Longer-than-four-months and at-least-four-days/week boundaries, three statutory days and reviewed lapse/variation rules are explicit.
- **A2:** Qualifying versus enhanced events are labelled without unnecessarily collecting relatives' personal details.
- **A3:** Proof/payment and absence recording stay distinct; medical content is not revealed to managers.
- **A4:** Cycle-spanning/part-day charge and historical rule snapshots agree across submission/approval/balance/report.

- **Criterion disposition:** A1-A4 outstanding; company enhancements must be labelled and approved separately.
- **Implemented:** Generic types/policies/evidence exist; no event/relationship/service eligibility category.
- **Missing functionality / contract:** A1-A4 service/schedule eligibility, qualifying versus enhanced events, distinct proof/payment/absence, part-day/cycle charges and shared historical results absent.
- **Verification only:** No approved family-responsibility/compassionate examples; generic type CRUD is not category verification.
- **Dependency / recommendation:** #75 legal/company/privacy decisions, #22/#78/#84. Category implementation not started; do not invent relatives or event facts.
- **Inspected source:** [LeaveRequest.cs](../../src/HRFlow.Domain/Entities/LeaveRequest.cs), [11-south-african-rule-decisions.md](../../docs/planning/11-south-african-rule-decisions.md).
- **Dated verification:** [south-african-rule-review](../verification/south-african-rule-review.md). **Merged PR evidence:** [#125](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/125).

## #88 — Deliver reviewed parental, adoption and commissioning leave cases

**GitHub: OPEN. Classification: Blocked.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/88)

### Fetched acceptance criteria

- **A1:** Actual court order rather than media summary governs single/sole-employed versus shared allocation; do not hardcode calendar months as days.
- **A2:** Recovery/fitness and miscarriage/stillbirth safeguards, continuity and reviewed disagreement deadlines are enforced without Gender being the sole eligibility input.
- **A3:** Other parent's external employment/allocation is a minimised declaration with HR review, not claimed verified interoperable data.
- **A4:** Policy/rule changes preserve charged history and case evidence; current pending validation shows applicable rule and any unresolved review.
- **A5:** UI distinguishes time-off rights, employer pay and uncertain UIF benefits; no unsupported legal-compliance promise.

- **Criterion disposition:** A1-A5 outstanding; research is not implementation/sign-off.
- **Implemented:** Source/decision/example pack exists; no parental case/allocation model or category workflow.
- **Missing functionality / contract:** A1-A5 reviewed allocation/recovery/notice/transitions, minimised external-parent declarations, historical cases/charges and time-off/pay/UIF separation are missing.
- **Verification only:** No qualified approved outcomes. Retrieval gaps and Van Wyk/legislative/UIF reconciliation remain unresolved, not assumed law.
- **Dependency / recommendation:** #75 L07-L11 and privacy decisions, #22/#78/#84. No gender-only gates or fixed-day calendar months. Category implementation not started.
- **Inspected source:** [11-south-african-rule-decisions.md](../../docs/planning/11-south-african-rule-decisions.md), [12-south-african-rule-examples.md](../../docs/planning/12-south-african-rule-examples.md), [LeaveRequest.cs](../../src/HRFlow.Domain/Entities/LeaveRequest.cs).
- **Dated verification:** [south-african-rule-review](../verification/south-african-rule-review.md). **Merged PR evidence:** [#125](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/125).

## #89 — Add HR CSV onboarding with mapping, preview and safe row confirmation

**GitHub: OPEN. Classification: Partial.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/89)

### Fetched acceptance criteria

- **A1:** Use Identity email normalizer; detect duplicates within file and against profile/Identity NormalizedEmail/NormalizedUserName, including inactive/unlinked accounts and employee numbers.
- **A2:** Validate departments, existing active Manager/same-department/cycle rules, full roles and employment/schedules; optional Gender neither required nor parental gate.
- **A3:** Revalidate each row under writer reservation/current HR; per-row transaction commits all Identity/profile/roles/reporting or rolls back all. Other valid rows can succeed, no orphan accounts.
- **A4:** Confirmation has actor-scoped idempotency and stable row outcomes; repeated requests do not re-create successes. Contention/failure rows require explicit retry after refreshed validation; imports race safely.
- **A5:** Activation replaces passwords; no secrets in logs/previews/results. Formula-injection-safe downloads contain minimal authorised row errors; delete temporary upload/preview by documented retention.

- **Criterion disposition:** A1/A3/A4 implemented; A2 department/roles/schedules implemented with explicit Gender limitation; A5 local activation/results/preview cleanup implemented, production channel unavailable.
- **Implemented:** 1 MiB/100-row UTF-8 maintained CSV parser, mapping/preview/Ready-Duplicate-Invalid, Identity-normalized collisions, server-held confirmation, protected shared per-row creation, durable idempotent outcomes/cancellation and activation recovery. Named schedule values copied into immutable revisions. Expired preview personal fields are stripped by the cleanup worker, minimal outcomes retained.
- **Missing functionality / contract:** Production invitation delivery depends on #79. Nonblank optional Gender is explicitly rejected, not stored; this is the authorised implemented limitation, but original template scope should be reconciled explicitly before full closure, not silently ticked as supported Gender.
- **Verification only:** Dated browser/duplicate/race/late-failure/migration/recovery; #133 verifies restored Created rows cannot replay. No fresh external provider or realistic-volume import run.
- **Dependency / recommendation:** #79 provider decisions for real onboarding. Owner accepts blank-only Gender limitation or separately approves minimal values/purpose/visibility/retention; never use it as eligibility.
- **Inspected source:** [CsvOnboardingService.cs](../../src/HRFlow.Infrastructure/Services/Employees/CsvOnboardingService.cs), [OnboardingCleanupWorker.cs](../../src/HRFlow.Infrastructure/Services/Employees/OnboardingCleanupWorker.cs).
- **Dated verification:** [hr-csv-onboarding](../verification/hr-csv-onboarding.md), [activation-delivery-coordination](../verification/activation-delivery-coordination.md), [restored-activation-safety](../verification/restored-activation-safety.md). **Merged PR evidence:** [#112](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/112), [#130](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/130), [#133](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/133).

## #90 — Build an API-backed personal leave dashboard with explicit periods and units

**GitHub: OPEN. Classification: Partial.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/90)

### Fetched acceptance criteria

- **A1:** Counts/dates/balances are real API data; Paid/unpaid/cycle/legacy units are labelled accurately.
- **A2:** Pending and upcoming periods are documented, overlapping requests never imply unique absence metrics.
- **A3:** Loading/empty/error/retry/refresh and accessible desktop/mobile links work.
- **A4:** Account/session keys, cancellation and initiating-session invalidation prevent delayed leaks; entry refreshes.

- **Criterion disposition:** A2-A4 delivered; A1 current-data/legacy labels delivered, future calculation units absent.
- **Implemented:** Owner-derived Employee/Manager snapshot, central current balances, all-Pending count/preview, ongoing and inclusive today-through-90-day Approved list, deterministic history, SAST dates/UTC as-of, honest metric labels and session-isolated refresh.
- **Missing functionality / contract:** A1 cycle/paid-unpaid/reviewed units cannot be delivered until #85/#22. Current configured balance minus all approved calendar days is not annual accrued/statutory entitlement.
- **Verification only:** Dated role/date/balance/browser/session evidence plus retained workflows; broad all-theme/AT/device checks separate.
- **Dependency / recommendation:** #75/#22/#85 for calculation-dependent completion. No need to redesign the shipped dashboard.
- **Inspected source:** [GetPersonalDashboardQuery.cs](../../src/HRFlow.Application/Features/LeaveRequests/Queries/GetPersonalDashboard/GetPersonalDashboardQuery.cs).
- **Dated verification:** [personal-leave-dashboard](../verification/personal-leave-dashboard.md). **Merged PR evidence:** [#113](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/113).

## #91 — Build the Manager workspace and read-only team calendar from scoped data

**GitHub: OPEN. Classification: Complete.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/91)

### Fetched acceptance criteria

- **A1:** Own requests appear in personal section but never eligible queue; current live Manager scope is authoritative.
- **A2:** Inactive approved history is labelled and excluded from active coverage; count distinct active people per date when shown, unioning overlaps.
- **A3:** Counts have explicit date periods/units; month navigation, retry/refresh and keyboard/mobile work.
- **A4:** Reassignment/decisions refresh this session's views; delayed account responses cannot appear.

- **Criterion disposition:** A1-A4 delivered; #132 addresses the earlier many-Tab-stops source finding.
- **Implemented:** Personal plus scoped Manager attention/upcoming/calendar/list, no self/team decision leakage, deduplicated active people per date, inactive history labels and 62-day bounded ranges. #132 adds one visible date Tab stop, independent focus/selection, arrow/Home/End and responsive focus repair.
- **Missing functionality / contract:** None found against A1-A4 current-data scope. Calendar is read-only; no staffing-capacity/holiday entitlement claims or added approval controls.
- **Verification only:** Dated real/controlled browser checks include leap/year/range bounds, Overview/Team routes, keyboard/loading/session cases. Physical-device/screen-reader review remains unexecuted. Today uses browser clock converted to SAST; receipt time is not server snapshot time.
- **Dependency / recommendation:** Recommend closure for current-data Manager scope. Statistical load or full WCAG certification is not inferred.
- **Inspected source:** [ManagerWorkspace.tsx](../../src/HRFlow.Client/src/features/manager-workspace/ManagerWorkspace.tsx), [CalendarDateGrid.tsx](../../src/HRFlow.Client/src/features/manager-workspace/CalendarDateGrid.tsx).
- **Dated verification:** [manager-workspace-calendar](../verification/manager-workspace-calendar.md), [team-calendar-keyboard](../verification/team-calendar-keyboard.md). **Merged PR evidence:** [#114](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/114), [#132](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/132).

## #92 — Build the HR workspace with scoped monitoring trends and administrative attention

**GitHub: OPEN. Classification: Partial.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/92)

### Fetched acceptance criteria

- **A1:** Selected period/request counts/distinct people/calendar durations/charged units are separate; inactive contributions and current-department attribution are explicit.
- **A2:** Draft filters never relabel old results; refresh/loading/error/empty and table alternative work.
- **A3:** Combined HR users get capability; revoked/inactive HR and non-HR direct URLs/API are denied.
- **A4:** Administrative attention reflects real incomplete inputs or failed onboarding, not speculative notifications.

- **Criterion disposition:** A2-A4 and current-calendar/count portion of A1 delivered; charged-unit portion absent.
- **Implemented:** Current-HR deferred snapshot, all-date employment/activation/import attention, period reports/trends/table alternatives and correct filter/session composition. #131 replaces per-employee activation/full schedule materialization with Infrastructure aggregation; one captured as-of timestamp and API unchanged.
- **Missing functionality / contract:** A1 preserved charged units remain unavailable until #22/#85. Report/trend/import materialization remains; efficiency improvement is shipped, not proof all reporting is bounded or parent complete.
- **Verification only:** Independent scalar reconciliation, DELETE/WAL snapshot barriers and fixed query-count tests; recorded local measurements and HR browser cases. No new benchmark/load/AT execution here.
- **Dependency / recommendation:** #75/#22 for charged-unit completion; realistic cardinality/host acceptance remain verification/operations work, not dashboard decoration.
- **Inspected source:** [GetHrWorkspaceQuery.cs](../../src/HRFlow.Application/Features/LeaveRequests/Queries/GetHrWorkspace/GetHrWorkspaceQuery.cs), [HrWorkforceAttentionReader.cs](../../src/HRFlow.Infrastructure/Services/Employees/HrWorkforceAttentionReader.cs).
- **Dated verification:** [hr-workspace](../verification/hr-workspace.md), [hr-workspace-query-efficiency](../verification/hr-workspace-query-efficiency.md). **Merged PR evidence:** [#115](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/115), [#131](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/131).

## #93 — Deliver durable scoped in-app notifications from committed leave events

**GitHub: OPEN. Classification: Complete.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/93)

### Fetched acceptance criteria

- **A1:** Only committed events deliver; late rollback delivers none and retries never duplicate a notification.
- **A2:** Current active recipients can read only their own notifications; deep links recheck current scope and can safely be unavailable after reassignment.
- **A3:** Medical content, diagnoses, raw deactivation reasons and note text do not appear in notification payloads/logs.
- **A4:** Read/unread behaviour works across retries and sessions with bounded polling; general preferences remain #94; no automatic replay of leave decisions.

- **Criterion disposition:** A1-A4 delivered; no invented legacy notifications or competing leave-transition audit.
- **Implemented:** Minimal atomic outbox, idempotent event/recipient uniqueness and worker, private inbox/count/read, reassignment-safe redaction/deep links and bounded session-scoped polling; optional preferences shipped separately.
- **Missing functionality / contract:** None against A1-A4 in-app criteria. Automatic notification/outbox retention cleanup is not implemented; current indefinite storage/growth and privacy policy remain explicit operational work.
- **Verification only:** Dated multi-process/rollback/inbox browser evidence and #133 committed-event restart check. No external email/SMS channel or broad load result.
- **Dependency / recommendation:** Recommend closure for messaging criteria, preserving retention/custody ownership under #98/privacy operations. Do not imply preference work or cleanup is still secretly implemented here.
- **Inspected source:** [LeaveNotificationWorker.cs](../../src/HRFlow.Infrastructure/Services/LeaveNotificationWorker.cs), [LeaveNotificationService.cs](../../src/HRFlow.Infrastructure/Services/LeaveNotificationService.cs).
- **Dated verification:** [in-app-leave-notifications](../verification/in-app-leave-notifications.md), [profile-and-preferences](../verification/profile-and-preferences.md), [restored-activation-safety](../verification/restored-activation-safety.md). **Merged PR evidence:** [#105](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/105), [#106](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/106), [#133](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/133).

## #94 — Add permitted self-service profile fields and working preferences

**GitHub: OPEN. Classification: Complete.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/94)

### Fetched acceptance criteria

- **A1:** Only allowlisted fields are accepted server-side; unknown privilege fields cannot change employee management state.
- **A2:** Stale edits conflict, inactive/revoked accounts fail and values are private/minimised.
- **A3:** Theme actually applies with contrast/reduced-motion; notification toggles affect delivered categories but cannot hide mandatory security notices.
- **A4:** Profile,password and preferences use session-scoped callbacks; no old-account settings/success after logout.

- **Criterion disposition:** A1-A4 implemented; A3 must say only implemented optional in-app categories, and no email/SMS/mandatory channel claim.
- **Implemented:** Own-account field allowlist/private phone, canonical HR fields read-only, independent profile/preferences versions, System/Light/Dark and transactional delivery-time category suppression without historical replay; guarded session callbacks.
- **Missing functionality / contract:** None for implemented optional categories. No mandatory security-notification channel exists; clarify A3 accordingly, rather than inventing a new channel to satisfy ambiguous wording.
- **Verification only:** Dated stale-write/worker/browser/theme/account replacement evidence. Full AT/contrast/real-employee privacy acceptance is not newly verified.
- **Dependency / recommendation:** Recommend closure after wording clarification; company privacy/retention approval remains a pilot gate, not claimed from the implementation.
- **Inspected source:** [OwnAccountService.cs](../../src/HRFlow.Infrastructure/Services/OwnAccountService.cs), [AccountSettings.cs](../../src/HRFlow.Domain/Entities/AccountSettings.cs), [AccountPages.tsx](../../src/HRFlow.Client/src/features/account/AccountPages.tsx).
- **Dated verification:** [profile-and-preferences](../verification/profile-and-preferences.md). **Merged PR evidence:** [#106](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/106).

## #95 — Add safe private profile image upload and removal

**GitHub: OPEN. Classification: Complete.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/95)

### Fetched acceptance criteria

- **A1:** Actual content/dimensions/decompression limits are checked; malformed/polyglot/oversize images cannot become public content.
- **A2:** Cross-user bind/remove and inactive access fail; old blobs are cleaned after successful replacement with safe failure compensation.
- **A3:** Images carry no EXIF/location data; medical retention/access is never reused.
- **A4:** Session change cancels/hides upload completion and duplicate actions are blocked.

- **Criterion disposition:** A1-A4 delivered, with immutable absence/concurrency markers retained.
- **Implemented:** Private versioned own-image mutation/current-HR view, bounded JPEG/PNG decoded limits, orientation/crop/re-encoding/metadata stripping, scan exact processed output, publication/remove fences and durable cleanup. #124 omits removed zero-size image hints while preserving own-account absence versions.
- **Missing functionality / contract:** None against A1-A4; owner/HR visibility is not medical-document access. Bound evidence is never affected by image cleanup.
- **Verification only:** Dated real ClamAV/EXIF/browser/recovery plus retained image tests. #133 hosted success is not newly rerun positive real-ClamAV detection or full physical-device verification.
- **Dependency / recommendation:** Recommend closure for implemented pipeline; target-host scanner/privacy/storage operation remains a release gate.
- **Inspected source:** [ProfileImageService.cs](../../src/HRFlow.Application/Services/ProfileImageService.cs), [PrivateProfileImageStorage.cs](../../src/HRFlow.Infrastructure/Services/PrivateProfileImageStorage.cs), [EmployeeDirectoryReader.cs](../../src/HRFlow.Infrastructure/Services/Employees/EmployeeDirectoryReader.cs).
- **Dated verification:** [private-profile-images](../verification/private-profile-images.md), [removed-profile-image-hints](../verification/removed-profile-image-hints.md). **Merged PR evidence:** [#119](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/119), [#124](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/124).

## #96 — Extend existing leave reports with scoped formula-safe CSV exports

**GitHub: OPEN. Classification: Partial.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/96)

### Fetched acceptance criteria

- **A1:** Exports repeat live current authorization, active lifecycle and existing scope/range filters; no arbitrary manager or unrestricted employee ID accepted.
- **A2:** Request counts, distinct employees, summed calendar request-days and preserved charged units have separate headers/units; overlaps remain explicit.
- **A3:** Empty/invalid/stale permission and interrupted downloads are safe; filenames and cells disclose no secrets or raw personnel context.
- **A4:** Export access logs contain actor/filter identifiers/counts, not row payloads; results remain session-bound.

- **Criterion disposition:** A1/A3/A4 and current-calendar portion of A2 delivered; historical charged-unit portion remains.
- **Implemented:** Fresh authorised HR/Manager CSV snapshots reuse reports, applied 366/62-day filters, separate counts/people/calendar-day headers, Text: formula protection, 5,000-row/2 MiB fail-not-truncate limits and session-safe cancellation/object URLs.
- **Missing functionality / contract:** A2 preserved charged-unit columns absent pending #22. Current clipped request-days must not be relabelled statutory or unique absence.
- **Verification only:** Independent downloaded-byte parsing and synthetic formula variants recorded. Excel/LibreOffice behaviour explicitly not exercised; no fresh spreadsheet-app test here.
- **Dependency / recommendation:** #75/#22 for charged-unit contract. Do not replace the current secure export implementation.
- **Inspected source:** [ExportLeaveReportsQuery.cs](../../src/HRFlow.Application/Features/LeaveRequests/Queries/ExportLeaveReports/ExportLeaveReportsQuery.cs), [ReportCsvWriter.cs](../../src/HRFlow.Infrastructure/Services/ReportCsvWriter.cs).
- **Dated verification:** [scoped-report-exports](../verification/scoped-report-exports.md). **Merged PR evidence:** [#116](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/116).

## #97 — Create a repeatable synthetic three-role pitch demonstration

**GitHub: OPEN. Classification: Complete.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/97)

### Fetched acceptance criteria

- **A1:** Three-role walkthrough works without manual SQL fixes or page reloads; HR never decides and Manager cannot self-approve.
- **A2:** Only synthetic names/addresses/documents are used; screenshots contain no secrets/local paths.
- **A3:** Each metric's units/date period and legal/legacy limitations are spoken explicitly; no full production/compliance claim.
- **A4:** Fixture restart cannot reactivate existing accounts or overwrite roles; source database is untouched.

- **Criterion disposition:** A1-A4 supported by fixture, runbook, synthetic screenshots and recorded rows; legal/deployment statements remain limitations.
- **Implemented:** Refuse-existing isolated Development fixture, generated private credentials, real CSV/activation/ClamAV/request/decision/history/notification/team/report/export walkthrough and restart/owned-cleanup guards.
- **Missing functionality / contract:** None against A1-A4 current-feature demo. A statutory/production demonstration is not delivered or claimed; keep those parent gates under #98 rather than silently expanding this completed runbook.
- **Verification only:** Dated fresh actual browser and persisted-state rehearsal. Automated duration is not the untimed human 8-10-minute script; fresh scanner definitions/platform rehearsal not rerun now.
- **Dependency / recommendation:** Recommend closure only for current-feature criteria. Real scanner setup required for a future positive rehearsal; no always-clean substitute or actual recipients.
- **Inspected source:** [pitch-demo.mjs](../../tools/pitch/pitch-demo.mjs), [rehearse.mjs](../../tools/pitch/rehearse.mjs), [pitch-rehearsal.md](../../docs/operations/pitch-rehearsal.md).
- **Dated verification:** [pitch-rehearsal](../verification/pitch-rehearsal.md). **Merged PR evidence:** [#118](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/118).

## #98 — HRFlow South African workplace product roadmap

**GitHub: OPEN. Classification: Partial.** [Current issue](https://github.com/Onthangaho/HRFlow-Leave-Management-System/issues/98)

### Fetched acceptance criteria

- **A1:** Review existing #17/#21/#22/#36 evidence before any closure or scope update.
- **A2:** Approve legal requirements and unresolved interpretations before statutory calculation code.
- **A3:** Complete dependency-ordered usable slices with explicit migration/privacy/history decisions.
- **A4:** Retain actual HTTP/row/file/browser evidence and focused regression gates after the explicit test-convention decision; state limitations honestly.
- **A5:** Demonstrate synthetic three-role workflows and operational restore readiness without claiming full legal/production compliance.

- **Criterion disposition:** A1 reviewed here; A2 legal approval absent; A3 substantial slices delivered, parent remainders open; A4 focused gates authorised; A5 synthetic demo/local recovery evidenced, not target-host readiness.
- **Implemented:** Dependency-ordered usable current-feature slices shipped through #133; this review reconciles all 29 issue contracts against source/evidence.
- **Missing functionality / contract:** Qualified decisions, calculation/history/category completion, approved production transport, privacy/retention and target-host operational acceptance remain. Tracker still describes many shipped features and test authorisation as future.
- **Verification only:** Actual hosted gates now run; no tests/builds/benchmarks/legal-source checks rerun in this documentation review. Pitch and hosted success are not pilot certification.
- **Dependency / recommendation:** Owner accepts proposed tracker changes; no issue closure/edit/board move in this phase. Use existing parents and preserve the closed #78 discrepancy.
- **Inspected source:** [05-product-roadmap.md](../../docs/planning/05-product-roadmap.md), [08-current-readiness-review.md](../../docs/planning/08-current-readiness-review.md), [10-proposed-tracker-updates.md](../../docs/planning/10-proposed-tracker-updates.md).
- **Dated verification:** [restored-activation-safety](../verification/restored-activation-safety.md). **Merged PR evidence:** [#120](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/120), [#125](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/125), [#128](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/128), [#130](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/130), [#131](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/131), [#132](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/132), [#133](https://github.com/Onthangaho/HRFlow-Leave-Management-System/pull/133).

## Tracker disposition

Copy-ready #98 replacement, closure caveats, partial updates and the next three slices are in [unpublished proposed updates](10-proposed-tracker-updates.md). The current [roadmap](05-product-roadmap.md) and [readiness review](08-current-readiness-review.md) use this same classification, not a competing backlog.
