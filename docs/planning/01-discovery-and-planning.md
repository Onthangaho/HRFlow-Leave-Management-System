> 14 October 2026: PR #117 is merged. #97 current-feature pitch rehearsal is locally implemented on feat/repeatable-pitch-demo: [runbook](../operations/pitch-rehearsal.md), [ADR 0024](../adr/0024-isolated-current-feature-pitch.md), [actual browser/persisted-row/regression evidence and screenshots](../verification/pitch-rehearsal.md). Development-only fresh private fixtures; no product/schema/dependency change. Hosted GitHub/Linux/Chromium remains billing-blocked. Statutory/cycle/legal, production invitation/deployment and broader reliability gates remain outstanding; #97 stays open. Prepared for independent review, not merged or deployed. Earlier status entries below are historical.

> 14 October 2026: PR #116 is merged. The first explicitly authorised #76 regression slice is implemented locally for review on test/critical-workflow-regressions. [Retained commands and coverage](../../tests/README.md), [strategy](../adr/0023-focused-regression-gates.md), [executed evidence and limitations](../verification/critical-workflow-regressions.md). Focused xUnit/Playwright tests replace the blanket deferral only in this scope. Medical/scanner recovery, comprehensive accessibility, load/deployment and #75/#22-qualified legal calculations remain untested. Hosted CI has not run. No commit/PR/deployment; #76 remains open. Earlier entries below are historical.

> 14 October 2026: PR #115 is merged. #96 current-data scoped CSV exports are locally implemented for review on feat/scoped-report-exports. [API/CSV/safety contract](../adr/0022-scoped-current-data-report-exports.md), [executed API/SQLite/browser evidence and screenshots](../verification/scoped-report-exports.md). Existing deferred projections/calculations reused; no schema/dependency/charging change. #22-dependent charged units remain outstanding and #96 stays open. Prepared for independent PR review; not merged or deployed. Earlier entries below are historical.

> 14 October 2026: PR #114 is merged. #92 current-data HR workspace is locally implemented for review on feat/hr-workspace. [Design/API/attention definitions](../adr/0021-current-data-hr-workspace.md), [actual verification/screenshots/limits](../verification/hr-workspace.md). Read-only operations and reconciled period trends; no schema/dependency/calculation change. #92 stays open for #85/#22/#75. No commit/PR/deployment or next feature. Earlier entries below are historical.

> 14 October 2026: PR #113 is merged. #91 Manager workspace/calendar is locally implemented for review on feat/manager-workspace-calendar. [Design/API](../adr/0020-manager-workspace-calendar.md), [actual verification/screenshots/limits](../verification/manager-workspace-calendar.md). Existing personal dashboard and decision workflow preserved; no new calculation, schema or dependency. Prepared for independent PR review; not merged or deployed. #92 not started. Earlier entries below are historical.

> 14 October 2026: PR #112 is merged. #90 current-data personal dashboard is locally implemented for review on feat/personal-leave-dashboard. [API/design](../adr/0019-personal-leave-dashboard.md), [actual verification/screenshots/limits](../verification/personal-leave-dashboard.md). Existing calendar-day balances are explicit; statutory/cycle completion remains #85/#22/#75-dependent. Prepared for independent PR review; not merged or deployed. #91/#92 not started. Earlier entries below are historical.

> 14 October 2026: PR #111 is merged. #89 CSV onboarding is locally implemented for review on feat/hr-csv-onboarding: [ADR 0018](../adr/0018-hr-csv-onboarding.md), [API/operations](../operations/hr-csv-onboarding.md), [executed evidence and limits](../verification/hr-csv-onboarding.md). Production invitation delivery, qualified calculations and testing convention gates remain unchanged. Prepared for independent PR review; not merged or deployed. Earlier entries below are historical.

> 9 October 2026: PR #110 is merged. #78 confirmed employment facts and fixed weekly schedule history are locally implemented for review on feat/employment-data-foundation. [ADR 0017](../adr/0017-confirmed-employment-data.md), [API/operations](../operations/employment-data-foundation.md), [actual evidence and limits](../verification/employment-data-foundation.md). Legacy values remain Unknown; calendar-day charging is unchanged. #78 stays open for #75/#22-dependent statutory/cycle/holiday scope. Prepared for independent PR review; no CSV onboarding or deployment. Earlier entries are historical status.

> 9 October 2026: PR #109 is merged. The authorised #84 company-configuration/submission-snapshot slice is locally implemented for review; see [ADR 0016](../adr/0016-request-evidence-requirements.md) and [verification/limitations](../verification/request-evidence-requirements.md). #22/#75 statutory/payment dependencies remain unimplemented; no legal-compliance or deployment claim.

# HRFlow — Phase 1: Discovery & Planning

> Current-state update: PR #99 merged on 8 October 2026 after this planning baseline. JWT expiry/current-permission protection is now shipped; #74 stays open for review. Begin the next phase with #75 qualified requirements review and the explicit #76 regression-convention decision; implementation of other roadmap slices needs separate authorization.

**Status:** Reconciled 8 October 2026 · **Owner:** Solo Developer (Product Owner + Engineer)

The original one-week brief is historical context. Current main includes merged PR #73; the
authorised South African expansion uses [roadmap #98](05-product-roadmap.md),
[source-backed legal requirements](04-south-african-leave-requirements.md),
[feature inventory/reconciliation](06-feature-inventory-and-reconciliation.md) and
[focused issue contracts](07-implementation-issue-catalogue.md). No product features are built
in this planning phase. Begin implementation with #74's JWT expiry/current-permission gaps;
#75 qualified legal review gates statutory calculation work. Reuse #21/#22; reconcile #17/#36
before closure. Current calculations are fixed per-type inclusive calendar entitlement, not
statutory cycles/accrual. This plan is not a production or legal-compliance claim.

---

## 1. Business Case

Small-to-mid-sized organizations still run leave management through email threads and shared spreadsheets. This causes lost requests, no audit trail, manual/error-prone leave-balance tracking, and no visibility for managers or HR into department-wide leave patterns.

**HRFlow** solves this with a role-based web platform where employees submit leave requests, managers approve or reject them with a paper trail, and HR administrators manage records, policies, and reporting — with leave balances calculated automatically and every action logged.

As a portfolio project, HRFlow is deliberately chosen because it lets a single developer demonstrate the full range of skills graduate interviewers screen for — secure authentication, role-based authorization, relational data modelling, layered architecture, and a real deployed SPA — inside a domain every interviewer already understands. That means interview time goes to *engineering decisions*, not business-domain explanation.

## 2. Vision Statement

> HRFlow aims to replace spreadsheet-and-email leave tracking with an explainable, auditable system for South African workplaces. Preserve historical decisions and explicitly show incomplete legacy information; do not promise statutory accuracy or a complete submission history where old events were never recorded.

## 3. Project Goals

**Product goals (what the system does):**
- Give employees self-service visibility into their leave balance and request history.
- Give managers a fast, low-friction approval queue.
- Give HR administrators control over employee records, leave policies, and read-only organisation reporting.
- Preserve recorded decisions and their actors/times; missing manager notes remain #21 work.

**Portfolio goals (why this project exists at all):**
- Produce a flagship, fully deployed project demonstrating secure JWT/RBAC authentication, layered architecture, and a professional Git/GitHub workflow.
- Produce a GitHub history (issues → branches → PRs) that itself reads as evidence of real engineering process, not just a code dump.
- Be defensible in a technical interview: every architectural decision has a stated reason.

## 4. Scope

**Original baseline, with current corrections:**
- Employee self-registration is out. #79 now locally replaces initial-password creation with pending one-time activation and private Development pickup; review and a real production delivery provider remain outstanding. See ADR 0011 and its verification report.
- Leave request submission, approval/rejection workflow, and automatic balance calculation.
- Three roles: Employee, Manager, HR Administrator, enforced via RBAC.
- Employee, reporting assignment and leave-type/policy management (HR Admin); department selection, not full department CRUD.
- A basic HR reporting view (pending approvals, leave-by-department).
- JWT authentication with refresh tokens.
- SPA + API deployment is an unverified operational goal (#81), not evidence of a live production system.

**Current expansion boundaries:**
- Focused maintained xUnit/Playwright regression gates are explicitly authorised for the first #76 slice (14 October 2026). Manual/disposable evidence remains useful for uncovered cases; broader frameworks, legal calculations and unrelated test rewrites remain outside this authorisation.
- General status email/SMS remains future scope. #79 plans reviewed secure activation delivery and #93 durable in-app notifications; no notification implementation is claimed just because an old plan named an interface.
- Private documents/images, configurable evidence, employment/schedules/cycles, secure CSV onboarding, profile/password/preferences, shared shell, real role dashboards/calendar and safe exports are now explicitly authorised planning scope, gated by security/legal/privacy review. They are not shipped by this planning work.
- Session persistence, full department CRUD, hard deletion and reactivation remain excluded.
- Payroll integration.
- Multi-tenancy (single organization only).

This scope boundary is itself a portfolio artifact — being able to say "here's what I deliberately left out and why" is a stronger interview signal than pretending nothing was cut.

## 5. Stakeholders

| Stakeholder | Interest |
|---|---|
| Employee (end user) | Fast, transparent leave requests and balance visibility |
| Manager (end user) | Low-friction approval queue, context on team leave |
| HR Administrator (end user) | Control over records/policies, accurate reporting |
| You (Developer / Product Owner) | A portfolio project that is deployable, defensible, and finished on time |
| Graduate Recruiters (indirect stakeholder) | Evidence of production-minded engineering judgment, not tutorial-following |

## 6. Assumptions

- Single developer, working solo — no team velocity assumptions apply; sprint sizing (Phase 4) is calibrated to one person.
- Free/student hosting was the original aspiration; durable database/private-file storage and cost/load require review in #81/#82.
- SQLite-specific Infrastructure protection uses BEGIN IMMEDIATE before write validation and deferred read snapshots for reporting/history. Another provider requires equivalent transactions, migration/type/error handling and cross-process verification; a connection-string/provider-registration change alone is unsafe. See [corrected ADR 0001](../adr/0001-sqlite-for-local-dev.md) and ADRs 0002–0008.
- "Enterprise-grade" here means *engineering discipline at enterprise standard*, not enterprise *scale* — we are not designing for 10,000 concurrent users.

## 7. Constraints

- **Timeline:** the one-week target is historical. Current phases use Essential for pitch / Important next / Future and relative effort; no new delivery dates are promised.
- **Team size:** solo developer — no parallelization of frontend/backend work.
- **Budget:** $0 — free-tier services only.
- **Technology:** preserve current .NET 8/EF Core SQLite and React/TypeScript/Tailwind architecture; provider changes require reviewed equivalent safeguards.
- **Focused testing authorised:** #76 retains critical regression gates; uncovered workflows still require honest manual verification and incremental coverage.

## 8. Success Criteria

MVP is considered complete and interview-ready when:

- [ ] All three roles can log in and see a role-appropriate view.
- [ ] An employee can submit a leave request and see it move through Pending → Approved/Rejected.
- [ ] Leave balances explain their calculation basis; current-policy reductions can yield negative remaining values without rewriting historical approvals. Statutory calculations await reviewed implementation.
- [ ] HR Admin can manage employees and leave types/policies, selecting existing departments; full department CRUD remains excluded.
- [ ] The API is documented (Swagger) and the auth flow is documented in the README.
- [ ] The application is deployed and reachable via a live URL.
- [ ] The GitHub repository shows a real issue → branch → PR history, not a single commit.
- [ ] The README includes an architecture diagram and a clear "what this demonstrates" section.

---

**Next:** review the phased roadmap and reconcile existing tracker contracts before implementing #74. Preserve additive role permissions: Managers decide eligible current direct reports, never self; HR monitors/administers and never gains approval authority. No unsupported compliance or production-readiness claims.
