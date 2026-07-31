#!/usr/bin/env bash
# v2 — idempotent, rate-limit-resilient bulk creation of HRFlow's labels/milestones/issues.
#
# Safe to re-run: before creating each issue, it checks whether an OPEN or CLOSED issue with
# the exact same title already exists and skips it if so. This fixes the v1 problem where a
# partial run (e.g. stopped by a GitHub secondary rate limit) couldn't be safely re-run without
# duplicating already-created issues.
#
# Prerequisites: gh installed + `gh auth login` already run.
# Run from the repo root: bash scripts/create-github-issues.sh

REPO=$(gh repo view --json nameWithOwner -q .nameWithOwner)
echo "==> Target repo: $REPO"

echo '==> Creating labels (idempotent by default)'
gh label create "epic:foundation" --color "5319e7" 2>/dev/null || true
gh label create "epic:auth" --color "0e8a16" 2>/dev/null || true
gh label create "epic:leave-lifecycle" --color "1d76db" 2>/dev/null || true
gh label create "epic:policy-admin" --color "fbca04" 2>/dev/null || true
gh label create "epic:reporting" --color "d93f0b" 2>/dev/null || true
gh label create "layer:domain" --color "c5def5" 2>/dev/null || true
gh label create "layer:application" --color "bfd4f2" 2>/dev/null || true
gh label create "layer:infrastructure" --color "bfdadc" 2>/dev/null || true
gh label create "layer:api" --color "c2e0c6" 2>/dev/null || true
gh label create "layer:client" --color "f9d0c4" 2>/dev/null || true
gh label create "priority:must" --color "b60205" 2>/dev/null || true
gh label create "priority:should" --color "fef2c0" 2>/dev/null || true

echo '==> Creating milestones (idempotent by default)'
gh api repos/:owner/:repo/milestones -f title="M0: Foundation & Setup" -f description="Solution scaffold, auth infrastructure, base entities." 2>/dev/null || true
gh api repos/:owner/:repo/milestones -f title="M1: Authentication & Access Control" -f description="Epic 1 — login, role routing, employee/role administration." 2>/dev/null || true
gh api repos/:owner/:repo/milestones -f title="M2: Leave Request Lifecycle" -f description="Epic 2 — submit, cancel, approve/reject leave requests." 2>/dev/null || true
gh api repos/:owner/:repo/milestones -f title="M3: Policy & Employee Administration" -f description="Epic 3 — leave types/policies, employee deactivation." 2>/dev/null || true
gh api repos/:owner/:repo/milestones -f title="M4: Reporting & Auditability" -f description="Epic 4 — team/department reporting, audit history." 2>/dev/null || true

# Helper: returns 0 (true) if an issue with this exact title already exists (open or closed)
issue_exists() {
  local title="$1"
  gh issue list --repo "$REPO" --state all --search "\"$title\" in:title" --json title -q ".[].title" \
    | grep -Fxq "$title"
}

CREATED=0
SKIPPED=0
FAILED=0

echo '==> Creating issues (skipping any that already exist)'
if issue_exists "Bootstrap EF Core DbContext + ASP.NET Identity with SQLite"; then
  echo "  SKIP (already exists): Bootstrap EF Core DbContext + ASP.NET Identity with SQLite"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Bootstrap EF Core DbContext + ASP.NET Identity with SQLite"
  if gh issue create --repo "$REPO" --title "Bootstrap EF Core DbContext + ASP.NET Identity with SQLite" --milestone "M0: Foundation & Setup" --label "epic:foundation,layer:infrastructure,priority:must" --body "$(cat <<'EOF'
**Story:** Enabler (supports all stories)

**Description:**
Set up HRFlowDbContext in HRFlow.Infrastructure, wire ASP.NET Core Identity (IdentityUser/IdentityRole) with SQLite provider, and generate the initial migration.

**Acceptance Criteria:**
- Given the API starts, When migrations are applied, Then the SQLite database file is created with Identity tables.
- Given the DbContext is registered, When resolved via DI in HRFlow.Api, Then no circular or upward-layer references exist.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Bootstrap EF Core DbContext + ASP.NET Identity with SQLite (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Create core Domain entities: Employee, Department (skeleton)"; then
  echo "  SKIP (already exists): Create core Domain entities: Employee, Department (skeleton)"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Create core Domain entities: Employee, Department (skeleton)"
  if gh issue create --repo "$REPO" --title "Create core Domain entities: Employee, Department (skeleton)" --milestone "M0: Foundation & Setup" --label "epic:foundation,layer:domain,priority:must" --body "$(cat <<'EOF'
**Story:** Enabler (supports Story 1, 2)

**Description:**
Define Employee and Department entities in HRFlow.Domain with the self-referencing ManagerId relationship, no EF Core attributes.

**Acceptance Criteria:**
- Given Employee is defined, When Infrastructure configures it via Fluent API, Then ManagerId is a nullable self-referencing FK.
- Given the Domain project is inspected, When checking its references, Then it has zero references to EF Core or ASP.NET packages.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Create core Domain entities: Employee, Department (skeleton) (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Seed initial HR Administrator account"; then
  echo "  SKIP (already exists): Seed initial HR Administrator account"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Seed initial HR Administrator account"
  if gh issue create --repo "$REPO" --title "Seed initial HR Administrator account" --milestone "M0: Foundation & Setup" --label "epic:foundation,layer:infrastructure,priority:must" --body "$(cat <<'EOF'
**Story:** Enabler (no self-registration in scope, so a first admin must exist)

**Description:**
Add a database seeding routine (run on startup in Development only) that creates one HR Administrator user and role.

**Acceptance Criteria:**
- Given the database is empty, When the API starts in Development, Then one HR Administrator account exists with a known dev-only password documented in the README.
- Given the seed runs twice, When the API restarts, Then no duplicate admin account is created.

**Estimated size:** 30 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Seed initial HR Administrator account (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "JWT login endpoint + refresh token issuance"; then
  echo "  SKIP (already exists): JWT login endpoint + refresh token issuance"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: JWT login endpoint + refresh token issuance"
  if gh issue create --repo "$REPO" --title "JWT login endpoint + refresh token issuance" --milestone "M0: Foundation & Setup" --label "epic:foundation,layer:api,priority:must" --body "$(cat <<'EOF'
**Story:** Enabler (supports Story 1)

**Description:**
Implement POST /api/v1/auth/login returning a short-lived JWT access token and a refresh token; implement POST /api/v1/auth/refresh to rotate it.

**Acceptance Criteria:**
- Given valid credentials, When POST /api/v1/auth/login is called, Then a 200 response includes an access token and refresh token.
- Given an expired access token and a valid refresh token, When POST /api/v1/auth/refresh is called, Then a new access token is issued and the old refresh token is invalidated.

**Estimated size:** 90 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: JWT login endpoint + refresh token issuance (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "React auth context, axios interceptor, and protected route wrapper"; then
  echo "  SKIP (already exists): React auth context, axios interceptor, and protected route wrapper"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: React auth context, axios interceptor, and protected route wrapper"
  if gh issue create --repo "$REPO" --title "React auth context, axios interceptor, and protected route wrapper" --milestone "M0: Foundation & Setup" --label "epic:foundation,layer:client,priority:must" --body "$(cat <<'EOF'
**Story:** Enabler (supports Story 1)

**Description:**
Build an AuthContext holding the current user/role and token, an axios interceptor that attaches the bearer token and retries once on 401 via refresh, and a protected route wrapper.

**Acceptance Criteria:**
- Given a user is not authenticated, When they navigate to a protected route, Then they are redirected to /login.
- Given an authenticated user without the required role, When they navigate to a role-gated route, Then they see an access-denied view.

**Estimated size:** 90 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: React auth context, axios interceptor, and protected route wrapper (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Login page and role-based post-login redirect"; then
  echo "  SKIP (already exists): Login page and role-based post-login redirect"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Login page and role-based post-login redirect"
  if gh issue create --repo "$REPO" --title "Login page and role-based post-login redirect" --milestone "M1: Authentication & Access Control" --label "epic:auth,layer:client,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 1

**Description:**
Build the login form (React Hook Form) calling the login endpoint, and redirect to the correct role dashboard on success.

**Acceptance Criteria:**
- Given a valid employee account, When I sign in, Then I am redirected to the employee dashboard.
- Given invalid credentials, When I submit the form, Then a clear inline error is shown without a page reload.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Login page and role-based post-login redirect (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Server-side role authorization on protected endpoints"; then
  echo "  SKIP (already exists): Server-side role authorization on protected endpoints"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Server-side role authorization on protected endpoints"
  if gh issue create --repo "$REPO" --title "Server-side role authorization on protected endpoints" --milestone "M1: Authentication & Access Control" --label "epic:auth,layer:api,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 1

**Description:**
Apply [Authorize(Roles=...)] consistently across controllers so role checks are enforced server-side.

**Acceptance Criteria:**
- Given an authenticated employee, When they call an HR-only endpoint directly, Then the API returns 403 Forbidden.

**Estimated size:** 45 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Server-side role authorization on protected endpoints (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Employee aggregate: create/update with department and role assignment"; then
  echo "  SKIP (already exists): Employee aggregate: create/update with department and role assignment"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Employee aggregate: create/update with department and role assignment"
  if gh issue create --repo "$REPO" --title "Employee aggregate: create/update with department and role assignment" --milestone "M1: Authentication & Access Control" --label "epic:auth,layer:domain,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 2

**Description:**
Add domain-level validation to Employee for required fields and valid department/role assignment.

**Acceptance Criteria:**
- Given an Employee is created without a department, When validated, Then a domain validation error is raised.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Employee aggregate: create/update with department and role assignment (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Employee management application services (CreateEmployee, UpdateEmployee)"; then
  echo "  SKIP (already exists): Employee management application services (CreateEmployee, UpdateEmployee)"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Employee management application services (CreateEmployee, UpdateEmployee)"
  if gh issue create --repo "$REPO" --title "Employee management application services (CreateEmployee, UpdateEmployee)" --milestone "M1: Authentication & Access Control" --label "epic:auth,layer:application,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 2

**Description:**
CQRS command handlers + FluentValidation validators for creating/updating employee profiles, department, and role.

**Acceptance Criteria:**
- Given valid input, When CreateEmployeeCommand is handled, Then a new Identity user and Employee record are created together, or neither is.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Employee management application services (CreateEmployee, UpdateEmployee) (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Employee management API endpoints"; then
  echo "  SKIP (already exists): Employee management API endpoints"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Employee management API endpoints"
  if gh issue create --repo "$REPO" --title "Employee management API endpoints" --milestone "M1: Authentication & Access Control" --label "epic:auth,layer:api,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 2

**Description:**
POST /api/v1/employees, PUT /api/v1/employees/{id} (HR Admin only).

**Acceptance Criteria:**
- Given an HR administrator is signed in, When they create or update an employee, Then the API returns the persisted employee record.

**Estimated size:** 45 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Employee management API endpoints (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "HR Admin: employee management UI"; then
  echo "  SKIP (already exists): HR Admin: employee management UI"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: HR Admin: employee management UI"
  if gh issue create --repo "$REPO" --title "HR Admin: employee management UI" --milestone "M1: Authentication & Access Control" --label "epic:auth,layer:client,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 2

**Description:**
A table + form for HR Admin to manage employees, department, and role, using TanStack Query mutations.

**Acceptance Criteria:**
- Given a role assignment is changed and saved, When the employee list refreshes, Then the new role is reflected without a manual page reload.

**Estimated size:** 90 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: HR Admin: employee management UI (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "LeaveRequest aggregate with balance and overlap invariants"; then
  echo "  SKIP (already exists): LeaveRequest aggregate with balance and overlap invariants"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: LeaveRequest aggregate with balance and overlap invariants"
  if gh issue create --repo "$REPO" --title "LeaveRequest aggregate with balance and overlap invariants" --milestone "M2: Leave Request Lifecycle" --label "epic:leave-lifecycle,layer:domain,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 3

**Description:**
LeaveRequest entity enforces: cannot exceed available balance (BR-1) AND cannot overlap a previously approved request for the same leave type unless the policy allows it (BR-2).

**Acceptance Criteria:**
- Given a request exceeding available balance, When validated, Then a domain exception with a specific balance error is raised.
- Given a request overlapping an approved request for the same leave type, When validated and the policy disallows overlap, Then a domain exception with a specific overlap error is raised.

**Estimated size:** 90 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: LeaveRequest aggregate with balance and overlap invariants (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "SubmitLeaveRequest command handler"; then
  echo "  SKIP (already exists): SubmitLeaveRequest command handler"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: SubmitLeaveRequest command handler"
  if gh issue create --repo "$REPO" --title "SubmitLeaveRequest command handler" --milestone "M2: Leave Request Lifecycle" --label "epic:leave-lifecycle,layer:application,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 3

**Description:**
CQRS command handler + validator wrapping the LeaveRequest aggregate; balance is calculated from policy + approved history, never a stored field.

**Acceptance Criteria:**
- Given I have available leave balance, When I submit a valid request, Then the request is created with Pending status.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: SubmitLeaveRequest command handler (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "POST /api/v1/leave-requests endpoint"; then
  echo "  SKIP (already exists): POST /api/v1/leave-requests endpoint"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: POST /api/v1/leave-requests endpoint"
  if gh issue create --repo "$REPO" --title "POST /api/v1/leave-requests endpoint" --milestone "M2: Leave Request Lifecycle" --label "epic:leave-lifecycle,layer:api,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 3

**Description:**
Endpoint mapping to SubmitLeaveRequestCommand, returning 201 with the created request or 400 Problem Details on a domain rule violation.

**Acceptance Criteria:**
- Given a request that violates balance or overlap rules, When submitted, Then the API returns 400 with a Problem Details response naming which rule failed.

**Estimated size:** 45 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: POST /api/v1/leave-requests endpoint (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Leave request submission form"; then
  echo "  SKIP (already exists): Leave request submission form"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Leave request submission form"
  if gh issue create --repo "$REPO" --title "Leave request submission form" --milestone "M2: Leave Request Lifecycle" --label "epic:leave-lifecycle,layer:client,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 3

**Description:**
React Hook Form with leave type, date range, reason; shows the specific balance/overlap error returned by the API inline.

**Acceptance Criteria:**
- Given the API returns a balance error, When the form receives it, Then the specific error message is shown next to the relevant field.

**Estimated size:** 90 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Leave request submission form (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Leave balance query + CancelLeaveRequest command"; then
  echo "  SKIP (already exists): Leave balance query + CancelLeaveRequest command"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Leave balance query + CancelLeaveRequest command"
  if gh issue create --repo "$REPO" --title "Leave balance query + CancelLeaveRequest command" --milestone "M2: Leave Request Lifecycle" --label "epic:leave-lifecycle,layer:application,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 4

**Description:**
Query handler computing current balance per leave type from policy + approved history; command handler transitioning a Pending request to Canceled.

**Acceptance Criteria:**
- Given I have a Pending request, When I cancel it, Then its status becomes Canceled.
- Given a request is Approved or Rejected, When I attempt to cancel it, Then the API rejects the action.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Leave balance query + CancelLeaveRequest command (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "GET balance + PATCH cancel endpoints"; then
  echo "  SKIP (already exists): GET balance + PATCH cancel endpoints"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: GET balance + PATCH cancel endpoints"
  if gh issue create --repo "$REPO" --title "GET balance + PATCH cancel endpoints" --milestone "M2: Leave Request Lifecycle" --label "epic:leave-lifecycle,layer:api,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 4

**Description:**
GET /api/v1/employees/{id}/leave-balance, PATCH /api/v1/leave-requests/{id}/cancel.

**Acceptance Criteria:**
- Given a non-owner attempts to cancel another employee's request, When called, Then the API returns 403.

**Estimated size:** 45 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: GET balance + PATCH cancel endpoints (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Balance view + leave request history with cancel action"; then
  echo "  SKIP (already exists): Balance view + leave request history with cancel action"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Balance view + leave request history with cancel action"
  if gh issue create --repo "$REPO" --title "Balance view + leave request history with cancel action" --milestone "M2: Leave Request Lifecycle" --label "epic:leave-lifecycle,layer:client,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 4

**Description:**
Employee dashboard section showing balance per leave type and a request history table with a cancel action on Pending rows only.

**Acceptance Criteria:**
- Given I open my leave history, When I view a completed request, Then I can see its final status and decision note.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Balance view + leave request history with cancel action (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Approval decision domain logic + audit entry creation"; then
  echo "  SKIP (already exists): Approval decision domain logic + audit entry creation"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Approval decision domain logic + audit entry creation"
  if gh issue create --repo "$REPO" --title "Approval decision domain logic + audit entry creation" --milestone "M2: Leave Request Lifecycle" --label "epic:leave-lifecycle,layer:domain,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 5

**Description:**
Domain method on LeaveRequest for Approve/Reject that enforces only assigned manager or HR Admin may decide, and raises an AuditEntryCreated domain event.

**Acceptance Criteria:**
- Given an employee attempts to approve their own request, When validated, Then a domain rule violation is raised.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Approval decision domain logic + audit entry creation (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "PATCH approve/reject endpoint + manager-scoped pending queue endpoint"; then
  echo "  SKIP (already exists): PATCH approve/reject endpoint + manager-scoped pending queue endpoint"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: PATCH approve/reject endpoint + manager-scoped pending queue endpoint"
  if gh issue create --repo "$REPO" --title "PATCH approve/reject endpoint + manager-scoped pending queue endpoint" --milestone "M2: Leave Request Lifecycle" --label "epic:leave-lifecycle,layer:api,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 5

**Description:**
PATCH /api/v1/leave-requests/{id}/decision, GET /api/v1/leave-requests?status=Pending scoped to the signed-in manager's direct reports.

**Acceptance Criteria:**
- Given a manager is signed in, When they call the pending queue endpoint, Then only requests from their direct reports are returned.

**Estimated size:** 45 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: PATCH approve/reject endpoint + manager-scoped pending queue endpoint (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Manager approval queue UI"; then
  echo "  SKIP (already exists): Manager approval queue UI"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Manager approval queue UI"
  if gh issue create --repo "$REPO" --title "Manager approval queue UI" --milestone "M2: Leave Request Lifecycle" --label "epic:leave-lifecycle,layer:client,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 5

**Description:**
Table of pending requests for the manager's team with inline approve/reject + decision note, using an optimistic TanStack Query mutation.

**Acceptance Criteria:**
- Given a manager approves or rejects a request, When the action completes, Then the queue updates without a full page reload.

**Estimated size:** 90 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Manager approval queue UI (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "LeaveType and LeavePolicy entities"; then
  echo "  SKIP (already exists): LeaveType and LeavePolicy entities"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: LeaveType and LeavePolicy entities"
  if gh issue create --repo "$REPO" --title "LeaveType and LeavePolicy entities" --milestone "M3: Policy & Employee Administration" --label "epic:policy-admin,layer:domain,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 6

**Description:**
Define LeaveType and LeavePolicy (accrual rule, overlap-allowed flag) in Domain, with Infrastructure Fluent API configuration and migration.

**Acceptance Criteria:**
- Given a LeavePolicy is created, When a balance is calculated, Then the calculation reads accrual rules from this entity, not a hardcoded value.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: LeaveType and LeavePolicy entities (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Leave type/policy CRUD endpoints"; then
  echo "  SKIP (already exists): Leave type/policy CRUD endpoints"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Leave type/policy CRUD endpoints"
  if gh issue create --repo "$REPO" --title "Leave type/policy CRUD endpoints" --milestone "M3: Policy & Employee Administration" --label "epic:policy-admin,layer:api,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 6

**Description:**
Full CRUD for LeaveType/LeavePolicy, HR Admin only.

**Acceptance Criteria:**
- Given a policy is updated, When a new request is submitted afterward, Then the updated rules govern the balance calculation.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Leave type/policy CRUD endpoints (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "HR Admin: leave type/policy management UI"; then
  echo "  SKIP (already exists): HR Admin: leave type/policy management UI"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: HR Admin: leave type/policy management UI"
  if gh issue create --repo "$REPO" --title "HR Admin: leave type/policy management UI" --milestone "M3: Policy & Employee Administration" --label "epic:policy-admin,layer:client,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 6

**Description:**
Simple CRUD table + form for leave types and their policy rules.

**Acceptance Criteria:**
- Given I create a leave type and policy, When I save it, Then it appears immediately in the leave request form's type dropdown.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: HR Admin: leave type/policy management UI (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Employee deactivation with cascading pending-request cancellation"; then
  echo "  SKIP (already exists): Employee deactivation with cascading pending-request cancellation"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Employee deactivation with cascading pending-request cancellation"
  if gh issue create --repo "$REPO" --title "Employee deactivation with cascading pending-request cancellation" --milestone "M3: Policy & Employee Administration" --label "epic:policy-admin,layer:application,priority:should" --body "$(cat <<'EOF'
**Story:** User Story 7

**Description:**
DeactivateEmployeeCommand marks the employee inactive and auto-cancels any Pending requests, writing an audit entry for each auto-cancellation.

**Acceptance Criteria:**
- Given an employee is marked inactive, When the command completes, Then all their Pending requests are Canceled with an audit entry noting the reason as deactivation.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Employee deactivation with cascading pending-request cancellation (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Deactivate endpoint + block new requests from inactive employees"; then
  echo "  SKIP (already exists): Deactivate endpoint + block new requests from inactive employees"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Deactivate endpoint + block new requests from inactive employees"
  if gh issue create --repo "$REPO" --title "Deactivate endpoint + block new requests from inactive employees" --milestone "M3: Policy & Employee Administration" --label "epic:policy-admin,layer:api,priority:should" --body "$(cat <<'EOF'
**Story:** User Story 7

**Description:**
PATCH /api/v1/employees/{id}/deactivate; SubmitLeaveRequest handler rejects requests from inactive employees.

**Acceptance Criteria:**
- Given a deactivated employee, When a new leave request is submitted, Then the API blocks it with a clear status message.
- Given a deactivated employee has prior requests, When I view their history, Then the old records remain visible.

**Estimated size:** 45 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Deactivate endpoint + block new requests from inactive employees (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Team leave summary query"; then
  echo "  SKIP (already exists): Team leave summary query"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Team leave summary query"
  if gh issue create --repo "$REPO" --title "Team leave summary query" --milestone "M4: Reporting & Auditability" --label "epic:reporting,layer:application,priority:should" --body "$(cat <<'EOF'
**Story:** User Story 8

**Description:**
Query returning approved leave by employee and date range for a manager's direct reports.

**Acceptance Criteria:**
- Given no team leave exists, When the summary is queried, Then an empty result set is returned, not an error.

**Estimated size:** 45 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Team leave summary query (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Manager team summary view"; then
  echo "  SKIP (already exists): Manager team summary view"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Manager team summary view"
  if gh issue create --repo "$REPO" --title "Manager team summary view" --milestone "M4: Reporting & Auditability" --label "epic:reporting,layer:client,priority:should" --body "$(cat <<'EOF'
**Story:** User Story 8

**Description:**
A simple calendar/list view of approved team leave for coverage planning.

**Acceptance Criteria:**
- Given a manager is signed in, When they open the team summary view, Then they see approved leave by employee and date range.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Manager team summary view (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Department-level leave reporting query"; then
  echo "  SKIP (already exists): Department-level leave reporting query"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Department-level leave reporting query"
  if gh issue create --repo "$REPO" --title "Department-level leave reporting query" --milestone "M4: Reporting & Auditability" --label "epic:reporting,layer:application,priority:should" --body "$(cat <<'EOF'
**Story:** User Story 9

**Description:**
Query aggregating pending requests and leave totals by department for HR Admin.

**Acceptance Criteria:**
- Given there are no records for a selected department, When the report is generated, Then the system shows a meaningful empty result.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Department-level leave reporting query (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "HR Admin reporting dashboard"; then
  echo "  SKIP (already exists): HR Admin reporting dashboard"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: HR Admin reporting dashboard"
  if gh issue create --repo "$REPO" --title "HR Admin reporting dashboard" --milestone "M4: Reporting & Auditability" --label "epic:reporting,layer:client,priority:should" --body "$(cat <<'EOF'
**Story:** User Story 9

**Description:**
Dashboard combining pending approvals count and department leave totals, using Recharts for the department breakdown.

**Acceptance Criteria:**
- Given data exists, When the dashboard loads, Then pending requests and leave totals by department are both visible without navigating away.

**Estimated size:** 90 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: HR Admin reporting dashboard (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "AuditLog entity + write-on-state-change interceptor"; then
  echo "  SKIP (already exists): AuditLog entity + write-on-state-change interceptor"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: AuditLog entity + write-on-state-change interceptor"
  if gh issue create --repo "$REPO" --title "AuditLog entity + write-on-state-change interceptor" --milestone "M4: Reporting & Auditability" --label "epic:reporting,layer:infrastructure,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 10

**Description:**
AuditLog entity (actor, timestamp, entity, old state, new state, correlation id); an EF Core SaveChanges interceptor writes an entry whenever a LeaveRequest changes status.

**Acceptance Criteria:**
- Given a request changes status, When the change is saved, Then an audit entry is created with actor, timestamp, and old/new state.
- Given sensitive fields exist on Employee, When an audit entry is written, Then sensitive data is never included in the log payload.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: AuditLog entity + write-on-state-change interceptor (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

if issue_exists "Audit history view on a leave request"; then
  echo "  SKIP (already exists): Audit history view on a leave request"
  SKIPPED=$((SKIPPED+1))
else
  echo "  Creating: Audit history view on a leave request"
  if gh issue create --repo "$REPO" --title "Audit history view on a leave request" --milestone "M4: Reporting & Auditability" --label "epic:reporting,layer:client,priority:must" --body "$(cat <<'EOF'
**Story:** User Story 10

**Description:**
A read-only timeline component showing the full status history of a single leave request.

**Acceptance Criteria:**
- Given I open a request's history, When I review it, Then I can see the full approval lifecycle in chronological order.

**Estimated size:** 60 minutes

EOF
)" ; then
    CREATED=$((CREATED+1))
  else
    echo "  FAILED: Audit history view on a leave request (see error above — will not stop the rest of the run)"
    FAILED=$((FAILED+1))
  fi
  sleep 2   # small delay to avoid GitHub's secondary rate limit on rapid issue creation
fi

echo "==> Done. Created=$CREATED  Skipped=$SKIPPED  Failed=$FAILED"
if [ "$FAILED" -gt 0 ]; then
  echo "==> $FAILED issue(s) failed — re-run this script, it will skip what succeeded and retry the rest."
fi