# HRFlow

<p align="center">
  <strong>A secure, auditable leave-management platform for employees, managers, and HR teams.</strong>
</p>

<p align="center">
  <a href="#-what-it-does">What it does</a> ·
  <a href="#-architecture">Architecture</a> ·
  <a href="#-run-it-locally">Run locally</a> ·
  <a href="#-project-status">Project status</a>
</p>

> **Portfolio project:** HRFlow demonstrates production-minded full-stack engineering: layered .NET architecture, JWT authentication, server-enforced RBAC, auditable leave decisions, and a responsive React single-page application.

## ✨ What it does

HRFlow replaces email-and-spreadsheet leave handling with clear, role-appropriate workflows:

- **🧑‍💼 Employees** submit leave requests, view live balances and decision history, and cancel their own pending requests.
- **🧭 Managers** review and decide only the pending requests of their current, same-department direct reports.
- **🏢 HR Administrators** create and edit employees, assign combined roles and reporting relationships, and monitor organisation-wide pending leave without receiving a decision override.
- **📝 Auditing** records leave lifecycle decisions with the actor, status transition, and timestamp.
- **🛡️ Authentication** uses JWT access tokens and rotating refresh tokens; the client isolates authenticated query caches so one user's data cannot appear after another user signs in.

## 🔐 Role and permission model

Permissions are enforced by the API, not just hidden in the interface.

| Capability | Employee | Manager | HR Administrator |
|---|:---:|:---:|:---:|
| Submit and view personal leave | ✅ | ✅ | ❌* |
| View live balances and personal history | ✅ | ✅ | ❌* |
| Cancel own pending request | ✅ | ✅ | ❌* |
| Approve or reject leave | ❌ | ✅ Direct reports only | ❌ |
| Actionable approval queue | ❌ | ✅ Scoped to direct reports | ❌ |
| Organisation-wide pending monitoring | ❌ | ❌ | ✅ Read-only |
| Employee profiles, roles, and reporting | ❌ | ❌ | ✅ Create/edit |

\* An account with only the **HR Administrator** role has no personal-leave capability. Combined roles receive the capabilities of their explicitly assigned roles.

### Approval safeguards

Managers may decide a request only when all of the following are true:

1. The request is still pending.
2. The actor has the Manager role and an employee record.
3. The request owner currently reports to that manager.
4. Both people belong to the same department.
5. The manager is not deciding their own request.

New personal leave requests require a valid assigned manager. HR monitoring remains deliberately separate from the manager approval queue.

Employee management uses explicit Preserve/Assign/Clear manager updates and original edit versions. See [the contract and SQLite safeguards](docs/adr/0003-hr-employee-management.md).

## 🏗️ Architecture

```text
┌──────────────────────────────────────────────────────────────────┐
│  React 19 + TypeScript SPA                                         │
│  React Router · TanStack Query · Axios · React Hook Form · Zod    │
└───────────────────────────────┬──────────────────────────────────┘
                                │ HTTPS / JSON
┌───────────────────────────────▼──────────────────────────────────┐
│  HRFlow.Api                                                        │
│  ASP.NET Core 8 Controllers · JWT Bearer · Swagger · MediatR      │
└───────────────────────────────┬──────────────────────────────────┘
                                │ depends inward only
┌───────────────────────────────▼──────────────────────────────────┐
│  HRFlow.Application                                                │
│  Commands, queries, validators, application services, interfaces  │
└───────────────────────────────┬──────────────────────────────────┘
                                │
┌───────────────────────────────▼──────────────────────────────────┐
│  HRFlow.Domain                                                     │
│  Leave, employee, department, policy, and audit business rules    │
└───────────────────────────────┬──────────────────────────────────┘
                                │
┌───────────────────────────────▼──────────────────────────────────┐
│  HRFlow.Infrastructure                                             │
│  EF Core 8 · SQLite · ASP.NET Identity · repositories · seeding   │
└──────────────────────────────────────────────────────────────────┘
```

The dependency direction is intentional: the domain has no ASP.NET Core or EF Core dependency, controllers contain no business decisions, and application behavior is expressed through MediatR commands and queries.

## 🧰 Technology

| Area | Current stack |
|---|---|
| Backend | .NET 8, ASP.NET Core Controllers, MediatR, FluentValidation |
| Data and identity | EF Core 8, SQLite, ASP.NET Core Identity |
| Security | JWT bearer authentication, rotating refresh tokens, RBAC, RFC 7807 problem responses |
| Frontend | React 19, TypeScript 6, Vite, Tailwind CSS 4 |
| Client data and forms | TanStack Query 5, Axios, React Hook Form, Zod |
| API exploration | Swagger UI in Development |

## 🚀 Run it locally

### Prerequisites

- [.NET SDK 8](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Node.js](https://nodejs.org/) 20 or later

### 1. Configure the API

The API requires a local JWT signing key. Keep secrets out of source control:

```powershell
dotnet user-secrets set "Authentication:Jwt:SigningKey" "<a-long-random-development-key>" --project src/HRFlow.Api
dotnet user-secrets set "Authentication:Jwt:RefreshTokenPepper" "<a-second-random-development-secret>" --project src/HRFlow.Api
```

Start the API:

```powershell
dotnet run --project src/HRFlow.Api
```

In the default HTTP profile, the API runs on `http://localhost:5228`. Development Swagger UI is available at `/swagger`, and the health endpoint is available at `/health`.

### 2. Configure and start the client

Create `src/HRFlow.Client/.env`:

```dotenv
VITE_API_BASE_URL=http://localhost:5228/api/v1
```

Then install dependencies and run Vite:

```powershell
Set-Location src/HRFlow.Client
npm ci
npm run dev
```

Open the address printed by Vite, normally `http://localhost:5173`.

### 3. Development seed accounts

The Development API seeds local accounts so role boundaries can be exercised on a clean database. These credentials are for local development only and must never be deployed:

| Role | Email | Default password |
|---|---|---|
| HR Administrator | `hr.administrator@hrflow.local` | `HrFlow!Dev2026` |
| Employee | `employee@hrflow.local` | `HrFlow!Employee2026` |
| Manager | `manager@hrflow.local` | `HrFlow!Manager2026` |

The passwords can be overridden through the corresponding `Seeding:*Password` configuration values. Seeding is idempotent, so restarting does not create duplicate accounts.

> **Local database note:** For a migration error on a pre-migration database, reset only a known disposable local store. If any records are needed, preserve and back up the database, stop writers, and review [migration preflight/remediation](docs/adr/0004-leave-policy-management.md#migration-and-legacy-review) before proceeding. Do not delete needed development or production data; startup does not automatically repair legacy records.

## 🧪 Quality checks

The repository currently uses build, lint, and manual end-to-end verification while automated tests are deliberately deferred.

```powershell
dotnet build HRFlow.sln

Set-Location src/HRFlow.Client
npm run build
npm run lint
```

`git diff --check` is also used before review to catch whitespace errors. The API exposes Swagger in Development for authenticated endpoint exploration.

## 📍 Project status

| Area | Status |
|---|---|
| Foundation and authentication | ✅ Layered solution, Identity, JWT login/refresh, protected routing, and server-side role checks are in place. |
| Leave lifecycle | ✅ Personal submission, live balance calculation, history, cancellation, scoped manager approval/rejection, and audit history are implemented. |
| HR operations | ✅ HR employee creation/editing, multiple roles, explicit manager assignment, and organisation-wide pending monitoring are available; employee writes use live HR authorization and version conflicts. |
| Client security | ✅ Authenticated query caches are scoped by user and cleared at session boundaries; refresh and retry flows are fenced against stale sessions. |
| Policy administration | 🚧 Leave types and policies exist in the domain. Full HR CRUD endpoints and UI remain planned work. |
| Reporting | 🚧 Team summaries and department-level reporting remain planned work. |

### Deliberately deferred

The following are out of scope for the current iteration: persistent sessions across full-page refreshes, full department CRUD, automated test suites, real email/SMTP delivery, attachments, payroll integration, and multi-tenancy.

## 🗺️ API highlights

All business endpoints are rooted at `/api/v1`.

| Area | Selected endpoints |
|---|---|
| Authentication | `POST /auth/login`, `POST /auth/refresh` |
| Leave requests | `POST /leave-requests`, `GET /leave-requests/balances`, `GET /leave-requests/history`, `POST /leave-requests/{id}/cancel` |
| Manager decisions | `GET /leave-requests?status=Pending`, `POST /leave-requests/{id}/approve`, `POST /leave-requests/{id}/reject` |
| HR monitoring | `GET /leave-requests/monitoring/pending` |
| Employee management | `GET /employees`, `GET /employees/{id}`, `POST /employees`, `PUT /employees/{id}` |
| Reference data | `GET /departments`, `GET /roles`, `GET /leave-types` |

Swagger is the authoritative interactive contract for a running Development API.

## 📁 Repository layout

```text
src/
├── HRFlow.Api/              # HTTP boundary: controllers, middleware, startup
├── HRFlow.Application/      # Commands, queries, validators, interfaces
├── HRFlow.Domain/           # Entities, enums, domain rules
├── HRFlow.Infrastructure/   # EF Core, Identity, persistence, seed data
└── HRFlow.Client/           # React SPA
docs/
├── planning/                # Discovery, requirements, agile plan
└── adr/                     # Architecture decision records
```

## 🤝 Contributing

This project follows a small, reviewable change workflow:

1. Create a focused branch from current `main`.
2. Keep layer dependencies pointing inward and preserve server-side authorization.
3. Run the smallest relevant build/lint/manual verification.
4. Open a pull request with a concise explanation of the behavior change.

---

Built to demonstrate deliberate engineering choices, not just screens and endpoints.
