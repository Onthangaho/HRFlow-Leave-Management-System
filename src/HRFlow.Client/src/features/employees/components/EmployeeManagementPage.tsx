import { useState } from 'react';
import { EmployeeTable } from './EmployeeTable';
import { EmployeeForm } from './EmployeeForm';
import type { Employee } from '../types';

export const EmployeeManagementPage = () => {
  const [editingEmployee, setEditingEmployee] = useState<Employee | null>(null);
  const [isCreating, setIsCreating] = useState(false);

  const handleEdit = (employee: Employee) => {
    setEditingEmployee(employee);
    setIsCreating(false);
  };

  const handleCreate = () => {
    setEditingEmployee(null);
    setIsCreating(true);
  };

  const handleCancel = () => {
    setEditingEmployee(null);
    setIsCreating(false);
  };

  const handleSuccess = () => {
    setEditingEmployee(null);
    setIsCreating(false);
  };

  return (
    <main className="mx-auto mt-16 w-full max-w-5xl space-y-8 rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
      <header>
        <h1 className="text-3xl font-bold text-slate-900">Employee Management</h1>
        <p className="mt-2 text-sm text-slate-600">
          Manage employee records and assignments.
        </p>
      </header>

      {(isCreating || editingEmployee) ? (
        <EmployeeForm
          employee={editingEmployee}
          onSuccess={handleSuccess}
          onCancel={handleCancel}
        />
      ) : (
        <>
          <div className="flex justify-end">
            <button
              onClick={handleCreate}
              className="rounded-lg bg-slate-900 px-4 py-2 font-semibold text-white transition hover:bg-slate-700"
            >
              New Employee
            </button>
          </div>
          <EmployeeTable onEdit={handleEdit} />
        </>
      )}
    </main>
  );
};