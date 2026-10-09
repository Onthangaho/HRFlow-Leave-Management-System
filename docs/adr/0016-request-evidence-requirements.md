# ADR 0016: Company submission requirements and immutable snapshots

## Decision and limits

Issue #84's current slice is company configuration, not a statutory calculation, payment or
medical-certificate engine. Requirements belong to **LeaveType**, independently of shared
LeavePolicy entitlement/overlap settings. Each type has DescriptionMode and EvidenceMode:
NotRequested, Optional or Required; EvidenceClass: Medical or Ordinary; and optional trimmed
plain-text RequirementInstructions (1000 characters). Type edits rotate the existing version.
A policy edit affects all its linked types' current entitlement/overlap, but does not change their
requirements. Both source versions are captured and checked conservatively on submission.

Medical + Required evidence is rejected in Domain and a database constraint. Sick/medical absence
must be explicitly configured Medical and use Optional or NotRequested evidence. Names do not
classify content. Required Ordinary evidence is only for appropriate company categories; HR must
not misclassify sick/medical absence to introduce a proof-before-reporting gate. This manual
classification limitation is not legal compliance. Even a Medical type still uses the existing
fixed entitlement and manager eligibility rules; this is not an unrestricted absence register.
Later employer-requested proof, payment eligibility, conditional rules, statutory cycles/accrual,
work schedules and reviewed South African calculations remain #22/#75-dependent work. #84 remains
open. See the [requirements distinction](../planning/04-south-african-leave-requirements.md#sick-leave-and-proofpayment).

## Submission protocol

The selector returns current type/policy versions and requirements. The client retains the selected
snapshot independently of background refetches. POST requires expectedTypeVersion and
expectedPolicyVersion; missing versions fail validation, changed versions return 409. A reload is
an explicit review action, retaining dates, description and document selection. Nothing retries
submission automatically. Changing type does not delete drafts or silently remove attachments.
NotRequested forbids nonblank descriptions/any bound documents. The form retains a nonblank text
field for explicit clearing, and retained selected documents remain available for deselection.

Descriptions are at most 1000 trimmed characters; blank becomes null. Plain-text rendering does
not interpret HTML-looking strings. Do not include diagnoses or sensitive medical details.
Document bytes are verified outside SQLite writer protection. Submission then reserves the writer
before authoritative current account, reporting, type/policy and approved-history reads. It checks
versions, modes, required description and at least one clean document when Ordinary evidence is
Required. Binding rechecks owner, unbound/Clean status, original document versions and class.
Medical configuration rejects Ordinary documents. Ordinary configuration permits already-Medical
evidence with its stronger privacy unchanged; binding never reclassifies content. Existing limits,
checksum snapshots, quarantine publication locks/tombstones and cleanup stay intact.

Request, immutable snapshot, binding, Submit audit and notification outbox persist together. There
is no new transition/audit mechanism and no file I/O/scanning under the writer reservation. The
snapshot is a nullable immutable Domain record stored as JSON by an Infrastructure EF converter.
It contains source type/policy IDs/versions, modes, class, instructions and CompanySubmission rule
version 1. This identifier is not a configurable script or a statutory rule.

Approval keeps fresh entitlement/overlap and authorization checks. It does not read current
requirements to demand additional evidence from existing Pending requests. Changing requirements
only affects future submissions; no snapshot/audit backfill is performed.

## Visibility and migration

Only the existing authorised single-request timeline exposes description/snapshot, not broad
reports, queues or notification payloads. Current owner/eligible Manager/HR scopes share the existing
read snapshot. Medical descriptions are additionally withheld from non-owner, non-HR Managers.
Medical document status-only access remains unchanged. Current actor names/audits are unchanged.

Migration AddRequestRequirements adds four type columns and nullable Description and
SubmissionRequirements request columns, plus a type requirement check constraint. Legacy defaults
are description NotRequested, evidence Optional, class Ordinary, instructions null. Ordinary is an
admission floor for migrated optional uploads, not a medical assertion or rewrite of document
classifications; the uploader continues to default to Medical. New HR forms/API default Medical.
Legacy snapshots/descriptions remain null and the UI explicitly says Unknown. SQLite rebuilds the
type table to install the check constraint; existing keys, relationships, versions and rows survive.
Stop writers and take the existing encrypted backup before applying the migration.

Rollback drops configuration, descriptions and snapshots recorded after upgrade. Request/audit
and document history remain, but lost requirement facts cannot be reconstructed by re-upgrade.
Re-upgrade uses legacy defaults/null snapshots; do not treat this as preservation of new facts.
No provider change, legal retention rule, medical proof/payment flow or deployment is included.
