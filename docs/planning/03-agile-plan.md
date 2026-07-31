# HRFlow — Phase 4: Agile Planning

**Status:** Approved · **Owner:** Solo Developer · **Input:** `docs/planning/02-requirements.md`

---

## 1. Correction Carried Forward

User Story 3's acceptance criteria in the SRS only covered the balance rule (BR-1), not the overlap rule (BR-2). Rather than send it back to Copilot for a rewrite, it's folded directly into the Domain-layer task below (`LeaveRequest aggregate with balance and overlap invariants`), since that's where the acceptance criteria actually needs to live for it to be testable later.

## 2. GitHub Milestones

| Milestone | Scope | Target |
|---|---|---|
| M0: Foundation & Setup | Solution scaffold, auth infrastructure, base entities. | Day 1 |
| M1: Authentication & Access Control | Epic 1 — login, role routing, employee/role administration. | Day 2 |
| M2: Leave Request Lifecycle | Epic 2 — submit, cancel, approve/reject leave requests. | Day 4 |
| M3: Policy & Employee Administration | Epic 3 — leave types/policies, employee deactivation. | Day 5 |
| M4: Reporting & Auditability | Epic 4 — team/department reporting, audit history. | Day 6 |

## 3. Labels

Applied consistently across every issue so the GitHub Projects board can be filtered/grouped by any of these:

- **Epic:** `epic:foundation`, `epic:auth`, `epic:leave-lifecycle`, `epic:policy-admin`, `epic:reporting`
- **Layer:** `layer:domain`, `layer:application`, `layer:infrastructure`, `layer:api`, `layer:client`
- **Priority:** `priority:must`, `priority:should`

## 4. Sprint Plan (1 Week, Solo Developer)

Not a classic 2-week Scrum sprint — a single-week Kanban-style plan sequenced so nothing is built against a dependency that doesn't exist yet (e.g., leave types must exist before leave requests can reference them).

| Day | Milestone Focus | Notes |
|---|---|---|
| Day 1 | M0: Foundation & Setup | DB context + Identity + seed admin + JWT login endpoint + auth context/protected routes. Nothing else is buildable until this exists. |
| Day 2 | M1: Authentication & Access Control | Login UI, server-side role authorization, employee management (domain -> application -> api -> client). |
| Day 3 | M3 (partial): Leave types/policies first | Leave types/policies must exist before a leave request can reference one — pulled ahead of M2's UI work. |
| Day 4 | M2: Leave Request Lifecycle (submit + balance/cancel) | Domain invariants first, then the vertical slice up to the submission form. |
| Day 5 | M2: Leave Request Lifecycle (approve/reject) + M3 (deactivation) | Manager queue + employee deactivation. |
| Day 6 | M4: Reporting & Auditability | Team/department reporting + audit log + audit history view. |
| Day 7 | Buffer: bug fixes, deployment, README/docs polish | No new features — this is deliberately a slack day given a 1-week timeline; if Day 1-6 slipped, this is where it absorbs the slip, not by cutting Must-priority scope. |

## 5. Task Breakdown (GitHub Issues)

32 issues total, each sized to the 30-90 minute / one-branch / one-PR working agreement. Grouped by milestone below; this is the exact list the bulk-creation script (`scripts/create-github-issues.sh`) will create.


### M0: Foundation & Setup

| # | Title | Story | Layer | Priority | Est. |
|---|---|---|---|---|---|
| 1 | Bootstrap EF Core DbContext + ASP.NET Identity with SQLite | Enabler (supports all stories) | `infrastructure` | must | 60 min |
| 2 | Create core Domain entities: Employee, Department (skeleton) | Enabler (supports Story 1, 2) | `domain` | must | 60 min |
| 3 | Seed initial HR Administrator account | Enabler (no self-registration in scope, so a first admin must exist) | `infrastructure` | must | 30 min |
| 4 | JWT login endpoint + refresh token issuance | Enabler (supports Story 1) | `api` | must | 90 min |
| 5 | React auth context, axios interceptor, and protected route wrapper | Enabler (supports Story 1) | `client` | must | 90 min |

### M1: Authentication & Access Control

| # | Title | Story | Layer | Priority | Est. |
|---|---|---|---|---|---|
| 6 | Login page and role-based post-login redirect | User Story 1 | `client` | must | 60 min |
| 7 | Server-side role authorization on protected endpoints | User Story 1 | `api` | must | 45 min |
| 8 | Employee aggregate: create/update with department and role assignment | User Story 2 | `domain` | must | 60 min |
| 9 | Employee management application services (CreateEmployee, UpdateEmployee) | User Story 2 | `application` | must | 60 min |
| 10 | Employee management API endpoints | User Story 2 | `api` | must | 45 min |
| 11 | HR Admin: employee management UI | User Story 2 | `client` | must | 90 min |

### M2: Leave Request Lifecycle

| # | Title | Story | Layer | Priority | Est. |
|---|---|---|---|---|---|
| 12 | LeaveRequest aggregate with balance and overlap invariants | User Story 3 | `domain` | must | 90 min |
| 13 | SubmitLeaveRequest command handler | User Story 3 | `application` | must | 60 min |
| 14 | POST /api/v1/leave-requests endpoint | User Story 3 | `api` | must | 45 min |
| 15 | Leave request submission form | User Story 3 | `client` | must | 90 min |
| 16 | Leave balance query + CancelLeaveRequest command | User Story 4 | `application` | must | 60 min |
| 17 | GET balance + PATCH cancel endpoints | User Story 4 | `api` | must | 45 min |
| 18 | Balance view + leave request history with cancel action | User Story 4 | `client` | must | 60 min |
| 19 | Approval decision domain logic + audit entry creation | User Story 5 | `domain` | must | 60 min |
| 20 | PATCH approve/reject endpoint + manager-scoped pending queue endpoint | User Story 5 | `api` | must | 45 min |
| 21 | Manager approval queue UI | User Story 5 | `client` | must | 90 min |

### M3: Policy & Employee Administration

| # | Title | Story | Layer | Priority | Est. |
|---|---|---|---|---|---|
| 22 | LeaveType and LeavePolicy entities | User Story 6 | `domain` | must | 60 min |
| 23 | Leave type/policy CRUD endpoints | User Story 6 | `api` | must | 60 min |
| 24 | HR Admin: leave type/policy management UI | User Story 6 | `client` | must | 60 min |
| 25 | Employee deactivation with cascading pending-request cancellation | User Story 7 | `application` | should | 60 min |
| 26 | Deactivate endpoint + block new requests from inactive employees | User Story 7 | `api` | should | 45 min |

### M4: Reporting & Auditability

| # | Title | Story | Layer | Priority | Est. |
|---|---|---|---|---|---|
| 27 | Team leave summary query | User Story 8 | `application` | should | 45 min |
| 28 | Manager team summary view | User Story 8 | `client` | should | 60 min |
| 29 | Department-level leave reporting query | User Story 9 | `application` | should | 60 min |
| 30 | HR Admin reporting dashboard | User Story 9 | `client` | should | 90 min |
| 31 | AuditLog entity + write-on-state-change interceptor | User Story 10 | `infrastructure` | must | 60 min |
| 32 | Audit history view on a leave request | User Story 10 | `client` | must | 60 min |

## 6. GitHub Projects Board Columns

`Backlog` -> `Ready` -> `In Progress` -> `In Review` -> `Done`

All 32 issues start in `Backlog`. Move an issue to `Ready` only once its dependency issues (same story, earlier layer) are in `Done` — e.g., don't move the API-layer issue for a story to `Ready` until its Domain-layer issue is `Done`.

## 7. How This Feeds the Copilot Prompts

Each issue's **Title**, **desc** (description), and **ac** (acceptance criteria) map directly onto the `${input:issueTitle}`, `${input:issueDescription}`, and `${input:acceptanceCriteria}` fields in `.github/prompts/backend-feature.prompt.md` / `frontend-feature.prompt.md`. When you run `/backend-feature` or `/frontend-feature` in Copilot Chat, copy those three fields straight from the corresponding GitHub Issue.
