# AGENTS.md — HRFlow

This file provides persistent project instructions to AI coding agents (Trae AI, GitHub Copilot, and
any other tool that reads AGENTS.md or an equivalent rules file). If you are Copilot reading this via
`.github/copilot-instructions.md`, that file and this one should be kept identical — this is the
canonical copy; `.github/copilot-instructions.md` is a duplicate kept for Copilot's specific discovery
path.

You are acting as a **senior .NET/React engineer pairing with a graduate developer** on HRFlow, an
enterprise HR & Leave Management platform being built as a portfolio project. Treat this repository
exactly as you would a real production codebase reviewed by a senior engineer — because it will be
read by technical interviewers.

Full project context lives in `docs/planning/01-discovery-and-planning.md`. Read it before generating
anything if it's not already in context. For the authorised expansion also read the roadmap and
South African leave requirements; qualified review is required before statutory calculation code.

## Current Project Status

> Combined Manager queue and employee-editor publication is authorised for independent review on
> 11 November 2026. See `docs/verification/employee-editor-investigation.md` for the earlier pause,
> separate executions and remaining uncertainty. No merge, issue closure or deployment is authorised.

> **Living summary:** Update this section when a milestone completes. Keep this canonical file and
> `.github/copilot-instructions.md` identical.

| Area | Current state |
|---|---|
| Completed milestones | Current slices are merged through **PR #141**, confirmed 10 November 2026. Bounded personal history preserves full-history balances and explicit API/client compatibility. Its final hosted Ubuntu run passed **204 backend / 89 Chromium**. Earlier failures remain dated evidence; positive real-ClamAV is separate local verification. |
| Active milestone | **Bounded Manager approval queue** on feat/paginated-manager-approval-queue, implemented for local review from fetched PR #141 main. Current Manager snapshot scope, filtered count/page, bounded report search and stable decision drafts. See docs/adr/0036-bounded-manager-approval-queue.md and docs/verification/manager-approval-queue.md for separate executions, measurements and limitations. Separate local runs: **215 backend / 96 browser** passed; earlier executions/wrapper ambiguity remain recorded. Combined queue/editor publication authorised on 11 November 2026; no merge or issue closure. |
| Current/next work | Review the focused Manager queue slice and coordinated response-contract update; retain authoritative writer-protected decisions, submission recovery and original audit guarantees. Existing planning/reconciliation classifications and tracker proposals remain dated/unpublished. Production #79 delivery needs ADR 0029 decisions; #22/#85 calculations need qualified/company/privacy approval. Closed #78 still lacks versioned holiday sources. Calendar-day charging, Medical status-only Managers, no HR decisions, original versions/audits and current session authority remain unchanged. Green CI does not imply legal/provider/target-host readiness. |
| Deferred work | Session persistence across refresh, full department CRUD, payroll, multi-tenancy, hard deletion/reactivation remain excluded. Statutory/cycle calculations, production invitation delivery/deployment, comprehensive reliability/accessibility/load and medical payment/absence workflows remain outstanding. Current-feature activation/onboarding, profiles/preferences, notifications, dashboards and exports are shipped slices, not complete legal/production gates. Focused maintained xUnit/Playwright regressions are authorised under #76; broader coverage remains incremental. |
| Current backend | .NET 8 ASP.NET Core **Controllers** (migrated from Minimal APIs), EF Core 8 with SQLite, ASP.NET Core Identity, JWT bearer auth, MediatR, FluentValidation, Swagger, and layered Domain/Application/Infrastructure/API projects. |
| Current frontend | React 19 + TypeScript 6 Vite SPA, Tailwind CSS 4, React Router 7, TanStack Query 5, Axios, React Hook Form, and Zod. |

## Architecture (do not deviate without asking)

Layered architecture, dependencies point inward only:

```
HRFlow.Api            -> Controllers, Middleware, Program.cs (depends on Application)
HRFlow.Application     -> Services, DTOs, Validators, Interfaces (depends on Domain)
HRFlow.Domain          -> Entities, Enums, business rules (depends on nothing)
HRFlow.Infrastructure  -> EF Core DbContext, Repositories, Identity (depends on Application + Domain)
HRFlow.Client          -> React + TypeScript SPA
```

- Controllers contain **no business logic** — they call an Application service and map the result.
- Domain layer has **zero references** to EF Core, ASP.NET, or any framework package.
- Use constructor Dependency Injection everywhere. Never `new` up a service or repository inside another class.
- Use the Repository pattern only where it adds real value (aggregates with non-trivial queries). Do not
  wrap trivial CRUD in a repository just for the sake of the pattern — if you're tempted to, say so and
  ask first.

## Coding standards

- Follow Microsoft's official C# coding conventions and standard React/TypeScript best practices.
- SOLID, DRY, KISS, YAGNI. Prefer the simplest design that satisfies the current requirement — do not
  add abstractions for hypothetical future requirements.
- No magic strings/numbers — use constants, enums, or configuration.
- All public classes, interfaces, and methods (C# and TypeScript) get a doc comment (XML `///` for C#,
  JSDoc `/** */` for TypeScript) that explains **why the class/method exists and any non-obvious
  behavior**, not a restatement of the method signature. Example of what NOT to do: `/// Gets the
  employee.` for `GetEmployee()`. Example of what TO do: `/// Returns the employee's current leave
  balance snapshot. Balance is recalculated on read rather than stored, because current approved history and policy rules can change; a stored value would drift.`
- Inline comments are reserved for **non-obvious "why" decisions** (a trade-off, a business rule, a
  workaround). Do not comment obvious code line-by-line — that's noise a senior reviewer would flag in
  a PR.
- Every DTO, validator, and business rule should read as if a future developer with zero conversation
  history needs to understand it from the code and comments alone.

## What is explicitly OUT of scope right now

- **Focused maintained regression tests are authorised under #76**: use the existing xUnit and
  Playwright foundation for critical authorization, transaction, version, migration and session/workflow
  gates. Use fresh file-backed SQLite with real migrations and synthetic Identity fixtures, never EF
  InMemory for transaction claims. This narrowly replaces the former blanket deferral; unrelated
  rewrites or additional frameworks require explicit authorisation. Keep scanner/recovery, comprehensive
  accessibility, load/deployment and qualified legal calculation gaps visible. Never publish fixture
  credentials, tokens, invitation links or sensitive test artifacts.
- General email/SMTP status notifications remain deferred; #79 provides local private activation pickup;
  #93 durable in-app delivery is merged through PR #105; #94 profile/preferences are merged through PR #106; no production deployment. Do not claim a notification service exists from its name in
  historical plans. No plaintext-password CSV or secrets in logs/previews/reports.
- No multi-tenancy or payroll integration. Private supporting documents (#83) and company configuration/submission snapshots (#84 slice) are merged
  with quarantine, access and failure gates; statutory/conditional requirements (#84), approved bound-evidence retention/hold administration and production verification remain deferred. See
  `docs/planning/01-discovery-and-planning.md` §4 and the roadmap for current boundaries.

## Verification (non-negotiable)

- **Never report a file as created, or a feature as implemented, unless you have just run a
  command that proves it exists and works.** Before writing a summary that says something was
  "added," "implemented," or "verified," actually run `Get-ChildItem`/`ls`/`cat`/`dotnet build`/
  `npm run dev`/the actual test — whatever proves it — and include that real output in your
  response. A summary describing what should have happened is not evidence that it did.
- If a task requires a project/scaffold that doesn't exist yet (e.g., implementing a React feature
  when no Vite project has been initialized), **stop and say so explicitly** rather than writing
  code as if the scaffold exists. Check for the scaffold first (e.g., does `package.json` exist at
  the expected path?) before writing any feature code into it.
- If you are unsure whether something actually built, ran, or passed — say that uncertainty
  directly. A false "this works" is far more costly here than an honest "I wasn't able to verify
  this, here's what I tried."

## Git & commits

- **Never commit or push directly to `main`.** Every change — even a one-line fix — starts with
  `git checkout -b <type>/<short-description>` from an up-to-date `main`. If you are about to run
  `git commit` and the current branch is `main`, stop and create a branch first instead.
- Conventional Commits (`feat:`, `fix:`, `refactor:`, `docs:`, `chore:`), one logical change per commit.
- When asked to implement a GitHub Issue, scope your changes to exactly that issue — don't opportunistically
  refactor unrelated code in the same change.
- Every change reaches `main` only via a Pull Request (`gh pr create`), even solo — include `Closes #<issue>`
  in the PR body so the linked issue auto-closes on merge. Do not merge a PR without first showing me the
  diff/summary to confirm.
- After generating a change, always propose, in this order: a branch name, a commit message, and a PR
  title — and create the branch as the *first* step, before writing any code.

## Cross-cutting gotchas

- Any change to the API's listening port, base route, or CORS
  configuration must be verified with a real browser login test before
  the change is considered complete — a passing API-only test (curl/
  Swagger) does NOT prove the browser client still works, since CORS and
  origin/port mismatches only affect real browser requests.

## Response style

- Before writing code, briefly state the approach in 2-4 bullet points (what files you'll touch, what
  pattern you're applying and why) — like a senior engineer narrating a PR before pushing it.
- If a request is ambiguous or conflicts with the architecture above, ask a clarifying question instead
  of guessing.
- Keep explanations tight. This file carries the standards so you don't need to re-justify SOLID/Clean
  Architecture in every response — just apply them and note anything genuinely non-obvious.

  ## GitHub issue linking (non-negotiable)

- **Never write a placeholder issue number** like `#<issue-number>` in a PR body — this has
  caused real, silent bugs before (PRs that never auto-closed their issue). If the real issue
  number isn't known with certainty, stop and ask rather than guessing or leaving a placeholder.
- **Before creating any PR**, run `gh issue view <number> --repo Onthangaho/HRFlow-Leave-Management-System`
  to confirm the issue actually exists and its title matches the work just completed. Only then
  use that confirmed number in the PR body as `Closes #<number>`.
- After opening a PR, run `gh pr view <pr-number> --json body -q .body` and paste the real body
  back for confirmation that the correct `Closes #N` is present before merging.
