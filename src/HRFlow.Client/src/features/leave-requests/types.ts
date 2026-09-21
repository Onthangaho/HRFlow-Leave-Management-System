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
