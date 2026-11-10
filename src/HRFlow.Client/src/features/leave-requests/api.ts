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

async function getPendingLeaveRequests(signal: AbortSignal): Promise<PendingLeaveRequest[]> {
  const response = await authHttpClient.get<PendingLeaveRequest[]>('/leave-requests', {
    params: { status: 'Pending' }, signal,
  });

  return response.data;
}

async function approveLeaveRequest({ requestId, decisionNote }: LeaveDecision): Promise<void> {
  await authHttpClient.post(`/leave-requests/${requestId}/approve`, { decisionNote });
}

async function getOrganisationPendingLeaveRequests(): Promise<PendingLeaveRequest[]> {
  const response = await authHttpClient.get<PendingLeaveRequest[]>(
    '/leave-requests/monitoring/pending',
  );

  return response.data;
}

async function rejectLeaveRequest({ requestId, decisionNote }: LeaveDecision): Promise<void> {
  await authHttpClient.post(`/leave-requests/${requestId}/reject`, { decisionNote });
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
export function usePendingLeaveRequests() {
  const { user, sessionVersion } = useAuth();

  return useQuery<PendingLeaveRequest[], Error>({
    queryKey: [...pendingLeaveRequestsQueryKey(user?.id ?? ''), sessionVersion],
    queryFn: ({ signal }) => getPendingLeaveRequests(signal),
    enabled: Boolean(user?.roles.includes('Manager')),
    refetchOnMount: 'always', retry: false,
  });
}

/** Retrieves HR's read-only organisation-wide pending-request monitoring data. */
export function useOrganisationPendingLeaveRequests() {
  const { user } = useAuth();

  return useQuery<PendingLeaveRequest[], Error>({
    queryKey: organisationPendingLeaveRequestsQueryKey(user?.id ?? ''),
    queryFn: getOrganisationPendingLeaveRequests,
    enabled: Boolean(user?.id),
  });
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
        queryClient.invalidateQueries({ queryKey: ['manager-attention', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: ['personal-leave-dashboard', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: pendingLeaveRequestsQueryKey(user.id) }),
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
        queryClient.invalidateQueries({ queryKey: ['manager-attention', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: ['personal-leave-dashboard', user.id, sessionVersion] }),
        queryClient.invalidateQueries({ queryKey: pendingLeaveRequestsQueryKey(user.id) }),
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
