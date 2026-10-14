# ADR 0023: First focused retained regression gates

Date: 14 October 2026. Explicit user authorisation narrows the previous blanket test deferral.
Status: locally implemented for review, not exhaustive completion of #76.

Choose xUnit 2.9.3 with Microsoft.NET.Test.Sdk 17.14.1 / xunit.runner.visualstudio 3.1.5 for
compatible .NET 8 HTTP/persistence gates. Choose Playwright Test 1.64.1 for real browser workflows
and explicit route barriers. These are one .NET framework and one browser runner; no mocking,
EF InMemory, WebApplicationFactory, testcontainers or additional client unit framework is introduced.
Versioned lockfiles keep the test dependencies reviewable. Production dependency manifests are unchanged.

The test executable has a private CLI entry so Playwright shares the same migrated/seeded fixture
as xUnit. It launches the built API rather than adding test-only HTTP provisioning/failure endpoints.
All data/credentials are synthetic/generated and disposable. Development seed configuration is explicitly
overridden with generated passwords; existing authorization, transactions and seed behavior are unchanged.
Real file-backed SQLite and two separate API processes exercise the actual BEGIN IMMEDIATE protocol.
Independent writer reservations and endpoint-entry signals coordinate races. Disposable SQL triggers
inject persistence failures; no production failure switch or automatic replay is introduced.

The migration gate starts at a retained earlier migration and inserts explicitly synthetic original
records, then compares preserved state/Unknown fields after upgrade. It never invents legacy submission
history, reuses a real employee database or defines unreviewed statutory entitlement fixtures.

Real HTTP workflow tests and controlled transport tests have explicit names. Transport 401/barrier tests
prove session/cache/refresh/replay boundaries; they do not imply real JWT expiry/clock-boundary coverage.
API and browser tests assert persisted state/audit/outbox/idempotent rows or authorised persisted timeline
in addition to statuses. Retries are disabled. Negative-control experiments temporarily remove original
stale checks, verify the retained assertions fail, then restore exact source bytes and rebuild.

Privacy outranks rich CI failure artifacts. No traces/video/screenshots are enabled; the safe reporter
removes automatic payload-bearing failure attachments and suppresses call-log error dumps. The restricted
private descriptor is not published. The workflow has contents:read only, immutable official action pins,
no stored credentials or uploaded fixture artifacts. Local results do not prove hosted CI execution.

[Commands, fixture lifecycle and coverage matrix](../../tests/README.md).
[Executed evidence and limitations](../verification/critical-workflow-regressions.md).

Sources checked 14 October 2026:
- [xUnit 2.9.3 release](https://xunit.net/releases/v2/2.9.3)
- [Test SDK 17.14.1](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/17.14.1)
- [Playwright configuration](https://playwright.dev/docs/test-configuration)
- [Playwright screenshot/trace/video options](https://playwright.dev/docs/test-use-options)

Official actions checkout/setup-dotnet/setup-node v5 commit refs were read from their GitHub repositories
before pinning. Hosted Linux/bundled Chromium, scanner recovery, accessibility, load/deployment and
qualified legal calculations remain unverified. No product feature, provider switch or production
security weakening is required for this slice. #76 remains open.
