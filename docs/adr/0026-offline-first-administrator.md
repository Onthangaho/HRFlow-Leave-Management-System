# ADR 0026: one-time offline first-administrator bootstrap

Date: 15 October 2026. Status: implemented locally for review, issue #81 partial slice.

## Decision

Extend the existing Operations composition root, not HTTP or UI registration. An existing current
schema store, explicit confirmed facts/department and private password file are required. Reuse
Identity validators, employment normalization, private-path validation, stopped-process inventory
and exclusive maintenance lease. Reserve SQLite's writer before authoritative checks. Every
Identity save shares the same transaction as department, roles and profile persistence.

Refuse any existing user/profile; validate rather than repair canonical roles. Create missing
canonical roles but grant HR Administrator alone. Bootstrap activation is independent of ordinary
invitation delivery: no delivery service, worker, host or seed is started. Email verification is
not fabricated. Manager/schedule can legitimately remain Unknown. New lease files use explicit
Unix 0600 creation mode; existing permissive files are rejected rather than silently chmodded.

## Consequences

Two processes cannot both provision: exclusive OS lease and database immediate transaction protect
the operation across processes. No callback replay or automatic recovery from uncertain commit.
This is not second-admin creation or account recovery; operators inspect before retrying.
There is no schema change. Account creation remains durable if the tool is removed. No source
credentials, invitation URLs or private inputs appear in outcome logs.

The single-host/local-filesystem constraints remain. The operator must stop all other writers,
including tools that ignore the lease. A successful local test does not certify target-host ACLs,
Linux provisioning, delivery, deployment, load or legal compliance.

See the [command runbook](../operations/offline-first-administrator.md) and
[dated verification](../verification/offline-first-administrator.md).
