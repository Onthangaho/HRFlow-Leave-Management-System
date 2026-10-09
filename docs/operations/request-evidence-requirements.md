# Request requirements API and HR configuration

Company configuration only. [Design/limits](../adr/0016-request-evidence-requirements.md) and
[executed verification](../verification/request-evidence-requirements.md).

## HR contracts

Existing HR-only GET/POST/PUT/DELETE management routes and versioned deletion safeguards remain.
POST /api/v1/management/leave-types accepts name, leavePolicyId and descriptionMode,
evidenceMode, evidenceClass, requirementInstructions. Creation defaults are NotRequested,
Optional, Medical and null. PUT /{id} additionally requires expectedVersion and all three mode/class
fields explicitly: omitted requirement fields cannot silently reset an existing type.
Instructions are optional/null, trimmed, at most 1000 characters. The response includes all fields.
An update is an explicit replacement. Preserve loaded versions and all draft fields on failure.

Modes are case-sensitive NotRequested/Optional/Required. Classes are Medical/Ordinary.
Invalid values, oversized instructions and Medical + Required evidence return 400; stale versions
return 409. Current active HR permission is rechecked under the existing writer reservation.
Actor is never client-selected. Policy contracts are unchanged. Requirements are type-specific;
shared policy edits still affect current balances and Pending approval entitlement/overlap.

Use Medical for sick/medical absence and keep evidence Optional or NotRequested. Do not use
Ordinary to impose a medical proof-before-absence gate. The system cannot infer legal purpose from
names, adjudicate certificates or payment, or verify that HR selected the correct category.
Ordinary Required supports company categories unrelated to that medical workflow. Never request
diagnoses. This is not a claim of statutory compliance.

## Personal submission

GET /api/v1/leave-types retains id/name and adds version, policyId, policyVersion, descriptionMode,
evidenceMode, evidenceClass, requirementInstructions. Reads use current authenticated permission
and a consistent snapshot. Personal POST /api/v1/leave-requests accepts leaveTypeId, dates,
expectedTypeVersion, expectedPolicyVersion, optional description and documentIds. The server derives
employee identity. Both expected versions are required nonempty GUIDs from the reviewed selector.
Any type/policy version change returns 409; explicitly reload/review before another submission.
Dates/text/selected uploaded drafts are retained. A type switch never deletes uploads or silently
binds hidden documents. Clear non-requested text/deselect non-requested attachments explicitly.

Description is plain text, at most 1000 trimmed characters, blank null. Required description must
be nonblank. Required Ordinary evidence needs at least one clean, owned, unbound document. At most
five distinct documents, existing format/size/scan safeguards apply. Medical-only configuration
cannot bind Ordinary evidence; already-Medical evidence may satisfy Ordinary requirements without
losing restricted access. Other invalid lifecycle/version/ownership cases retain #83 safe errors.
No successful request, snapshot, binding or audit survives a failed submission transaction.

GET /api/v1/leave-requests/{id}/timeline adds description and submissionRequirements (nullable for
legacy). Snapshot fields: typeId/typeVersion, policyId/policyVersion, descriptionMode/evidenceMode,
evidenceClass/instructions, ruleId/ruleVersion. Existing request scopes remain authoritative.
Medical descriptions are owner/HR-only; eligible Manager readers get null. Raw medical files remain
status-only to Manager-only readers. Other descriptions/snapshots are available only through
existing authorised detail scope; no content is added to notifications/reports. Later configuration
changes never replace stored snapshots or introduce new evidence requirements at approval.

## Operations

Apply the generated migration offline with all writers stopped and a verified backup. No database
is reset or repaired. Empty legacy snapshot means Unknown, not that a requirement was absent.
Backup/restore preserves settings/snapshots and actual evidence using the existing operator tools.
Rollback permanently removes new text/configuration/snapshots; re-upgrade cannot recreate them.
Review and approve legal/payment/calculation dependencies before extending this slice.
