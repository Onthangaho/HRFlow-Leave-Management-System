import { useEmployees } from '../api';
import type { Employee } from '../types';

interface EmployeeTableProps {
  onEdit: (employee: Employee) => void;
}

export function EmployeeTable({ onEdit }: EmployeeTableProps) {
  const { data: employees, isLoading, error } = useEmployees();

  if (isLoading) {
    return <div>Loading employees...</div>;
  }

  if (error) {
    return <div className="text-rose-600">Error loading employees: {error.message}</div>;
  }

  return (
    <div className="overflow-x-auto rounded-lg border border-slate-200">
      <table className="min-w-full divide-y divide-slate-200">
        <thead className="bg-slate-50">
          <tr>
            <th scope="col" className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500">
              Full Name
            </th>
            <th scope="col" className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500">
              Email
            </th>
            <th scope="col" className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500">
              Department
            </th>
            <th scope="col" className="px-6 py-3 text-left text-xs font-medium uppercase tracking-wider text-slate-500">
              Role
            </th>
            <th scope="col" className="relative px-6 py-3">
              <span className="sr-only">Edit</span>
            </th>
          </tr>
        </thead>
        <tbody className="divide-y divide-slate-200 bg-white">
          {employees?.map((employee) => (
            <tr key={employee.id} className="hover:bg-slate-50">
              <td className="whitespace-nowrap px-6 py-4 text-sm font-medium text-slate-900">{employee.fullName}</td>
              <td className="whitespace-nowrap px-6 py-4 text-sm text-slate-500">{employee.email}</td>
              <td className="whitespace-nowrap px-6 py-4 text-sm text-slate-500">{employee.departmentName}</td>
              <td className="whitespace-nowrap px-6 py-4 text-sm text-slate-500">{employee.roleName}</td>
              <td className="whitespace-nowrap px-6 py-4 text-right text-sm font-medium">
                <button onClick={() => onEdit(employee)} className="text-slate-600 hover:text-slate-900">
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