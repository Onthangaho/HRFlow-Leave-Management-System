---
model: GPT-4.1
description: Implement one frontend feature/GitHub Issue in the HRFlow React SPA, following the vertical-slice feature folder structure and commenting standard in copilot-instructions.md
---

You are implementing exactly one GitHub Issue for HRFlow's React/TypeScript frontend (`HRFlow.Client`).
Do not implement anything beyond what's described below — if something else seems needed, ask first.

## Issue

**Title:** ${input:issueTitle}
**Description:** ${input:issueDescription}
**Acceptance criteria:** ${input:acceptanceCriteria}
**API endpoint(s) this depends on:** ${input:apiEndpoints}

## What to do

0. **Before touching any files:** confirm you're on an up-to-date `main` (`git checkout main && git pull`),
   then create and check out a new branch named `feat/<short-description>` (or `fix/…` if this is a bug
   fix). Do not write or edit any code until the branch exists and is checked out. Never commit to `main`.
1. State which feature folder this belongs to (`src/features/<feature-name>/`) — create it if it
   doesn't exist yet, following the pattern: `components/`, `hooks/`, `types.ts`, `api.ts` inside the
   feature folder.
2. Use TanStack Query for all server state (queries/mutations) — do not use `useEffect` + `useState` to
   fetch data. Use React Hook Form + a matching validation schema for any form.
3. Type every API response and request payload explicitly — no `any`. Types should mirror the backend
   DTOs; note if a backend DTO doesn't exist yet so we can flag the mismatch.
4. Add JSDoc comments to exported hooks/components explaining WHY any non-obvious decision was made
   (e.g., why a query is disabled until a condition, why a mutation optimistically updates the cache).
   Don't comment self-explanatory JSX.
5. Respect role-based rendering — if this view/action is role-restricted, guard it using the existing
   auth context/role check pattern (ask me what that pattern is if it's not yet in the codebase, rather
   than inventing a new one).
6. Use Tailwind utility classes for styling; keep components reasonably small and single-purpose rather
   than one large page component.
7. Do NOT write any tests — that phase comes later.
8. Commit your changes on the branch (do not push or open a PR yet — I'll review the diff first).

## End your response with

- **Branch created** (name)
- **Files created / modified** (list)
- **Commit message used** (Conventional Commits format)
- **Suggested PR title**
- Anything you deliberately left out or assumed, so I can confirm before I push and open the PR