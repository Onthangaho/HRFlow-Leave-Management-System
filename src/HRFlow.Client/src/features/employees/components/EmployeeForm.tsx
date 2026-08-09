import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useEffect } from 'react';
import { isAxiosError } from 'axios';
import { useCreateEmployee, useUpdateEmployee, useDepartments, useRoles } from '../api';
import type { Employee, EmployeeFormValues } from '../types';

const employeeSchema = z.object({
  fullName: z.string().min(1, 'Full name is required'),
  email: z.string().email('Invalid email address'),
  password: z.string().min(1, 'Password is required'),
  departmentId: z.string().min(1, 'Department is required'),
  roleName: z.string().min(1, 'Role is required'),
});

const employeeEditSchema = employeeSchema.omit({ password: true });

interface EmployeeFormProps {
  employee?: Employee | null;
  onSuccess: () => void;
  onCancel: () => void;
}

export function EmployeeForm({ employee, onSuccess, onCancel }: EmployeeFormProps) {
  const isEditing = !!employee;

  const { data: departments, isLoading: isLoadingDepartments } = useDepartments();
  const { data: roles, isLoading: isLoadingRoles } = useRoles();

  const createEmployee = useCreateEmployee();
  const updateEmployee = useUpdateEmployee();

  const { register, handleSubmit, formState: { errors }, setError, reset } = useForm<EmployeeFormValues>({
    resolver: zodResolver(isEditing ? employeeEditSchema : employeeSchema),
  });

  useEffect(() => {
    if (employee) {
      const department = departments?.find(d => d.name === employee.departmentName);
      reset({
        fullName: employee.fullName,
        email: employee.email,
        departmentId: department?.id || '',
        roleName: employee.roleName,
      });
    } else {
      reset({
        fullName: '',
        email: '',
        password: '',
        departmentId: '',
        roleName: '',
      });
    }
  }, [employee, reset, departments]);

  const mutation = isEditing ? updateEmployee : createEmployee;

  useEffect(() => {
    if (isAxiosError(mutation.error)) {
      if (mutation.error.response?.status === 409) {
        setError('email', { type: 'manual', message: 'Email already in use' });
      }
    }
  }, [mutation.error, setError]);

  const onSubmit = handleSubmit(async (values) => {
    if (isEditing && employee) {
      await updateEmployee.mutateAsync({ ...values, id: employee.id });
    } else {
      await createEmployee.mutateAsync(values);
    }
    onSuccess();
  });

  return (
    <div className="rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
      <h2 className="text-2xl font-bold text-slate-900">{isEditing ? 'Edit Employee' : 'Create New Employee'}</h2>
      <form className="mt-6 grid grid-cols-1 gap-y-6 sm:grid-cols-2 sm:gap-x-8" onSubmit={onSubmit}>
        <div className="sm:col-span-1">
          <label className="block text-sm font-medium text-slate-700" htmlFor="fullName">
            Full Name
          </label>
          <input
            id="fullName"
            type="text"
            className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-slate-900 outline-none focus:border-slate-500"
            {...register('fullName')}
          />
          {errors.fullName && <p className="mt-1 text-sm text-rose-600">{errors.fullName.message}</p>}
        </div>

        <div className="sm:col-span-1">
          <label className="block text-sm font-medium text-slate-700" htmlFor="email">
            Email
          </label>
          <input
            id="email"
            type="email"
            className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-slate-900 outline-none focus:border-slate-500"
            {...register('email')}
          />
          {errors.email && <p className="mt-1 text-sm text-rose-600">{errors.email.message}</p>}
        </div>

        {!isEditing && (
          <div className="sm:col-span-2">
            <label className="block text-sm font-medium text-slate-700" htmlFor="password">
              Password
            </label>
            <input
              id="password"
              type="password"
              className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-slate-900 outline-none focus:border-slate-500"
              {...register('password')}
            />
            {errors.password && <p className="mt-1 text-sm text-rose-600">{errors.password.message}</p>}
          </div>
        )}

        <div className="sm:col-span-1">
          <label className="block text-sm font-medium text-slate-700" htmlFor="departmentId">
            Department
          </label>
          <select
            id="departmentId"
            className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-slate-900 outline-none focus:border-slate-500"
            {...register('departmentId')}
            disabled={isLoadingDepartments}
          >
            <option value="">Select Department</option>
            {departments?.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}
          </select>
          {errors.departmentId && <p className="mt-1 text-sm text-rose-600">{errors.departmentId.message}</p>}
        </div>

        <div className="sm:col-span-1">
          <label className="block text-sm font-medium text-slate-700" htmlFor="roleName">
            Role
          </label>
          <select
            id="roleName"
            className="mt-1 w-full rounded-lg border border-slate-300 px-3 py-2 text-slate-900 outline-none focus:border-slate-500"
            {...register('roleName')}
            disabled={isLoadingRoles}
          >
            <option value="">Select Role</option>
            {roles?.map(r => <option key={r.name} value={r.name}>{r.name}</option>)}
          </select>
          {errors.roleName && <p className="mt-1 text-sm text-rose-600">{errors.roleName.message}</p>}
        </div>

        <div className="sm:col-span-2 flex justify-end gap-3">
          <button
            type="button"
            onClick={onCancel}
            className="rounded-lg border border-slate-300 px-4 py-2 text-sm font-medium text-slate-900 hover:bg-slate-100"
          >
            Cancel
          </button>
          <button
            type="submit"
            disabled={mutation.isPending}
            className="rounded-lg bg-slate-900 px-4 py-2 font-semibold text-white transition hover:bg-slate-700 disabled:cursor-not-allowed disabled:bg-slate-400"
          >
            {mutation.isPending ? 'Saving...' : 'Save Employee'}
          </button>
        </div>
      </form>
    </div>
  );
}