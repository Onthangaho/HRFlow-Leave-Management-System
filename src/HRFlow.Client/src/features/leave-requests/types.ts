/**
 * Represents the pending-request projection returned to reviewers so decisions can be made without
 * loading the employee's full profile.
 */
export interface PendingLeaveRequest {
  id: string;
  employeeId: string;
  employeeFullName: string;
  employeeEmail: string;
  leaveTypeId: string;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  status: 'Pending';
}

/** Represents a server-calculated leave balance for one policy-backed leave type. */
export interface LeaveBalance {
  leaveTypeId: string;
  leaveTypeName: string;
  entitledDays: number;
  usedDays: number;
  remainingDays: number;
}

/** Represents one recorded status transition in an employee's request timeline. */
export interface LeaveRequestDecision {
  decisionNote: string | null;
  action: string;
  actorFullName: string;
  timestamp: string;
  oldStatus: string | null;
  newStatus: string;
}

/** Represents a self-scoped leave request with the audit context needed for employee history. */
export interface EmployeeLeaveRequest {
  id: string;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
  status: 'Pending' | 'Approved' | 'Rejected' | 'Cancelled';
  processedOn: string | null;
  decisionHistory: LeaveRequestDecision[];
}

/** A decision carries context only; the server derives actor and validates the current request. */
export interface LeaveDecision { requestId: string; decisionNote?: string | null }
/** Mirrors the documented domain bound; whitespace-only notes are omitted. */
export const MAX_DECISION_NOTE_LENGTH = 500;
