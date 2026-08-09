import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { authHttpClient } from '../auth/api';
import type { Employee, EmployeeFormValues } from './types';

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
  const response = await authHttpClient.put(`/employees/${id}`, employee);
  return response.data;
};

export const useEmployees = () => {
  return useQuery<Employee[], Error>({
    queryKey: ['employees'],
    queryFn: getEmployees,
  });
};

export const useDepartments = () => {
  return useQuery<{ id: string, name: string }[], Error>({
    queryKey: ['departments'],
    queryFn: getDepartments,
  });
};

export const useRoles = () => {
  return useQuery<{ name: string }[], Error>({
    queryKey: ['roles'],
    queryFn: getRoles,
  });
};

export const useCreateEmployee = () => {
  const queryClient = useQueryClient();
  return useMutation<Employee, Error, EmployeeFormValues>({
    mutationFn: createEmployee,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['employees'] });
    },
  });
};

export const useUpdateEmployee = () => {
  const queryClient = useQueryClient();
  return useMutation<Employee, Error, { id: string } & EmployeeFormValues>({
    mutationFn: updateEmployee,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['employees'] });
    },
  });
};