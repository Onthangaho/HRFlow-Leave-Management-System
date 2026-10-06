import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';
import type { Employee, EmployeeFormValues, EmployeeWriteResult } from './types';

const employeesQueryKey = (userId: string) => ['employees', userId] as const;
const departmentsQueryKey = (userId: string) => ['departments', userId] as const;
const rolesQueryKey = (userId: string) => ['roles', userId] as const;

const createEmployee = async (values: EmployeeFormValues): Promise<EmployeeWriteResult> => {
  const response = await authHttpClient.post<EmployeeWriteResult>('/employees', {
    fullName: values.fullName, email: values.email, password: values.password,
    departmentId: values.departmentId, roles: values.roles, managerId: values.managerId || null,
  });
  return response.data;
};

const updateEmployee = async (
  { id, expectedVersion, ...values }: { id: string; expectedVersion: string } & EmployeeFormValues,
): Promise<EmployeeWriteResult> => {
  const response = await authHttpClient.put<EmployeeWriteResult>(`/employees/${id}`, {
    fullName: values.fullName, email: values.email, departmentId: values.departmentId,
    roles: values.roles, expectedVersion, managerAssignment: values.managerAssignment,
    ...(values.managerAssignment === 'Assign' ? { managerId: values.managerId } : {}),
  });
  return response.data;
};

/** Reads only the signed-in account's directory cache and cancels old requests when the session changes. */
export function useEmployees() {
  const { user } = useAuth();
  return useQuery<Employee[], Error>({
    queryKey: employeesQueryKey(user?.id ?? ''),
    queryFn: async ({ signal }) => (await authHttpClient.get<Employee[]>('/employees', { signal })).data,
    enabled: Boolean(user?.id),
  });
}

/** Loads department choices without implementing department mutation workflows. */
export function useDepartments() {
  const { user } = useAuth();
  return useQuery<{ id: string; name: string }[], Error>({
    queryKey: departmentsQueryKey(user?.id ?? ''),
    queryFn: async ({ signal }) => (await authHttpClient.get('/departments', { signal })).data,
    enabled: Boolean(user?.id),
  });
}

/** Uses server-supported capabilities rather than a single-role edit that can drop combined membership. */
export function useRoles() {
  const { user } = useAuth();
  return useQuery<{ name: string }[], Error>({
    queryKey: rolesQueryKey(user?.id ?? ''),
    queryFn: async ({ signal }) => (await authHttpClient.get('/roles', { signal })).data,
    enabled: Boolean(user?.id),
  });
}

function useRefreshManagementQueries() {
  const client = useQueryClient();
  const { user } = useAuth();
  return async () => {
    if (!user?.id) return;
    await Promise.all([
      client.invalidateQueries({ queryKey: employeesQueryKey(user.id) }),
      client.invalidateQueries({ queryKey: ['pending-leave-requests', user.id] }),
      client.invalidateQueries({ queryKey: ['organisation-pending-leave-requests', user.id] }),
    ]);
  };
}

/** Waits for account-scoped invalidation so a created account appears without reloading the page. */
export function useCreateEmployee() {
  const refresh = useRefreshManagementQueries();
  return useMutation<EmployeeWriteResult, Error, EmployeeFormValues>({
    mutationFn: createEmployee, onSuccess: refresh,
  });
}

/** Sends the original form version and refreshes reporting/management caches only for its account. */
export function useUpdateEmployee() {
  const refresh = useRefreshManagementQueries();
  return useMutation<EmployeeWriteResult, Error, { id: string; expectedVersion: string } & EmployeeFormValues>({
    mutationFn: updateEmployee, onSuccess: refresh,
  });
}
