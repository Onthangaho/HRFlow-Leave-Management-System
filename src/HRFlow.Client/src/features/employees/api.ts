import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import type { Employee, EmployeeFormValues } from './types';

const API_BASE_URL = 'http://localhost:5228/api/v1';

const getEmployees = async (): Promise<Employee[]> => {
  const response = await fetch(`${API_BASE_URL}/employees`);
  if (!response.ok) {
    throw new Error('Failed to fetch employees');
  }
  return response.json();
};

const createEmployee = async (employee: EmployeeFormValues): Promise<Employee> => {
  const response = await fetch(`${API_BASE_URL}/employees`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(employee),
  });
  if (!response.ok) {
    const errorData = await response.json();
    throw new Error(errorData.detail || 'Failed to create employee');
  }
  return response.json();
};

const updateEmployee = async ({ id, ...employee }: { id: string } & EmployeeFormValues): Promise<Employee> => {
  const response = await fetch(`${API_BASE_URL}/employees/${id}`, {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(employee),
  });
  if (!response.ok) {
    const errorData = await response.json();
    throw new Error(errorData.detail || 'Failed to update employee');
  }
  return response.json();
};

export const useEmployees = () => {
  return useQuery<Employee[], Error>({
    queryKey: ['employees'],
    queryFn: getEmployees,
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