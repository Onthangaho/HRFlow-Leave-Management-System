# HRFlow

HRFlow is a layered HR and leave management scaffold built with ASP.NET Core, EF Core, SQLite, and React.

## Local Setup

Configure a local JWT signing key with User Secrets before starting the API:

`dotnet user-secrets set "Authentication:Jwt:SigningKey" "<your-base64-32-byte-key>" --project src/HRFlow.Api`

## Development-only seeded administrator

When the API starts in `Development`, it creates one HR Administrator account so the system has an initial sign-in path on a clean local database.

This account is development-only and must not exist in any real deployment.

- Email: `hr.administrator@hrflow.local`
- Role: `HR Administrator`
- Password (default): `HrFlow!Dev2026` (override with `Seeding:HrAdministratorPassword` in configuration)

The seed checks for the administrator by email before creating it, so repeated restarts do not create duplicates.

## Development-only seeded employee

When the API starts in `Development`, it creates one Employee account so role-based authorization can be verified on a clean local database.

This account is development-only and must not exist in any real deployment.

- Email: `employee@hrflow.local`
- Role: `Employee`
- Password (default): `HrFlow!Employee2026` (override with `Seeding:EmployeePassword` in configuration)

The seed checks for the employee by email before creating it, so repeated restarts do not create duplicates.

Troubleshooting: if a local `.db` predates the `EnsureCreated` to `MigrateAsync` change and throws a migration error, delete the file and restart.