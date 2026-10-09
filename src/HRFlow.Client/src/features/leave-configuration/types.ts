/** Current policy snapshot; references explain the effects of shared rule changes. */
export interface LeavePolicy {
  id: string; name: string; version: string; allowOverlap: boolean; defaultBalance: number;
  linkedLeaveTypes: { id: string; name: string; requestCount: number }[]; canDelete: boolean;
}
/** A type's version is independent of its shared policy's version. */
export interface ManagedLeaveType {
  descriptionMode: RequirementMode; evidenceMode: RequirementMode; evidenceClass: EvidenceClass; requirementInstructions: string | null;
  id: string; name: string; version: string; leavePolicyId: string; policyName: string;
  policyVersion: string; allowOverlap: boolean; defaultBalance: number; requestCount: number; canDelete: boolean;
}
/** Required policy rules; zero entitlement is supported rather than interpreted as unlimited. */
export interface PolicyInput { name: string; defaultBalance: number; allowOverlap: boolean }
/** Explicit policy assignment for a leave category. */
export type RequirementMode = 'NotRequested' | 'Optional' | 'Required';
export type EvidenceClass = 'Medical' | 'Ordinary';
/** Selector carries original versions for explicit submission review after a configuration change. */
export interface TypeSelection { id: string; name: string; version: string; policyId: string; policyVersion: string;
  descriptionMode: RequirementMode; evidenceMode: RequirementMode; evidenceClass: EvidenceClass; requirementInstructions: string | null }
export interface TypeInput { name: string; leavePolicyId: string; descriptionMode: RequirementMode; evidenceMode: RequirementMode; evidenceClass: EvidenceClass; requirementInstructions: string | null }
/** Original resource version accompanies edits and deletes, never a refetched version. */
export type ConfigurationWrite =
  | { kind: 'leave-policies'; method: 'post' | 'put'; id?: string; body: PolicyInput & { expectedVersion?: string } }
  | { kind: 'leave-types'; method: 'post' | 'put'; id?: string; body: TypeInput & { expectedVersion?: string } }
  | { kind: 'leave-policies' | 'leave-types'; method: 'delete'; id: string; expectedVersion: string };
