import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';
import type { Employee, EmployeeFormValues } from './types';

const employeesQueryKey = (userId: string) => ['employees', userId] as const;
const departmentsQueryKey = (userId: string) => ['departments', userId] as const;
const rolesQueryKey = (userId: string) => ['roles', userId] as const;

const getEmployees = async (): Promise<Employee[]> => {
  const response = await authHttpClient.get('/employees');
  return response.data;
};

const getDepartments = async (): Promise<{ id: string, name: string }[]> => {
  const response = await authHttpClient.get('/departments');
  return response.data;
};

const getRoles = async (): Promise<{ name: string }[]> => {
  const response = await authHttpClient.get('/roles');
  return response.data;
};

const createEmployee = async (employee: EmployeeFormValues): Promise<Employee> => {
  const response = await authHttpClient.post('/employees', employee);
  return response.data;
};

const updateEmployee = async ({ id, ...employee }: { id: string } & EmployeeFormValues): Promise<Employee> => {
  const payload = {
    employeeId: id,
    fullName: employee.fullName,
    email: employee.email,
    departmentId: employee.departmentId,
    managerId: null,
    roleName: employee.roleName,
  };
  const response = await authHttpClient.put(`/employees/${id}`, payload);
  return response.data;
};

export const useEmployees = () => {
  const { user } = useAuth();

  return useQuery<Employee[], Error>({
    queryKey: employeesQueryKey(user?.id ?? ''),
    queryFn: getEmployees,
    enabled: Boolean(user?.id),
  });
};

export const useDepartments = () => {
  const { user } = useAuth();

  return useQuery<{ id: string, name: string }[], Error>({
    queryKey: departmentsQueryKey(user?.id ?? ''),
    queryFn: getDepartments,
    enabled: Boolean(user?.id),
  });
};

export const useRoles = () => {
  const { user } = useAuth();

  return useQuery<{ name: string }[], Error>({
    queryKey: rolesQueryKey(user?.id ?? ''),
    queryFn: getRoles,
    enabled: Boolean(user?.id),
  });
};

export const useCreateEmployee = () => {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  return useMutation<Employee, Error, EmployeeFormValues>({
    mutationFn: createEmployee,
    onSuccess: () => {
      if (user?.id) {
        queryClient.invalidateQueries({ queryKey: employeesQueryKey(user.id) });
      }
    },
  });
};

export const useUpdateEmployee = () => {
  const queryClient = useQueryClient();
  const { user } = useAuth();
  return useMutation<Employee, Error, { id: string } & EmployeeFormValues>({
    mutationFn: updateEmployee,
    onSuccess: () => {
      if (user?.id) {
        queryClient.invalidateQueries({ queryKey: employeesQueryKey(user.id) });
      }
    },
  });
};
