# HRFlow — Phase 2/3: Requirements

**Status:** Draft  ·  **Owner:** Solo Developer (Product Owner + Engineer)  ·  **Date:** 2026-07-31

---

## 1. Business Requirements

### Functional Requirements

#### Employee
1. FR-01: An employee can view their current leave balance and request history for each leave type.
2. FR-02: An employee can submit a leave request with a leave type, start date, end date, and reason.
3. FR-03: An employee can cancel a pending leave request before it is approved or rejected.
4. FR-04: An employee can see the current status of each request and the approval history for completed requests.

#### Manager
5. FR-05: A manager can view a queue of pending leave requests for their team.
6. FR-06: A manager can approve or reject a pending leave request with a decision note.
7. FR-07: A manager can view a simple team leave summary so they can plan for coverage.

#### HR Administrator
8. FR-08: An HR administrator can create or update employee records, assign roles, and place employees in departments.
9. FR-09: An HR administrator can define leave types and policies that drive balance calculation.
10. FR-10: An HR administrator can view pending approvals and department-level leave reporting.
11. FR-11: An HR administrator can deactivate an employee without losing historical leave records.

### Non-Functional Requirements
- Security: Authentication uses JWT access and refresh tokens, passwords are hashed through ASP.NET Identity, and server-side authorization enforces Employee, Manager, and HR Administrator permissions on every protected action.
- Performance: Core screens such as login, balance view, approval queue, and reporting should load in under 2 seconds for a single-tenant deployment with up to 500 active employees and 1,000 leave requests.
- Reliability: Leave request state changes must be saved atomically so a request cannot be partially created or approved without a valid persisted outcome.
- Scalability: The application architecture must support the single-organization MVP and remain easy to extend to a larger tenant without rewriting the domain model.
- Maintainability: The solution must follow a layered structure with dependency injection, clear interfaces, and no business logic in controllers or UI components.
- Accessibility: Core workflows must be keyboard accessible, use form labels and clear error messages, and maintain sufficient contrast for text and controls.
- Monitoring and Logging: All leave state changes must be logged with actor, timestamp, old state, new state, and a correlation identifier; sensitive data must never be written to logs.

### Business Rules
1. Leave requests cannot exceed the employee’s currently available balance for the requested leave type.
2. A leave request must be for a valid date range and cannot overlap a previously approved leave request for the same leave type unless the policy explicitly allows it.
3. Only an assigned Manager-role employee may approve or reject a pending request. The manager must be the request owner's current direct manager and in the same department; managers cannot decide their own requests. HR administrators monitor pending work but have no approval override.
4. A pending request may be canceled by the employee before approval; once approved or rejected, the request becomes read-only.
5. Leave balances are calculated from policy rules and approved leave history; no manually stored balance field is used.
6. If an employee is deactivated, any pending request is canceled automatically and historical records remain visible for audit purposes.
7. A new leave request requires a current valid manager assignment. The assigned manager must be an existing same-department employee with the Manager role; self-assignment and reporting cycles are rejected. Existing pending requests are preserved when HR corrects an invalid assignment and use the current valid reporting relationship.

### Role Capability Matrix

| Capability | Employee | Manager | HR Administrator |
|---|---|---|---|
| Submit, view balances/history, cancel own Pending request | Yes | Yes | No, unless separately assigned Employee or Manager |
| Approve/reject Pending requests | No | Only assigned same-department direct reports | No |
| View actionable approval queue | No | Direct-report Pending requests only | No |
| View read-only team leave summary | No | Current same-department direct reports' Approved requests, including inactive history | No, unless separately assigned Manager |
| Monitor organisation-wide Pending requests | No | No | Yes, read-only |
| Report period leave totals by current department | No | No, unless separately assigned HR | Yes, read-only; includes inactive history |
| Own employee, role, department, and leave-policy administration | No | No | Yes; employee creation/editing and reporting assignments are exposed |

> Implementation note: HR employee creation and editing now use current Identity membership,
> complete role collections, explicit Preserve/Assign/Clear reporting updates, and edit versions.
> SQLite writer protection makes profile/Identity/role changes atomic and serializes relationship
> validation. See [the reviewed contract and verification](../adr/0003-hr-employee-management.md).
> Leave type/policy management APIs (#23) are merged in PR #67 and verified with shared current rules, versions,
> restricted deletion, and SQLite coordination with submissions/approvals. See [ADR 0004](../adr/0004-leave-policy-management.md)
> and its verification record. Policy management UI (#24) now includes the connected minimal personal submission form
> needed to verify that HR-created types appear in the real selector; see [UI verification](../verification/leave-policy-management-ui.md).
> Backend rules are unchanged. Deactivation and full department CRUD remain separate.
> Manager coverage planning (#27/#28) now has a locally verified API and month-based list:
> current Manager membership and reporting scope share a read snapshot; only Approved leave
> is shown, and distinct active people are counted once per selected month. See
> [ADR 0006](../adr/0006-manager-team-leave-summary.md) and
> [executed API/browser verification](../verification/manager-team-leave-summary.md).
> HR department reporting (#29/#30) is locally implemented with inclusive period intersections,
> clipped summed request-days, distinct employee/inactive-history metrics and applied-filter
> chart/table views. Attribution uses current departments, without historical reconstruction.
> See [ADR 0007](../adr/0007-hr-department-leave-reporting.md) and
> [executed verification and limitations](../verification/hr-department-leave-reporting.md).

### Personas

#### Employee
An employee wants a simple way to request time off without chasing managers over email. They are frustrated by spreadsheets that are easy to misread and by messages that get lost in inboxes. Success for them means seeing their balance clearly, submitting a request in minutes, and knowing exactly where it stands.

#### Manager
A manager wants to approve leave quickly so team coverage is clear and delays do not cause confusion. They are frustrated by having to search through scattered emails and manually reconcile leave decisions. Success for them means seeing all pending requests in one place and making a decision in one workflow.

#### HR Administrator
An HR administrator wants accurate employee records, consistent leave policies, and dependable reporting. They are frustrated by manual spreadsheets that drift from the truth and by the lack of a reliable audit trail. Success for them means managing policies and employee data centrally and producing reliable leave insights without rework.

### User Journeys
1. Employee: sign in, view balance, submit a request, and track its approval status.
2. Manager: sign in, review pending requests, approve or reject one, and confirm team coverage impact.
3. HR Administrator: sign in, update employee or policy data, monitor pending approvals without deciding them, and check reporting.

---

## 2. Software Requirements Specification

### Epic 1 — Authentication and Access Control

#### User Story 1
As an Employee, I want to sign in with my assigned credentials and access only my role-appropriate workspace, so that I can manage my leave securely.

Acceptance Criteria:
- Given a valid employee account, When I sign in, Then I am redirected to the employee dashboard and can access employee-only routes.
- Given an authenticated employee, When I attempt to open an HR-only or manager-only route, Then the system denies access and shows an authorization error.

Priority: Must  ·  Est. Size: S

#### User Story 2
As an HR Administrator, I want to create and update employee profiles and roles, so that user access and department assignments stay accurate.

Acceptance Criteria:
- Given an HR administrator is signed in, When I create or update an employee profile, Then the employee record is saved with the selected department and role.
- Given a role assignment is changed, When the update is saved, Then subsequent authentication and authorization checks reflect the new role.

Priority: Must  ·  Est. Size: M

### Epic 2 — Leave Request Lifecycle

#### User Story 3
As an Employee, I want to submit a leave request with dates, type, and reason, so that my time off is recorded correctly.

Acceptance Criteria:
- Given I have available leave balance, When I submit a valid request, Then the request is created with Pending status.
- Given I submit a request that exceeds my available balance, When the request is validated, Then the system rejects it with a clear balance error.

Priority: Must  ·  Est. Size: M

#### User Story 4
As an Employee, I want to view my leave balance and cancel a pending request, so that I can correct mistakes before approval.

Acceptance Criteria:
- Given I have an existing request with Pending status, When I choose to cancel it, Then the request status changes to Canceled.
- Given I open my leave history, When I view a completed request, Then I can see its final status and decision history.

Priority: Must  ·  Est. Size: S

#### User Story 5
As a Manager, I want to approve or reject pending requests from my team, so that approvals are handled quickly and consistently.

Acceptance Criteria:
- Given a manager is signed in, When they open the pending approvals queue, Then they see only requests from employees in their scope.
- Given a manager approves or rejects a request, When the action is saved, Then the request status updates and the decision is stored with the actor and timestamp.

Priority: Must  ·  Est. Size: M

### Epic 3 — Policy and Employee Administration

#### User Story 6
As an HR Administrator, I want to define leave types and policies, so that balances are calculated according to the organization’s rules.

Acceptance Criteria:
- Given I create a leave type and policy, When I save it, Then the policy is available for future request validation.
- Given a policy is updated, When a new request is submitted, Then the updated rules govern the balance calculation.

Priority: Must  ·  Est. Size: M

#### User Story 7
As an HR Administrator, I want to deactivate employees without losing historical records, so that the system remains accurate and auditable.

Acceptance Criteria:
- Given an employee is marked inactive, When a new leave request is submitted, Then the system blocks it and shows a clear status message.
- Given a deactivated employee has prior requests, When I view their history, Then the old records remain available for audit.

Priority: Should  ·  Est. Size: S

### Epic 4 — Reporting and Auditability

#### User Story 8
As a Manager, I want to view a simple team leave summary, so that I can plan for staffing and coverage.

Acceptance Criteria:
- Given a manager is signed in, When they open the team summary view, Then they see approved leave by employee and date range.
- Given no team leave exists, When the summary is opened, Then the view shows an empty state rather than an error.

Priority: Should  ·  Est. Size: S

#### User Story 9
As an HR Administrator, I want to view pending approvals and department-level leave reporting, so that I can monitor leave activity across the organization.

Acceptance Criteria:
- Given an HR administrator opens the reporting view, When data exists, Then they see pending requests and leave totals by department.
- Given there are no records for a selected department, When the report is generated, Then the system shows a meaningful empty result.

Priority: Should  ·  Est. Size: M

#### User Story 10
As an HR Administrator, I want to review audit history for leave requests, so that every decision is traceable.

Acceptance Criteria:
- Given a request changes status, When the change is saved, Then an audit entry is created with actor, timestamp, and old/new state.
- Given I open a request’s history, When I review it, Then I can see the full approval lifecycle in order.

Priority: Must  ·  Est. Size: S

### Product Backlog

| # | Epic | User Story | Priority | Est. Size (S/M/L) |
|---|---|---|---|---|
| 1 | Authentication and Access Control | Sign in with assigned credentials and access role-appropriate workspace | Must | S |
| 2 | Authentication and Access Control | Create and update employee profiles and roles | Must | M |
| 3 | Leave Request Lifecycle | Submit a leave request with dates, type, and reason | Must | M |
| 4 | Leave Request Lifecycle | View leave balance and cancel a pending request | Must | S |
| 5 | Leave Request Lifecycle | Approve or reject pending requests | Must | M |
| 6 | Policy and Employee Administration | Define leave types and policies | Must | M |
| 7 | Policy and Employee Administration | Deactivate employees without losing history | Should | S |
| 8 | Reporting and Auditability | View a simple team leave summary | Should | S |
| 9 | Reporting and Auditability | View pending approvals and department leave reporting | Should | M |
| 10 | Reporting and Auditability | Review audit history for leave requests | Must | S |

### MVP Definition
The MVP includes a secure, role-based HRFlow experience for Employees, Managers, and HR Administrators with authentication, leave request submission and approval, automatic balance calculation, basic employee and policy administration, and a simple reporting and audit view. This stays aligned with the Phase 1 scope by excluding self-service registration, attachments, email delivery, payroll integration, and multi-tenancy, while keeping the feature set small enough to deliver within the one-week portfolio timeline.

### Scope Summary
- Epic 1 — Authentication and Access Control: 2 stories
- Epic 2 — Leave Request Lifecycle: 3 stories
- Epic 3 — Policy and Employee Administration: 2 stories
- Epic 4 — Reporting and Auditability: 3 stories
