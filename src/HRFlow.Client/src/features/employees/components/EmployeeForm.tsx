import { useEffect } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { isAxiosError } from 'axios';
import { useCreateEmployee, useUpdateEmployee, useDepartments, useRoles } from '../api';
import { employeeRoles, type Employee, type EmployeeFormValues } from '../types';

const schema = z.object({
  fullName: z.string().trim().min(1, 'Full name is required').max(200),
  email: z.string().trim().email('Enter a valid email address').max(256),
  password: z.string().optional(),
  departmentId: z.string().min(1, 'Select a department'),
  roles: z.array(z.enum(employeeRoles)).min(1, 'Select at least one role'),
  managerAssignment: z.enum(['Preserve', 'Assign', 'Clear']),
  managerId: z.string(),
});

interface EmployeeFormProps {
  employee: Employee | null;
  employees: Employee[];
  onSuccess: () => void;
  onCancel: () => void;
  onReload: () => Promise<void>;
  reloadError: string;
}

const inputClass = 'mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-slate-900 focus:border-slate-600';
const errorClass = 'mt-1 text-sm text-rose-700';

/** Preserves the original version, roles, and reporting intent until the user explicitly reloads or saves. */
export function EmployeeForm({ employee, employees, onSuccess, onCancel, onReload, reloadError }: EmployeeFormProps) {
  const isEditing = Boolean(employee);
  const departments = useDepartments();
  const availableRoles = useRoles();
  const create = useCreateEmployee();
  const update = useUpdateEmployee();
  const formSchema = schema.superRefine((values, context) => {
    if (!isEditing && !/^(?=.*[a-z])(?=.*[A-Z])(?=.*[0-9])(?=.*[^a-zA-Z0-9]).{8,}$/.test(values.password ?? '')) {
      context.addIssue({ code: 'custom', path: ['password'], message: 'Use at least 8 characters with uppercase, lowercase, a number, and a symbol.' });
    }
    if (isEditing && values.managerAssignment === 'Assign' && !values.managerId) {
      context.addIssue({ code: 'custom', path: ['managerId'], message: 'Select a manager to assign.' });
    }
  });
  const { register, handleSubmit, reset, control, setValue, formState: { errors, isSubmitting } } = useForm<EmployeeFormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: { fullName: '', email: '', departmentId: '', roles: [], managerAssignment: 'Clear', managerId: '', password: '' },
  });
  const departmentId = useWatch({ control, name: 'departmentId' });
  const managerAssignment = useWatch({ control, name: 'managerAssignment' });
  const eligibleManagers = employees.filter(candidate =>
    candidate.isActive && candidate.id !== employee?.id && candidate.departmentId === departmentId && candidate.roles.includes('Manager'),
  );
  const mutation = employee ? update : create;
  const busy = mutation.isPending || isSubmitting;
  const choicesUnavailable = departments.isPending || availableRoles.isPending ||
    Boolean(departments.error || availableRoles.error) || !departments.data?.length || !availableRoles.data?.length;

  useEffect(() => {
    reset(employee ? {
      fullName: employee.fullName, email: employee.email, departmentId: employee.departmentId,
      roles: [...employee.roles], managerAssignment: 'Preserve', managerId: '',
    } : { fullName: '', email: '', password: '', departmentId: '', roles: [], managerAssignment: 'Clear', managerId: '' });
  }, [employee, reset]);

  let failure = '';
  let conflict = false;
  if (mutation.error) {
    failure = 'Unable to save this employee. Please try again.';
    if (isAxiosError(mutation.error)) {
      const problem = mutation.error.response?.data;
      conflict = mutation.error.response?.status === 409;
      failure = typeof problem?.detail === 'string' ? problem.detail : failure;
      if (problem?.errors && typeof problem.errors === 'object') {
        failure = Object.values(problem.errors).flat().filter(value => typeof value === 'string').join(' ');
      }
      if (mutation.error.response?.status === 403) failure = 'You no longer have permission to manage employees. Sign in again or contact HR.';
    }
  }

  const submit = handleSubmit(async values => {
    if (mutation.isPending) return;
    try {
      if (employee) await update.mutateAsync({ ...values, id: employee.id, expectedVersion: employee.version });
      else await create.mutateAsync(values);
      onSuccess();
    } catch {
      // Mutation state displays the safe server error and keeps the user's unsaved input available.
    }
  });
  const departmentRegistration = register('departmentId');
  const managerRegistration = register('managerAssignment');

  return (
    <section className="rounded-xl border border-slate-200 bg-slate-50 p-4 sm:p-6" aria-labelledby="employee-form-title">
      <h2 id="employee-form-title" className="text-xl font-bold">{employee ? 'Edit Employee' : 'New Employee'}</h2>
      {departments.isPending || availableRoles.isPending ? <p role="status" className="mt-4">Loading choices...</p> : null}
      {(departments.error || availableRoles.error) && <div role="alert" className={errorClass}>
        Unable to load department or role choices. <button type="button" className="underline"
          onClick={() => { void departments.refetch(); void availableRoles.refetch(); }}>Try again</button>
      </div>}
      {!departments.isPending && departments.data?.length === 0 && <p role="alert" className={errorClass}>No departments are available. Contact your administrator.</p>}
      {!availableRoles.isPending && availableRoles.data?.length === 0 && <p role="alert" className={errorClass}>No roles are available. Contact your administrator.</p>}
      {failure && <div role="alert" className="mt-4 whitespace-pre-line rounded-lg bg-rose-50 p-3 text-sm text-rose-800">
        {failure}
        {conflict && employee && <button type="button" disabled={busy} className="ml-2 underline"
          onClick={() => { void onReload().then(() => update.reset()); }}>Reload latest values</button>}
      </div>}
      {reloadError && <p role="alert" className={errorClass}>{reloadError}</p>}
      <form onSubmit={submit} className="mt-6">
        <fieldset disabled={busy} className="grid grid-cols-1 gap-5 sm:grid-cols-2">
          <legend className="sr-only">Employee details</legend>
          <div><label htmlFor="employee-full-name" className="text-sm font-medium">Full name</label>
            <input id="employee-full-name" autoComplete="name" className={inputClass} aria-invalid={Boolean(errors.fullName)}
              aria-describedby={errors.fullName ? 'full-name-error' : undefined} {...register('fullName')} />
            {errors.fullName && <p id="full-name-error" className={errorClass}>{errors.fullName.message}</p>}</div>
          <div><label htmlFor="employee-email" className="text-sm font-medium">Email</label>
            <input id="employee-email" type="email" autoComplete="off" className={inputClass} aria-invalid={Boolean(errors.email)}
              aria-describedby={errors.email ? 'email-error' : undefined} {...register('email')} />
            {errors.email && <p id="email-error" className={errorClass}>{errors.email.message}</p>}</div>
          {!employee && <div className="sm:col-span-2"><label htmlFor="employee-password" className="text-sm font-medium">Initial password</label>
            <input id="employee-password" type="password" autoComplete="new-password" className={inputClass}
              aria-invalid={Boolean(errors.password)} aria-describedby="password-help password-error" {...register('password')} />
            <p id="password-help" className="mt-1 text-xs text-slate-600">At least 8 characters, including uppercase, lowercase, a number, and a symbol.</p>
            <p id="password-error" className={errorClass}>{errors.password?.message}</p></div>}
          <div><label htmlFor="employee-department" className="text-sm font-medium">Department</label>
            <select id="employee-department" className={inputClass} aria-invalid={Boolean(errors.departmentId)}
              aria-describedby={errors.departmentId ? 'department-error' : undefined} {...departmentRegistration}
              onChange={event => { void departmentRegistration.onChange(event); setValue('managerId', ''); }}>
              <option value="">Select a department</option>
              {departments.data?.map(department => <option key={department.id} value={department.id}>{department.name}</option>)}
            </select>
            {errors.departmentId && <p id="department-error" className={errorClass}>{errors.departmentId.message}</p>}</div>
          <fieldset><legend className="text-sm font-medium">Roles</legend>
            <div className="mt-2 flex flex-wrap gap-x-4 gap-y-2">
              {employeeRoles.filter(role => availableRoles.data?.some(available => available.name === role)).map(role =>
                <label key={role} className="flex items-center gap-2 text-sm"><input type="checkbox" value={role}
                  aria-describedby={errors.roles ? 'roles-error' : undefined} {...register('roles')} />{role}</label>,
              )}
            </div>{errors.roles && <p id="roles-error" className={errorClass}>{errors.roles.message}</p>}</fieldset>
          {employee && <div><label htmlFor="manager-change" className="text-sm font-medium">Manager assignment</label>
            <select id="manager-change" className={inputClass} {...managerRegistration}
              onChange={event => { void managerRegistration.onChange(event); setValue('managerId', ''); }}>
              <option value="Preserve">Keep current manager</option><option value="Assign">Assign a manager</option><option value="Clear">Clear manager assignment</option>
            </select><p className="mt-1 text-sm text-slate-600">Current manager: {employee.managerName || 'No manager'}</p></div>}
          {(!employee || managerAssignment === 'Assign') && <div><label htmlFor="employee-manager" className="text-sm font-medium">Manager</label>
            <select id="employee-manager" className={inputClass} {...register('managerId')}
              aria-invalid={Boolean(errors.managerId)} aria-describedby="manager-help manager-error">
              <option value="">{employee ? 'Select a manager' : 'No manager'}</option>
              {eligibleManagers.map(manager => <option key={manager.id} value={manager.id}>{manager.fullName}</option>)}
            </select>
            <p id="manager-help" className="mt-1 text-xs text-slate-600">{departmentId && eligibleManagers.length === 0
              ? 'No eligible managers in this department.' : 'Choose a manager in the selected department.'}</p>
            <p id="manager-error" className={errorClass}>{errors.managerId?.message}</p></div>}
          <div className="flex flex-wrap justify-end gap-3 sm:col-span-2">
            <button type="button" onClick={onCancel} className="rounded-lg border border-slate-300 px-4 py-2">Cancel</button>
            <button type="submit" disabled={busy || choicesUnavailable}
              className="rounded-lg bg-slate-900 px-4 py-2 font-semibold text-white disabled:opacity-50">{busy ? 'Saving...' : 'Save Employee'}</button>
          </div>
        </fieldset>
      </form>
    </section>
  );
}
