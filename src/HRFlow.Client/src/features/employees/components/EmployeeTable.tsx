import type { Employee } from '../types';

interface EmployeeTableProps {
  employees: Employee[];
  onEdit: (employee: Employee) => void;
  editing: boolean;
}

/** Shows complete roles and current reporting with accessible actions and a scrollable narrow-screen layout. */
export function EmployeeTable({ employees, onEdit, editing }: EmployeeTableProps) {
  if (employees.length === 0) {
    return <p className="rounded-lg bg-slate-50 p-6 text-slate-600">No employees yet. Create an employee to get started.</p>;
  }
  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200">
      <table className="min-w-full divide-y divide-slate-200">
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
                <div>{employee.fullName}</div><div className="text-sm font-normal text-slate-600">{employee.email}</div>
              </th>
              <td className="px-4 py-4 text-sm">
                <span className={`inline-flex rounded-full px-2 py-1 text-xs font-semibold ${employee.isActive
                  ? 'bg-emerald-50 text-emerald-800' : 'bg-slate-100 text-slate-700'}`}>
                  {employee.isActive ? 'Active' : 'Inactive'}
                </span>
              </td>
              <td className="px-4 py-4 text-sm">{employee.departmentName}</td>
              <td className="px-4 py-4 text-sm">{employee.roles.join(', ') || 'No roles'}</td>
              <td className="px-4 py-4 text-sm">{employee.managerName || 'No manager'}</td>
              <td className="px-4 py-4">
                <button type="button" disabled={editing} aria-label={`Edit ${employee.fullName}`}
                  onClick={() => onEdit(employee)}
                  className="rounded-lg border border-slate-300 px-3 py-2 text-sm font-medium hover:bg-slate-100 disabled:opacity-50">
                  Edit
                </button>
              </td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}
