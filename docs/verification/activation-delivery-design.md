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


## Main reconciliation - 21 October 2026

PR #129 confirmed merged at 14:57:02 UTC, merge dd018a7ad8ce1b14e96dd08167b46f5945dc218d.
Fetched origin/main and merged it into the existing PR #128 branch. Only the two instruction files
conflicted; their living summaries now preserve the activation-design milestone and the merged
security correction, with byte-identical contents. The proposal remains unchanged and unapproved.
The lockfile and security report are inherited from main, not new delivery-PR implementation.

### Historical failed hosted result (preserved)

Original PR #128 [run 39495689726](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/39495689726)
failed at npm audit. Backend restore/build and frontend build/lint passed (existing Fast Refresh
warning); backend and Chromium tests were skipped. The audit reported follow-redirects 1.16.0 high
(GHSA-q3jh-wrc6-jgq8, GHSA-8r9p-f939-6h3c) and Vite 8.2.1 moderate (GHSA-698v-4r3x-3vcv).
That failure is not rewritten as passed or rerun by documentation checks.

### Separate subsequent evidence

Merged #129 [run 39504378768](https://github.com/Onthangaho/HRFlow-Leave-Management-System/actions/runs/39504378768)
passed hosted Ubuntu build/lint/audits, 88 backend and 33 default Chromium tests. It patches
follow-redirects to 1.16.1 and Vite to 8.2.4 (embedded launch-editor 2.14.2), preserving manifest
ranges. Its [verification record](client-audit-advisories.md) retains local production-built browser
and download evidence separately, plus incomplete attribution of its initial local backend failures.
These are #129 results, not the result of the updated #128 branch.

New work in this reconciliation is merge/source inspection and documentation checks only: relative
links/anchors, strict UTF-8/formatting, whitespace/conflict markers, bounded sensitive-content scan
and instruction byte comparison. No local product build/test or external-source retrieval was rerun.
The updated #128 hosted workflow is triggered after push; its actual result is reported separately
on the PR. No claim of real email delivery, provider/domain approval, production enablement or legal
compliance follows from either hosted regression run. No service connected or message sent.
