import { ScheduleWorkspace } from './ScheduleWorkspace';
import { ConfirmationDialog } from '../../../components/ConfirmationDialog';
import type { Employee } from '../types';

/** HR-only preserved lifecycle details never offer editing, reactivation or deletion. */
export function EmployeeDetailsDialog({ employee, returnFocus, onClose }: {
  employee: Employee; returnFocus: HTMLElement | null; onClose: () => void;
}) {
  const fields = [
    ['Employee number', employee.employeeNumber ?? 'Unknown'], ['Employment start', employee.employmentStartDate ?? 'Unknown'], ['Name', employee.fullName], ['Email', employee.email], ['Status', employee.isActive ? 'Active' : 'Inactive'],
    ['Activation', employee.requiresActivation ? 'Pending activation' : 'Activated'],
    ['Activated at', employee.activatedAtUtc ? new Date(employee.activatedAtUtc).toLocaleString() : 'Not recorded for existing accounts'],
    ['Department', employee.departmentName], ['Roles', employee.roles.join(', ') || 'No roles'],
    ['Manager', employee.managerName || 'No manager'],
    ['Deactivated at', employee.deactivatedAtUtc ? new Date(employee.deactivatedAtUtc).toLocaleString() : 'Not recorded'],
    ['Deactivated by', employee.deactivatedByName || 'Not recorded'],
    ['Reason', employee.deactivationReason || 'Not recorded'],
  ];
  return <ConfirmationDialog title="Employee details" confirmLabel="Close details" neutral returnFocus={returnFocus} onCancel={onClose} onConfirm={onClose}>
    <dl className="space-y-3">{fields.map(([label, value]) => <div key={label}>
      <dt className="font-semibold text-slate-900">{label}</dt><dd className="whitespace-pre-wrap break-words">{value}</dd>
    </div>)}</dl>
    <ScheduleWorkspace employeeId={employee.id} readOnly />
    <p>This is a read-only historical record. Account access is blocked; historical requests and audits remain preserved.</p>
  </ConfirmationDialog>;
}
