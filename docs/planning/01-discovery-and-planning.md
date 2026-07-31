# HRFlow — Phase 1: Discovery & Planning

**Status:** Approved · **Owner:** Solo Developer (Product Owner + Engineer) · **Date:** Week 1, Day 1

---

## 1. Business Case

Small-to-mid-sized organizations still run leave management through email threads and shared spreadsheets. This causes lost requests, no audit trail, manual/error-prone leave-balance tracking, and no visibility for managers or HR into department-wide leave patterns.

**HRFlow** solves this with a role-based web platform where employees submit leave requests, managers approve or reject them with a paper trail, and HR administrators manage records, policies, and reporting — with leave balances calculated automatically and every action logged.

As a portfolio project, HRFlow is deliberately chosen because it lets a single developer demonstrate the full range of skills graduate interviewers screen for — secure authentication, role-based authorization, relational data modelling, layered architecture, and a real deployed SPA — inside a domain every interviewer already understands. That means interview time goes to *engineering decisions*, not business-domain explanation.

## 2. Vision Statement

> For employees, managers, and HR administrators at small-to-mid-sized companies, HRFlow is a secure, role-based web platform that replaces spreadsheet-and-email leave tracking with an auditable, self-service system — unlike ad-hoc manual processes, HRFlow guarantees an accurate, real-time leave balance and a full approval history for every request.

## 3. Project Goals

**Product goals (what the system does):**
- Give employees self-service visibility into their leave balance and request history.
- Give managers a fast, low-friction approval queue.
- Give HR administrators control over employee records, leave policies, and reporting.
- Guarantee every leave decision is auditable — who approved what, when, and why.

**Portfolio goals (why this project exists at all):**
- Produce a flagship, fully deployed project demonstrating secure JWT/RBAC authentication, layered architecture, and a professional Git/GitHub workflow.
- Produce a GitHub history (issues → branches → PRs) that itself reads as evidence of real engineering process, not just a code dump.
- Be defensible in a technical interview: every architectural decision has a stated reason.

## 4. Scope

**In scope for MVP (this week):**
- Employee self-registration is out — HR creates accounts (invite-based), so auth/identity work stays scoped.
- Leave request submission, approval/rejection workflow, and automatic balance calculation.
- Three roles: Employee, Manager, HR Administrator, enforced via RBAC.
- Employee, department, and leave-type/policy management (HR Admin).
- A basic HR reporting view (pending approvals, leave-by-department).
- JWT authentication with refresh tokens.
- Deployed, working SPA + API (Azure free tier).

**Explicitly out of scope for MVP (documented, not forgotten):**
- Automated/manual testing — deferred to a later iteration per your instruction; architecture will be written testably (DI, interfaces) so this isn't a rewrite later.
- Real email/SMTP delivery — status-change notifications will be logged/mocked in v1, with a documented interface (`INotificationService`) ready for a real provider later.
- File attachments on leave requests.
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
- Deployment target is Azure App Service free/student tier; architecture avoids anything that requires a paid tier.
- SQLite is used for local development (zero-install, file-based) via EF Core's SQLite provider; the
  DbContext and repository code are written provider-agnostically so a production deployment could swap
  to SQL Server or PostgreSQL by changing the connection string and provider registration only — no
  application code changes. See `docs/adr/0001-sqlite-for-local-dev.md`.
- "Enterprise-grade" here means *engineering discipline at enterprise standard*, not enterprise *scale* — we are not designing for 10,000 concurrent users.

## 7. Constraints

- **Timeline:** one week before graduate applications open. This is the hard constraint every scope decision above is weighed against.
- **Team size:** solo developer — no parallelization of frontend/backend work.
- **Budget:** $0 — free-tier services only.
- **Technology:** stack is fixed per your brief (ASP.NET Core 8, EF Core, SQL Server, React/TypeScript/Tailwind) — no stack-selection debate needed, which saves real time this week.
- **Testing deferred:** by your explicit instruction, automated testing is out of this iteration — noted here so it isn't silently reintroduced mid-week and eats the schedule.

## 8. Success Criteria

MVP is considered complete and interview-ready when:

- [ ] All three roles can log in and see a role-appropriate view.
- [ ] An employee can submit a leave request and see it move through Pending → Approved/Rejected.
- [ ] Leave balance is calculated automatically and cannot go negative.
- [ ] HR Admin can manage employees, departments, and leave types.
- [ ] The API is documented (Swagger) and the auth flow is documented in the README.
- [ ] The application is deployed and reachable via a live URL.
- [ ] The GitHub repository shows a real issue → branch → PR history, not a single commit.
- [ ] The README includes an architecture diagram and a clear "what this demonstrates" section.

---

**Next:** Phase 2 (Business Analysis) and Phase 3 (Requirements Engineering) — condensed into one combined planning session to protect timeline.
