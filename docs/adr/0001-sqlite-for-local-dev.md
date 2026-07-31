# ADR 0001: Use SQLite for local development instead of SQL Server

**Status:** Accepted
**Date:** Week 1

## Context

The original stack (per project brief) specifies SQL Server. Running SQL Server locally normally means
Docker, which isn't available in the current development environment.

## Decision

Use SQLite (via `Microsoft.EntityFrameworkCore.Sqlite`) for local development. Keep all EF Core usage
provider-agnostic in `HRFlow.Infrastructure` — no raw SQL, no SQL-Server-specific column types or
functions in the Domain/Application layers — so the only thing that changes to target SQL Server in a
real deployment is the provider registration and connection string in `Program.cs`/`appsettings.json`.

## Consequences

- **Positive:** zero local install friction, one-week timeline protected, `.db` file is trivial to
  reset/reseed during development.
- **Negative / trade-off:** a few SQL Server-specific EF Core features aren't available in SQLite
  (e.g., certain concurrency-token behaviors, some data types) — noted individually in code comments if
  and when they matter for a specific feature (e.g., the `RowVersion` concurrency token approach used in
  the later e-commerce-style portfolio project won't map 1:1 to SQLite and would need revisiting before a
  real SQL Server migration).
- Deployment target remains Azure SQL / SQL Server for production, as originally scoped — this ADR only
  changes the *local dev* database, not the intended production architecture. Worth stating exactly this
  way in interviews: "I used SQLite locally for iteration speed, with the data layer written so swapping
  to SQL Server in production is a configuration change, not a rewrite."
