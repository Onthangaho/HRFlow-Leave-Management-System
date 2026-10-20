# Activation delivery design review - 20 October 2026

## Preparation and scope

PR #127 confirmed MERGED at 19:38:25 UTC, merge 40c2617577ebf4d661d6db1c596156426d18fe90.
Fetched origin/main; clean working tree before creating docs/activation-delivery-design from that base.
GitHub #79 was read and remains Open: Provision HR-created accounts through secure one-time activation.
No tracker edits. [Proposal and exact source links](../adr/0029-production-activation-delivery-proposal.md).

Read both identical instruction files, discovery/roadmap/readiness, current South African requirements,
activation ADR, shared activation and delivery interfaces/implementation, employee creation/edit/
deactivation, CSV confirmation, SQLite transaction helper, controller/page, deployment configuration
and restore security invalidation. This is source inspection, not runtime execution.

## New external research

Official Microsoft application SMTP submission and OAuth guidance, Postmark Email API/delivery webhook
and Mailpit runtime options were retrieved on 20 October 2026. Exact URLs, relevant sections, capability
limits and retrieval failure are in ADR 0029. The attempted Postmark sender-signatures user-guide URL
was unavailable; the successfully retrieved Email API independently states sender confirmation.
Microsoft pages displayed an access notice but returned relevant article content. No authenticated
provider account, pricing/free-tier, service agreement, DNS setup or organisation policy was verified.
No source copies or secrets downloaded into the repository. Recommendations are proposed engineering
choices, not vendor guarantees. No claim of exactly-once delivery or mailbox placement.

## Documentation checks executed

The final working-tree check includes new files: strict UTF-8 decoding, relative file/heading links,
fenced-block balance, trailing whitespace/conflict markers, bounded sensitive-literal scanning and
byte equality of AGENTS.md with the Copilot copy. git diff --check also runs; executable-file scope is
checked against the branch base. Results and counts are recorded after the final check below.

## Evidence not rerun / limitations

No builds, product tests, mail sink, email formatting/redemption or network failure/race cases ran:
no executable file changes are authorised. Earlier activation, CSV, restore and PR #127 CI results
remain dated historical evidence, not proof of a production adapter. All retained tests in ADR 0029
are acceptance proposals. No service connected, email sent, provider purchased, production gate
changed, issue edited/closed or legal approval obtained. Owner decisions D1-D6 are Unassigned/Pending.

Final check outcome: seven Markdown files, including both new files; 102 relative file/anchor links
passed. Strict UTF-8, fenced-block/whitespace/conflict-marker checks, bounded sensitive-literal scan
and instruction byte equality passed. New-text punctuation degraded by the shell pipeline was
corrected to ASCII before final review. git diff --check passed. No executable files changed.

## Publication review

Finalisation inspected all seven documentation changes, including the proposed acknowledgement
guards, explicit rotated resend, synthetic egress-denied test transport and production-disabled
boundary. Official-source attribution remains the dated research above; no fresh provider research
or email verification is claimed. Documentation checks were rerun on the complete staged patch
before the authorised commit. Hosted workflow results are reported on the PR separately and do
not verify a real email transport, domain or mailbox delivery.
