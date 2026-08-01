---
model: GPT-4.1
description: Implement one backend feature/GitHub Issue in HRFlow, following the layered architecture and commenting standard in copilot-instructions.md
---

You are implementing exactly one GitHub Issue for HRFlow's backend. Do not implement anything beyond
what's described below — if you think something else is needed, ask first instead of expanding scope.

## Issue

**Title:** ${input:issueTitle}
**Description:** ${input:issueDescription}
**Acceptance criteria:** ${input:acceptanceCriteria}

## What to do

0. **Before touching any files:** confirm you're on an up-to-date `main` (`git checkout main && git pull`),
   then create and check out a new branch named `feat/<short-description>` (or `fix/…` if this is a bug
   fix). Do not write or edit any code until the branch exists and is checked out. Never commit to `main`.
1. Identify which layer(s) this touches (`HRFlow.Domain`, `HRFlow.Application`, `HRFlow.Infrastructure`,
   `HRFlow.Api`) and state that up front, in one line per layer.
2. Implement the change following the architecture and coding standards in `.github/copilot-instructions.md`.
   - Domain rules/invariants belong in the Domain layer, not in a service or controller.
   - Add FluentValidation validators for any new request DTOs.
   - Add XML doc comments to every new public class/method explaining WHY, not just what.
   - Wire up Dependency Injection registrations if you add a new service/interface pair.
3. Update `Program.cs` / DI registration only if required by this change — don't reorganize unrelated
   registrations.
4. If this issue touches the database schema, generate the EF Core migration and briefly state what it
   does (added table/column, new constraint, etc.) — do not apply it automatically, I'll review and
   apply it myself.
5. Do NOT write any tests — that phase comes later.
6. Commit your changes on the branch (do not push or open a PR yet — I'll review the diff first).

## End your response with

- **Branch created** (name)
- **Files created / modified** (list)
- **Commit message used** (Conventional Commits format)
- **Suggested PR title**
- Anything you deliberately left out or assumed, so I can confirm before I push and open the PR