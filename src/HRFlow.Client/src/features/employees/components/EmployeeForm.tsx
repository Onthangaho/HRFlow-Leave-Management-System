import { ScheduleWorkspace } from './ScheduleWorkspace';
import { useAuth } from '../../auth/hooks/useAuth';
import { useEffect } from 'react';
import { useForm, useWatch } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { isAxiosError } from 'axios';
import { useCreateEmployee, useUpdateEmployee, useDepartments, useRoles } from '../api';
import { employeeRoles, type Employee, type EmployeeFormValues } from '../types';

const schema = z.object({
  confirmEmploymentFacts: z.boolean(), employeeNumber: z.string(), employmentStartDate: z.string(),
  fullName: z.string().trim().min(1, 'Full name is required').max(200),
  email: z.string().trim().email('Enter a valid email address').max(256),
  departmentId: z.string().min(1, 'Select a department'),
  roles: z.array(z.enum(employeeRoles)).min(1, 'Select at least one role'),
  managerAssignment: z.enum(['Preserve', 'Assign', 'Clear']),
  managerId: z.string(),
});

interface EmployeeFormProps {
  employee: Employee | null;
  employees: Employee[];
  onSuccess: (deliveryState?: string | null) => void;
  onCancel: () => void;
  onReload: () => Promise<void>;
  reloadError: string;
}

const inputClass = 'ui-input';
const errorClass = 'mt-1 text-sm text-rose-700';

/** Preserves the original version, roles, and reporting intent until the user explicitly reloads or saves. */
export function EmployeeForm({ employee, employees, onSuccess, onCancel, onReload, reloadError }: EmployeeFormProps) {
  const { sessionVersion, getSessionVersion } = useAuth();
  const isEditing = Boolean(employee);
  const departments = useDepartments();
  const availableRoles = useRoles();
  const create = useCreateEmployee();
  const update = useUpdateEmployee();
  const formSchema = schema.superRefine((values, context) => {
    if (!isEditing || values.confirmEmploymentFacts) {
      if (!/^[A-Za-z0-9][A-Za-z0-9_-]{0,31}$/.test(values.employeeNumber.trim())) context.addIssue({ code: 'custom', path: ['employeeNumber'], message: 'Use 1-32 ASCII letters, digits, hyphens or underscores, starting with a letter or digit.' });
      const date = values.employmentStartDate;
      if (!/^\d{4}-\d{2}-\d{2}$/.test(date) || date < '1900-01-01' || date > '2100-12-31' || Number.isNaN(Date.parse(date)) || new Date(date).toISOString().slice(0,10) !== date) context.addIssue({code:'custom',path:['employmentStartDate'],message:'Enter a valid confirmed date between 1900 and 2100.'});
    }
    if (isEditing && values.managerAssignment === 'Assign' && !values.managerId) {
      context.addIssue({ code: 'custom', path: ['managerId'], message: 'Select a manager to assign.' });
    }
  });
  const { register, handleSubmit, reset, control, setValue, formState: { errors, isSubmitting } } = useForm<EmployeeFormValues>({
    resolver: zodResolver(formSchema),
    defaultValues: { confirmEmploymentFacts: false, employeeNumber: '', employmentStartDate: '', fullName: '', email: '', departmentId: '', roles: [], managerAssignment: 'Clear', managerId: '' },
  });
  const departmentId = useWatch({ control, name: 'departmentId' });
  const confirmFacts = useWatch({ control, name: 'confirmEmploymentFacts' });
  const managerAssignment = useWatch({ control, name: 'managerAssignment' });
  const eligibleManagers = employees.filter(candidate =>
    candidate.isActive && !candidate.requiresActivation && candidate.id !== employee?.id && candidate.departmentId === departmentId && candidate.roles.includes('Manager'),
  );
  const mutation = employee ? update : create;
  const busy = mutation.isPending || isSubmitting;
  const choicesUnavailable = departments.isPending || availableRoles.isPending ||
    Boolean(departments.error || availableRoles.error) || !departments.data?.length || !availableRoles.data?.length;

  useEffect(() => {
    reset(employee ? {
      confirmEmploymentFacts: false, employeeNumber: employee.employeeNumber ?? '', employmentStartDate: employee.employmentStartDate ?? '',
      fullName: employee.fullName, email: employee.email, departmentId: employee.departmentId,
      roles: [...employee.roles], managerAssignment: 'Preserve', managerId: '',
    } : { confirmEmploymentFacts: false, employeeNumber: '', employmentStartDate: '', fullName: '', email: '', departmentId: '', roles: [], managerAssignment: 'Clear', managerId: '' });
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
      const result = employee ? await update.mutateAsync({ ...values, id: employee.id, expectedVersion: employee.version }) : await create.mutateAsync(values);
      if (getSessionVersion() === sessionVersion) onSuccess(result.invitationDeliveryState);
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
          <div className="sm:col-span-2 space-y-3">
            <h3 className="font-semibold">Confirmed employment facts</h3>
            {employee && <><p>Number: {employee.employeeNumber ?? 'Unknown'}; start date: {employee.employmentStartDate ?? 'Unknown'}. Omission preserves these facts.</p><label className="flex gap-2"><input type="checkbox" {...register('confirmEmploymentFacts')} />Confirm or revise employment facts</label></>}
            {(!employee || confirmFacts) && <div className="grid gap-4 sm:grid-cols-2"><label className="ui-label">Employee number<input className="ui-input" maxLength={32} {...register('employeeNumber')} aria-describedby="employment-help" />{errors.employeeNumber && <span role="alert">{errors.employeeNumber.message}</span>}</label><label className="ui-label">Confirmed employment start date<input className="ui-input" type="date" min="1900-01-01" max="2100-12-31" {...register('employmentStartDate')} aria-describedby="employment-help" />{errors.employmentStartDate && <span role="alert">{errors.employmentStartDate.message}</span>}</label></div>}
            <p id="employment-help" className="text-sm">Confirm from employment records, not account creation. Future start dates do not delay activated account access. Leave still uses inclusive calendar days.</p>
          </div>
          <div><label htmlFor="employee-full-name" className="text-sm font-medium">Full name</label>
            <input id="employee-full-name" autoComplete="name" className={inputClass} aria-invalid={Boolean(errors.fullName)}
              aria-describedby={errors.fullName ? 'full-name-error' : undefined} {...register('fullName')} />
            {errors.fullName && <p id="full-name-error" className={errorClass}>{errors.fullName.message}</p>}</div>
          <div><label htmlFor="employee-email" className="text-sm font-medium">Email</label>
            <input id="employee-email" type="email" autoComplete="off" className={inputClass} aria-invalid={Boolean(errors.email)}
              aria-describedby={errors.email ? 'email-error' : undefined} {...register('email')} />
            {errors.email && <p id="email-error" className={errorClass}>{errors.email.message}</p>}</div>
          {!employee && <p className="sm:col-span-2 text-sm text-slate-600">The employee sets their own password using a private invitation. They cannot sign in until activation. Development uses private local pickup, not email delivery.</p>}
          <div><label htmlFor="employee-department" className="text-sm font-medium">Department</label>
            <select id="employee-department" className={inputClass} aria-invalid={Boolean(errors.departmentId)}
              aria-describedby={errors.departmentId ? 'department-error' : undefined} {...departmentRegistration} value={departmentId}
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
            <button type="button" onClick={onCancel} className="ui-secondary">Cancel</button>
            <button type="submit" disabled={busy || choicesUnavailable}
              className="ui-primary">{busy ? 'Saving...' : 'Save Employee'}</button>
          </div>
        </fieldset>
      </form>
      {employee && <ScheduleWorkspace employeeId={employee.id} />}
    </section>
  );
}
