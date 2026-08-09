import type { Employee } from '../types';

interface EmployeeTableProps {
  employees: Employee[];
  onEdit: (employee: Employee) => void;
}

export const EmployeeTable = ({ employees, onEdit }: EmployeeTableProps) => {
  return (
    <table>
      <thead>
        <tr>
          <th>Full Name</th>
          <th>Email</th>
          <th>Department</th>
          <th>Role</th>
          <th>Actions</th>
        </tr>
      </thead>
      <tbody>
        {employees.map((employee) => (
          <tr key={employee.id}>
            <td>{employee.fullName}</td>
            <td>{employee.email}</td>
            <td>{employee.departmentName}</td>
            <td>{employee.roleName}</td>
            <td>
              <button onClick={() => onEdit(employee)}>Edit</button>
            </td>
          </tr>
        ))}
      </tbody>
    </table>
  );
};