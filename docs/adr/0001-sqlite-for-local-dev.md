# ADR 0001: Use SQLite for local development instead of SQL Server

**Status:** Accepted
**Date:** Week 1

## Context

The original stack (per project brief) specifies SQL Server. Running SQL Server locally normally means
Docker, which isn't available in the current development environment.

## Decision

Use SQLite (via `Microsoft.EntityFrameworkCore.Sqlite`) for local development. Keep provider-specific
behaviour in Infrastructure behind Application interfaces; Domain remains framework-free.
The original configuration-only portability assumption is superseded by the implemented transaction
design in ADRs 0002–0008. `SqliteWriteTransaction` explicitly uses non-deferred `BEGIN IMMEDIATE`
before authoritative validation reads; reporting/history use deferred SQLite read snapshots.
Migrations also contain SQLite-specific SQL and constraints. A move to SQL Server/PostgreSQL requires
equivalent isolation/locking and error mapping, reviewed migrations/types/concurrency behaviour,
and cross-process verification. A connection-string/provider-registration change alone is unsafe.

## Consequences

- **Positive:** zero local install friction, one-week timeline protected, `.db` file is trivial to
  reset/reseed during development.
- **Negative / trade-off:** a few SQL Server-specific EF Core features aren't available in SQLite
  (e.g., certain concurrency-token behaviors, some data types) — noted individually in code comments if
  and when they matter for a specific feature (e.g., the `RowVersion` concurrency token approach used in
  the later e-commerce-style portfolio project won't map 1:1 to SQLite and would need revisiting before a
  real SQL Server migration).
- Azure SQL / SQL Server was the original intended target, not an executed deployment or portability
  proof. Hosting/provider selection must be reviewed against durable storage, locking, cost and load.
  SQLite serializes the whole database's writers and has bounded per-command waits without application
  replay. Do not imply enterprise throughput, network-filesystem safety or interchangeable isolation.

## Review correction (8 October 2026)

The South African product roadmap explicitly schedules deployment/restore and provider evaluation.
This documentation correction changes no persistence code or database. Preserve backups and existing
records; even a local reset is appropriate only for explicitly disposable data.
