import { useState } from 'react';
import { useEmployees, useCreateEmployee, useUpdateEmployee } from '../api';
import type { Employee, EmployeeFormValues } from '../types';
import { EmployeeTable } from './EmployeeTable';
import { EmployeeForm } from './EmployeeForm';


export const EmployeeManagementPage = () => {
  const [editingEmployee, setEditingEmployee] = useState<Employee | null>(null);
  const { data: employees, isLoading, error } = useEmployees();
  const createEmployee = useCreateEmployee();
  const updateEmployee = useUpdateEmployee();

  const handleSubmit = (data: EmployeeFormValues) => {
    if (editingEmployee) {
      updateEmployee.mutate({ id: editingEmployee.id, ...data });
    } else {
      createEmployee.mutate(data);
    }
    setEditingEmployee(null);
  };

  if (isLoading) {
    return <div>Loading...</div>;
  }

  if (error) {
    return <div>Error: {error.message}</div>;
  }

  return (
    <div>
      <h1>Employee Management</h1>
      <EmployeeTable employees={employees || []} onEdit={setEditingEmployee} />
      <h2>{editingEmployee ? 'Edit Employee' : 'Create Employee'}</h2>
      <EmployeeForm
        onSubmit={handleSubmit}
        initialValues={editingEmployee ? {
          fullName: editingEmployee.fullName,
          email: editingEmployee.email,
          departmentId: '', // This will need to be fetched
          roleName: editingEmployee.roleName,
        } : undefined}
        isSubmitting={createEmployee.isPending || updateEmployee.isPending}
      />
    </div>
  );
};