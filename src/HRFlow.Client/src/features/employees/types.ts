/** Existing capabilities supported by HR management; role selection is a complete replacement on edit. */
export const employeeRoles = ['Employee', 'Manager', 'HR Administrator'] as const;
export type EmployeeRole = typeof employeeRoles[number];
export type ManagerAssignment = 'Preserve' | 'Assign' | 'Clear';

/** Complete editable snapshot, including the version used to detect stale forms. */
export interface Employee {
  id: string;
  isActive: boolean;
  /** HR-only account linkage identifies self-deactivation without comparing mutable email. */
  identityUserId: string | null;
  deactivatedAtUtc: string | null;
  deactivatedByName: string | null;
  deactivationReason: string | null;
  fullName: string;
  email: string;
  departmentId: string;
  departmentName: string;
  managerId: string | null;
  managerName: string | null;
  roles: EmployeeRole[];
  version: string;
}

/** Form state keeps manager intent separate from its ID so a profile edit never accidentally clears reporting. */
export interface EmployeeFormValues {
  fullName: string;
  email: string;
  password?: string;
  departmentId: string;
  roles: EmployeeRole[];
  managerAssignment: ManagerAssignment;
  managerId: string;
}

/** Only a created/updated employee ID and its accepted version are returned by writes. */
export interface EmployeeWriteResult {
  employeeId: string;
  version: string;
}

/** The cancellation count is authoritative only after the lifecycle transaction commits. */
export interface EmployeeDeactivationResult extends EmployeeWriteResult {
  isActive: boolean;
  cancelledRequestCount: number;
}
