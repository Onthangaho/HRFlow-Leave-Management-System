import { useState } from 'react';
import { useEmployees } from '../api';
import type { Employee } from '../types';
import { EmployeeForm } from './EmployeeForm';
import { EmployeeTable } from './EmployeeTable';

/** Keeps an explicit edit snapshot until save or reload so background refetches cannot silently rebase a stale form. */
export function EmployeeManagementPage() {
  const employees = useEmployees();
  const [editing, setEditing] = useState<Employee | null | undefined>(undefined);
  const [notice, setNotice] = useState('');
  const [reloadError, setReloadError] = useState('');
  const openForm = (employee: Employee | null) => { setEditing(employee); setNotice(''); setReloadError(''); };
  const reload = async () => {
    if (!editing) return;
    const result = await employees.refetch();
    const current = result.data?.find(employee => employee.id === editing.id);
    if (!result.error && current) { setEditing(current); setReloadError(''); }
    else setReloadError('Unable to reload this employee. Try again or cancel the edit.');
  };
  return (
    <main className="mx-auto my-8 w-full max-w-6xl space-y-6 rounded-2xl border border-slate-200 bg-white p-4 shadow-sm sm:p-8">
      <header className="flex flex-wrap items-center justify-between gap-4">
        <div><h1 className="text-3xl font-bold text-slate-900">Employee Management</h1>
          <p className="mt-2 text-sm text-slate-600">Manage employee profiles, access, and reporting relationships.</p></div>
        <button type="button" disabled={editing !== undefined || employees.isPending || Boolean(employees.error)}
          onClick={() => openForm(null)}
          className="rounded-lg bg-slate-900 px-4 py-2 font-semibold text-white disabled:opacity-50">New Employee</button>
      </header>
      {notice && <p role="status" className="text-emerald-700">{notice}</p>}
      {editing !== undefined && <EmployeeForm employee={editing} employees={employees.data ?? []}
        onSuccess={() => { setNotice(editing ? 'Employee updated.' : 'Employee created.'); setEditing(undefined); }}
        onCancel={() => setEditing(undefined)} onReload={reload} reloadError={reloadError} />}
      {employees.isPending ? <p role="status">Loading employees...</p> :
        employees.error ? <div role="alert" className="text-rose-700">
          Unable to load employees. <button type="button" className="underline" onClick={() => void employees.refetch()}>Try again</button>
        </div> : <EmployeeTable employees={employees.data ?? []} onEdit={openForm} editing={editing !== undefined} />}
    </main>
  );
}
