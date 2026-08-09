import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import type { EmployeeFormValues } from '../types';

const employeeSchema = z.object({
  fullName: z.string().min(1, 'Full name is required'),
  email: z.string().email('Invalid email address'),
  password: z.string().optional(),
  departmentId: z.string().uuid('Invalid department'),
  roleName: z.string().min(1, 'Role is required'),
});

interface EmployeeFormProps {
  onSubmit: (data: EmployeeFormValues) => void;
  initialValues?: EmployeeFormValues;
  isSubmitting: boolean;
}

export const EmployeeForm = ({ onSubmit, initialValues, isSubmitting }: EmployeeFormProps) => {
  const { register, handleSubmit, formState: { errors } } = useForm<EmployeeFormValues>({
    resolver: zodResolver(employeeSchema),
    defaultValues: initialValues,
  });

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <div>
        <label htmlFor="fullName">Full Name</label>
        <input id="fullName" {...register('fullName')} />
        {errors.fullName && <p>{errors.fullName.message}</p>}
      </div>
      <div>
        <label htmlFor="email">Email</label>
        <input id="email" {...register('email')} />
        {errors.email && <p>{errors.email.message}</p>}
      </div>
      <div>
        <label htmlFor="password">Password</label>
        <input id="password" type="password" {...register('password')} />
        {errors.password && <p>{errors.password.message}</p>}
      </div>
      <div>
        <label htmlFor="departmentId">Department</label>
        <input id="departmentId" {...register('departmentId')} />
        {errors.departmentId && <p>{errors.departmentId.message}</p>}
      </div>
      <div>
        <label htmlFor="roleName">Role</label>
        <input id="roleName" {...register('roleName')} />
        {errors.roleName && <p>{errors.roleName.message}</p>}
      </div>
      <button type="submit" disabled={isSubmitting}>
        {isSubmitting ? 'Submitting...' : 'Submit'}
      </button>
    </form>
  );
};