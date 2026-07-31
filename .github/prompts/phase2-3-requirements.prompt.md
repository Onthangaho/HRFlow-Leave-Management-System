---
mode: agent
description: Generate HRFlow's combined Business Analysis + Requirements Engineering docs (Phase 2 + 3)
---

Read `docs/planning/01-discovery-and-planning.md` for full context (business case, vision, scope,
stakeholders, constraints, success criteria) before doing anything else.

Acting as a senior business analyst and product owner, produce the following as a single new file,
`docs/planning/02-requirements.md`, in professional Markdown with proper headings:

## 1. Business Requirements

- Functional requirements grouped by role (Employee / Manager / HR Administrator), each as a short,
  numbered, testable statement (e.g., "FR-12: An employee cannot submit a leave request that exceeds
  their current available balance for that leave type.").
- Non-functional requirements: security, performance, reliability, scalability, maintainability,
  accessibility, monitoring/logging — each stated concretely, not generically ("passwords hashed via
  ASP.NET Identity" not "the system shall be secure").
- Business rules as a distinct, numbered list (e.g., leave-balance accrual rules, who can approve whose
  requests, what happens to a pending request if an employee is deactivated).
- Three short user personas (Employee, Manager, HR Administrator) — 3-4 sentences each: goals,
  frustrations with the current spreadsheet/email process, and what success looks like for them.
- One user journey per persona, as a short numbered step sequence (not prose paragraphs).

## 2. Software Requirements Specification

- Convert the functional requirements above into user stories in the standard format: "As a [role], I
  want [capability], so that [benefit]."
- Group user stories into 3-4 epics that map cleanly onto the MVP scope in the Phase 1 doc.
- Give every user story 2-4 concrete, testable acceptance criteria (Given/When/Then format).
- Assign each story a priority (Must / Should / Could, MoSCoW) — the MVP boundary should map to "Must"
  and "Should" only.
- Produce a flat product backlog table: `# | Epic | User Story | Priority | Est. Size (S/M/L)`.
- Restate the MVP definition in one paragraph, cross-checked against the Phase 1 scope section so there's
  no drift between the two documents.

Keep the whole document tight — this feeds directly into GitHub Issues next, so every user story needs
to be small enough to become one Issue (roughly a 30-90 minute implementation task per the project's
working agreement). If a story is bigger than that, split it into multiple stories now rather than later.

End your response with a short summary of epic names and story counts per epic, so I can sanity-check
scope before we turn this into GitHub Issues.
