import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';
import type { Employee, EmployeeFormValues, EmployeeWriteResult, EmployeeDeactivationResult } from './types';

const employeesQueryKey = (userId: string) => ['employees', userId] as const;
const departmentsQueryKey = (userId: string) => ['departments', userId] as const;
const rolesQueryKey = (userId: string) => ['roles', userId] as const;

const createEmployee = async (values: EmployeeFormValues): Promise<EmployeeWriteResult> => {
  const response = await authHttpClient.post<EmployeeWriteResult>('/employees', {
    fullName: values.fullName, email: values.email,
    departmentId: values.departmentId, roles: values.roles, managerId: values.managerId || null,
    employeeNumber: values.employeeNumber, employmentStartDate: values.employmentStartDate,
  }, { skipAuthReplay: true });
  return response.data;
};

const updateEmployee = async (
  { id, expectedVersion, ...values }: { id: string; expectedVersion: string } & EmployeeFormValues,
): Promise<EmployeeWriteResult> => {
  const response = await authHttpClient.put<EmployeeWriteResult>(`/employees/${id}`, {
    fullName: values.fullName, email: values.email, departmentId: values.departmentId,
    confirmEmploymentFacts: values.confirmEmploymentFacts,
    ...(values.confirmEmploymentFacts ? { employeeNumber: values.employeeNumber, employmentStartDate: values.employmentStartDate } : {}),
    roles: values.roles, expectedVersion, managerAssignment: values.managerAssignment,
    ...(values.managerAssignment === 'Assign' ? { managerId: values.managerId } : {}),
  }, { skipAuthReplay: true });
  return response.data;
};

/** Applied filters are part of account/session keys, never labels over previous-page data. */
export interface DirectoryFilter { search: string; status: string; departmentId: string; attention: string; reportsTo: string; sort: string; page: number; pageSize: number }
/** Server count, page and as-of metadata belong to the same authorised snapshot. */
export interface DirectoryPage { items: Employee[]; totalCount: number; page: number; pageSize: number; asOfUtc: string }
/** Minimal current Manager choice; a lookup never grants permission to commit a stale assignment. */
export interface ManagerChoice { id: string; fullName: string }
interface ManagerPage { items: ManagerChoice[]; totalCount: number; page: number; pageSize: number; assignedManager: ManagerChoice | null }
/** Server authorization/count/page share a snapshot; requests and caches belong to this session. */
export function useEmployees(filter: DirectoryFilter) {
  const { user, sessionVersion } = useAuth();
  return useQuery({ queryKey: [...employeesQueryKey(user?.id ?? ''), sessionVersion, 'directory', filter],
    queryFn: async ({ signal }) => (await authHttpClient.get<DirectoryPage>('/employees', { signal,
      params: { ...filter, departmentId: filter.departmentId || undefined, reportsTo: filter.reportsTo || undefined } })).data,
    enabled: Boolean(user?.roles.includes('HR Administrator')), retry: false, refetchOnMount: 'always' });
}
/** Manager search is independent of the current directory page and includes an assigned snapshot. */
export function useManagers(departmentId: string, employeeId: string | undefined, search: string, page: number) {
  const { user, sessionVersion } = useAuth();
  return useQuery({ queryKey: [...employeesQueryKey(user?.id ?? ''), sessionVersion, 'managers', departmentId, employeeId, search, page],
    queryFn: async ({ signal }) => (await authHttpClient.get<ManagerPage>('/employees/managers', { signal,
      params: { departmentId, employeeId, search, page } })).data,
    enabled: Boolean(user?.roles.includes('HR Administrator') && departmentId), retry: false, refetchOnMount: 'always' });
}

/** Loads department choices without implementing department mutation workflows. */
export function useDepartments() {
  const { user, sessionVersion } = useAuth();
  return useQuery<{ id: string; name: string }[], Error>({
    queryKey: [...departmentsQueryKey(user?.id ?? ''), sessionVersion],
    queryFn: async ({ signal }) => (await authHttpClient.get('/departments', { signal })).data,
    enabled: Boolean(user?.id),
  });
}

/** Uses server-supported capabilities rather than a single-role edit that can drop combined membership. */
export function useRoles() {
  const { user, sessionVersion } = useAuth();
  return useQuery<{ name: string }[], Error>({
    queryKey: [...rolesQueryKey(user?.id ?? ''), sessionVersion],
    queryFn: async ({ signal }) => (await authHttpClient.get('/roles', { signal })).data,
    enabled: Boolean(user?.id),
  });
}

function useRefreshManagementQueries() {
  const client = useQueryClient();
  const { user, sessionVersion, getSessionVersion } = useAuth();
  return async () => {
    if (!user?.id || getSessionVersion() !== sessionVersion) return;
    await Promise.all([
      client.invalidateQueries({ queryKey: [...employeesQueryKey(user.id), sessionVersion] }),
      client.invalidateQueries({ queryKey: ['pending-leave-requests', user.id] }),
      client.invalidateQueries({ queryKey: ['organisation-pending-leave-requests', user.id] }),
      client.invalidateQueries({ queryKey: ['hr-workspace', user.id, sessionVersion] }),
      client.invalidateQueries({ queryKey: ['manager-attention', user.id, sessionVersion] }),
      client.invalidateQueries({ queryKey: ['team-leave-summary', user.id, sessionVersion] }),
      client.invalidateQueries({ queryKey: ['department-leave-report', user.id] }),
      client.invalidateQueries({ queryKey: ['leave-request-timeline', user.id] }),
    ]);
  };
}

/** Waits for account-scoped invalidation so a created account appears without reloading the page. */
export function useCreateEmployee() {
  const refresh = useRefreshManagementQueries();
  return useMutation<EmployeeWriteResult, Error, EmployeeFormValues>({
    mutationFn: createEmployee, onSuccess: refresh, retry: false,
  });
}

/** Sends the original form version and refreshes reporting/management caches only for its account. */
export function useUpdateEmployee() {
  const refresh = useRefreshManagementQueries();
  return useMutation<EmployeeWriteResult, Error, { id: string; expectedVersion: string } & EmployeeFormValues>({
    mutationFn: updateEmployee, onSuccess: refresh,
  });
}

/** Sends one versioned lifecycle decision; neither Axios nor the mutation retries it. */
export function useDeactivateEmployee() {
  const client = useQueryClient();
  const { user, sessionVersion, getSessionVersion } = useAuth();
  return useMutation<EmployeeDeactivationResult, Error, { employee: Employee; reason: string }>({
    retry: false,
    mutationFn: async ({ employee, reason }) => {
      if (!user || getSessionVersion() !== sessionVersion) throw new Error('The session has changed.');
      return (await authHttpClient.patch<EmployeeDeactivationResult>(`/employees/${employee.id}/deactivate`, {
        expectedVersion: employee.version, reason: reason.trim(),
      }, { skipAuthReplay: true })).data;
    },
    onSuccess: async (_, { employee }) => {
      if (!user || getSessionVersion() !== sessionVersion || employee.identityUserId === user.id) return;
      await client.invalidateQueries({ queryKey: ['hr-workspace', user.id, sessionVersion] });
      await client.invalidateQueries({ queryKey: ['manager-attention', user.id, sessionVersion] });
      await client.invalidateQueries({ queryKey: ['employees', user.id, sessionVersion] });
      await Promise.all(['leave-balances', 'employee-leave-history', 'pending-leave-requests',
        'organisation-pending-leave-requests', 'leave-types', 'managed-leave-types', 'leave-policies',
        'departments', 'roles', 'team-leave-summary', 'department-leave-report', 'report-departments', 'leave-request-timeline'].map(key => client.invalidateQueries({ queryKey: [key, user.id] })));
    },
  });
}
