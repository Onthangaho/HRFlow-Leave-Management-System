import type { Employee } from '../types';

interface EmployeeTableProps {
  employees: Employee[];
  onResend: (employee: Employee) => void;
  onEdit: (employee: Employee) => void;
  editing: boolean;
  onDeactivate: (employee: Employee) => void;
  onView: (employee: Employee) => void;
}

/** Shows complete roles and current reporting with accessible actions and a scrollable narrow-screen layout. */
export function EmployeeTable({ onResend, employees, onEdit, editing, onDeactivate, onView }: EmployeeTableProps) {
  if (employees.length === 0) {
    return <p className="rounded-lg bg-slate-50 p-6 text-slate-600">No employees match this view. Adjust the search or status filter, or create an employee.</p>;
  }
  return (
    <div tabIndex={0} role="region" aria-label="Employee directory table" className="max-w-full overflow-x-auto rounded-lg border border-slate-200 focus-visible:outline-3 focus-visible:outline-offset-3 focus-visible:outline-indigo-600">
      <table className="w-full min-w-[52rem] divide-y divide-slate-200">
        <caption className="sr-only">Employees and their current departments, roles, and managers</caption>
        <thead className="bg-slate-50">
          <tr>{['Employee', 'Status', 'Department', 'Roles', 'Manager', 'Actions'].map(label =>
            <th key={label} scope="col" className="px-4 py-3 text-left text-sm font-semibold text-slate-600">{label}</th>,
          )}</tr>
        </thead>
        <tbody className="divide-y divide-slate-200">
          {employees.map(employee => (
            <tr key={employee.id}>
              <th scope="row" className="px-4 py-4 text-left font-medium">
                <div className="max-w-60 break-words">{employee.fullName}</div><div className="max-w-60 break-words text-sm font-normal text-slate-600">{employee.email}</div>
              </th>
              <td className="px-4 py-4 text-sm">
                <span className={`inline-flex rounded-full px-2 py-1 text-xs font-semibold ${employee.isActive
                  ? 'bg-emerald-50 text-emerald-800' : 'bg-slate-100 text-slate-700'}`}>
                  {employee.isActive ? 'Active' : 'Inactive'}
                </span>
                <p className="mt-2 text-xs text-slate-600">{employee.requiresActivation ? 'Pending activation' : 'Activated'}</p>
                {employee.requiresActivation && <p className="text-xs">{employee.invitationDeliveryState === 'PickupReady' ? 'Private pickup ready' : employee.invitationDeliveryState === 'Expired' ? 'Invitation expired' : employee.invitationDeliveryState === 'DeliveryFailed' ? 'Delivery failed' : 'Delivery pending'}</p>}
              </td>
              <td className="px-4 py-4 text-sm">{employee.departmentName}</td>
              <td className="px-4 py-4 text-sm">{employee.roles.join(', ') || 'No roles'}</td>
              <td className="px-4 py-4 text-sm">{employee.managerName || 'No manager'}</td>
              <td className="px-4 py-4">
                <div className="flex flex-wrap gap-2">
                  {employee.isActive && employee.requiresActivation && <button type="button" disabled={editing} className="ui-secondary" onClick={() => onResend(employee)}>Resend invitation</button>}
                  {employee.isActive ? <>
                    <button type="button" disabled={editing} aria-label={`Edit ${employee.fullName}`} onClick={() => onEdit(employee)} className="ui-secondary">Edit</button>
                    <button type="button" disabled={editing} aria-label={`Deactivate ${employee.fullName}`} onClick={() => onDeactivate(employee)} className="ui-danger">Deactivate</button>
                  </> : <button type="button" disabled={editing} aria-label={`View details for ${employee.fullName}`} onClick={() => onView(employee)} className="ui-secondary">View details</button>}
                </div>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
