import { CancelledError, useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';
import type { EmployeeLeaveRequest, LeaveBalance, PendingLeaveRequest, LeaveDecision } from './types';

const pendingLeaveRequestsQueryKey = (userId: string) =>
  ['pending-leave-requests', userId] as const;
const organisationPendingLeaveRequestsQueryKey = (userId: string) =>
  ['organisation-pending-leave-requests', userId] as const;
const leaveBalancesQueryKey = (userId: string) => ['leave-balances', userId] as const;
const employeeLeaveHistoryQueryKey = (userId: string) =>
  ['employee-leave-history', userId] as const;

/** Applied queue values never derive authorization from selected IDs. */
export interface QueueFilters { employeeId: string; employeeName: string; leaveTypeId: string; start: string; end: string; page: number; pageSize: number }
/** Count and rows share a current authorized snapshot. */
export interface QueuePage { items: PendingLeaveRequest[]; totalCount: number; page: number; pageSize: number; totalPages: number; capturedAtUtc: string }
/** No previous account's filters or rows seed a new workspace. */
export const queueDefaults: QueueFilters = { employeeId: '', employeeName: '', leaveTypeId: '', start: '', end: '', page: 1, pageSize: 25 };
async function getPendingLeaveRequests(filters: QueueFilters, signal: AbortSignal): Promise<QueuePage> {
  const { employeeName: _label, ...params } = filters; void _label;
  return (await authHttpClient.get<QueuePage>('/leave-requests', { params: { status: 'Pending', ...Object.fromEntries(Object.entries(params).filter(([, value]) => value !== '')) }, signal })).data;
}
/** Rechecks the original dialog ID independently of whichever page/filter is now displayed. */
export async function checkQueueDecision(requestId: string, signal: AbortSignal): Promise<boolean> {
  const result = await authHttpClient.get<QueuePage>('/leave-requests', { params: { requestId, pageSize: 1 }, signal });
  return result.data.items.some(item => item.id === requestId);
}
/** Explicit search returns at most 25 eligible Pending owners across the entire queue. */
export function useQueuePeople(search: string) {
  const { user, sessionVersion, getSessionVersion } = useAuth();
  return useQuery({ queryKey: ['queue-people', user?.id, sessionVersion, search], enabled: !!user?.roles.includes('Manager'), retry: false,
    queryFn: async ({ signal }) => { const data = (await authHttpClient.get<{ id: string; name: string }[]>('/leave-requests/queue-people', { params: { search }, signal })).data; if (getSessionVersion() !== sessionVersion) throw new CancelledError(); return data; } });
}
/** Uses the existing type selector contract, not type values from only the displayed queue page. */
export function useQueueTypes() {
  const { user, sessionVersion } = useAuth();
  return useQuery({ queryKey: ['leave-types', user?.id ?? '', sessionVersion], enabled: !!user?.roles.includes('Manager'), retry: false,
    queryFn: async ({ signal }) => (await authHttpClient.get<import('../leave-configuration/types').TypeSelection[]>('/leave-types', { signal })).data });
}

async function approveLeaveRequest({ requestId, decisionNote }: LeaveDecision): Promise<void> {
  await authHttpClient.post(`/leave-requests/${requestId}/approve`, { decisionNote }, { skipAuthReplay: true });
}

/** Applied HR filters narrow a read-only operational view, never a decision scope. */
export interface MonitoringFilters { search: string; departmentId: string; leaveTypeId: string; start: string; end: string; page: number; pageSize: number }
/** Monitoring shares the bounded row envelope, not Manager authorization or actions. */
export type MonitoringPage = QueuePage;
/** A replacement session starts with its own fresh filter state. */
export const monitoringDefaults: MonitoringFilters = { search: '', departmentId: '', leaveTypeId: '', start: '', end: '', page: 1, pageSize: 25 };

async function rejectLeaveRequest({ requestId, decisionNote }: LeaveDecision): Promise<void> {
  await authHttpClient.post(`/leave-requests/${requestId}/reject`, { decisionNote }, { skipAuthReplay: true });
}

async function getLeaveBalances(signal: AbortSignal): Promise<LeaveBalance[]> {
  const response = await authHttpClient.get<LeaveBalance[]>('/leave-requests/balances', { signal });
  return response.data;
}

/** Applied values form part of the account/session cache identity. Empty strings mean no filter. */
export interface HistoryFilters { status: string; leaveTypeId: string; start: string; end: string; page: number; pageSize: number }
/** The server clamps unavailable pages and captures count/rows in the same snapshot. */
export interface HistoryPage { items: EmployeeLeaveRequest[]; totalCount: number; page: number; pageSize: number; totalPages: number; capturedAtUtc: string }
/** Fresh filter defaults are never inferred from a previous account's history. */
export const historyDefaults: HistoryFilters = { status: '', leaveTypeId: '', start: '', end: '', page: 1, pageSize: 25 };
async function getEmployeeLeaveHistory(filters: HistoryFilters, signal: AbortSignal): Promise<HistoryPage> {
  const response = await authHttpClient.get<HistoryPage>('/leave-requests/history', {
    params: Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== '')), signal,
  });
  return response.data;
}

async function cancelLeaveRequest(leaveRequestId: string): Promise<void> {
  await authHttpClient.post(`/leave-requests/${leaveRequestId}/cancel`, undefined, { skipAuthReplay: true });
}

/**
 * Retrieves the review queue from the server because its role-scoped contents can change outside
 * this page as other reviewers process requests.
 */
export function usePendingLeaveRequests(filters: QueueFilters) {
  const { user, sessionVersion, getSessionVersion } = useAuth();

  return useQuery<QueuePage, Error>({
    queryKey: [...pendingLeaveRequestsQueryKey(user?.id ?? ''), sessionVersion, filters],
    queryFn: async ({ signal }) => { const data = await getPendingLeaveRequests(filters, signal); if (getSessionVersion() !== sessionVersion) throw new CancelledError(); return data; },
    enabled: Boolean(user?.roles.includes('Manager')),
    refetchOnMount: 'always', retry: false,
  });
}

/** Retrieves HR's read-only organisation-wide pending-request monitoring data. */
export function useOrganisationPendingLeaveRequests(filters: MonitoringFilters) {
  const { user, sessionVersion, getSessionVersion } = useAuth();

  return useQuery<MonitoringPage, Error>({
    queryKey: [...organisationPendingLeaveRequestsQueryKey(user?.id ?? ''), sessionVersion, filters],
    queryFn: async ({ signal }) => {
      const data = (await authHttpClient.get<MonitoringPage>('/leave-requests/monitoring/pending', { signal,
        params: Object.fromEntries(Object.entries(filters).filter(([, value]) => value !== '')) })).data;
      if (getSessionVersion() !== sessionVersion) throw new CancelledError();
      return data;
    },
    enabled: Boolean(user?.roles.includes('HR Administrator')), retry: false, refetchOnMount: 'always',
  });
}

/** Uses the existing type catalogue, not just the types visible on the current monitoring page. */
export function useMonitoringTypes() {
  const { user, sessionVersion } = useAuth();
  return useQuery({ queryKey: ['leave-types', user?.id ?? '', sessionVersion], enabled: Boolean(user?.roles.includes('HR Administrator')), retry: false,
    queryFn: async ({ signal }) => (await authHttpClient.get<import('../leave-configuration/types').TypeSelection[]>('/leave-types', { signal })).data });
}

/**
 * Refreshes the role-scoped queue after approval so a processed request disappears without reload.
 */
export function useApproveLeaveRequest() {
  const queryClient = useQueryClient();
  const { user, sessionVersion, getSessionVersion } = useAuth();

  return useMutation<void, Error, LeaveDecision>({
    retry: false,
    mutationFn: approveLeaveRequest,
    onSuccess: async () => {
      if (!user?.id || getSessionVersion() !== sessionVersion) return;
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['hr-workspace', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: [...organisationPendingLeaveRequestsQueryKey(user.id), sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: ['manager-attention', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: ['personal-leave-dashboard', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: [...pendingLeaveRequestsQueryKey(user.id), sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: ['queue-people', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: ['team-leave-summary', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: ['department-leave-report', user.id] }),
        queryClient.invalidateQueries({ queryKey: ['leave-request-timeline', user.id] }),
      ]);
    },
  });
}

/**
 * Refreshes the role-scoped queue after rejection so it remains a faithful pending-work view.
 */
export function useRejectLeaveRequest() {
  const queryClient = useQueryClient();
  const { user, sessionVersion, getSessionVersion } = useAuth();

  return useMutation<void, Error, LeaveDecision>({
    retry: false,
    mutationFn: rejectLeaveRequest,
    onSuccess: async () => {
      if (!user?.id || getSessionVersion() !== sessionVersion) return;
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['hr-workspace', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: [...organisationPendingLeaveRequestsQueryKey(user.id), sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: ['manager-attention', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: ['personal-leave-dashboard', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: [...pendingLeaveRequestsQueryKey(user.id), sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: ['queue-people', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: ['team-leave-summary', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: ['department-leave-report', user.id] }),
        queryClient.invalidateQueries({ queryKey: ['leave-request-timeline', user.id] }),
      ]);
    },
  });
}

/** Retrieves server-derived balances so the client never replicates leave-policy calculations. */
export function useLeaveBalances() {
  const { user, sessionVersion, getSessionVersion } = useAuth();

  return useQuery<LeaveBalance[], Error>({
    queryKey: [...leaveBalancesQueryKey(user?.id ?? ''), sessionVersion],
    queryFn: async ({ signal }) => {
      const result = await getLeaveBalances(signal);
      if (getSessionVersion() !== sessionVersion) throw new CancelledError();
      return result;
    },
    enabled: Boolean(user?.id),
  });
}

/** Retrieves the signed-in employee's personal request timeline and recorded decisions. */
export function useEmployeeLeaveHistory(filters: HistoryFilters) {
  const { user, sessionVersion, getSessionVersion } = useAuth();
  return useQuery<HistoryPage, Error>({
    queryKey: [...employeeLeaveHistoryQueryKey(user?.id ?? ''), sessionVersion, filters],
    queryFn: async ({ signal }) => { const result = await getEmployeeLeaveHistory(filters, signal); if (getSessionVersion() !== sessionVersion) throw new CancelledError(); return result; },
    enabled: Boolean(user?.roles.some(role => role === 'Employee' || role === 'Manager')),
    refetchOnMount: 'always', retry: false,
  });
}

/** Refreshes history and balances after cancellation so the employee sees the authoritative state. */
export function useCancelLeaveRequest() {
  const queryClient = useQueryClient();
  const { user, sessionVersion, getSessionVersion } = useAuth();

  return useMutation<void, Error, string>({
    mutationFn: cancelLeaveRequest, retry: false,
    onSuccess: async () => {
      if (!user?.id || getSessionVersion() !== sessionVersion) {
        return;
      }

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: ['hr-workspace', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: [...organisationPendingLeaveRequestsQueryKey(user.id), sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: ['manager-attention', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: ['personal-leave-dashboard', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: [...employeeLeaveHistoryQueryKey(user.id), sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: [...leaveBalancesQueryKey(user.id), sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: ['department-leave-report', user.id] }),
        queryClient.invalidateQueries({ queryKey: ['leave-request-timeline', user.id] }),
      ]);
    },
  });
}
