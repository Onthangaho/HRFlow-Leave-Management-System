import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';
import type { EmployeeLeaveRequest, LeaveBalance, PendingLeaveRequest } from './types';

const pendingLeaveRequestsQueryKey = (userId: string) =>
  ['pending-leave-requests', userId] as const;
const organisationPendingLeaveRequestsQueryKey = (userId: string) =>
  ['organisation-pending-leave-requests', userId] as const;
const leaveBalancesQueryKey = (userId: string) => ['leave-balances', userId] as const;
const employeeLeaveHistoryQueryKey = (userId: string) =>
  ['employee-leave-history', userId] as const;

async function getPendingLeaveRequests(): Promise<PendingLeaveRequest[]> {
  const response = await authHttpClient.get<PendingLeaveRequest[]>('/leave-requests', {
    params: { status: 'Pending' },
  });

  return response.data;
}

async function approveLeaveRequest(leaveRequestId: string): Promise<void> {
  await authHttpClient.post(`/leave-requests/${leaveRequestId}/approve`);
}

async function getOrganisationPendingLeaveRequests(): Promise<PendingLeaveRequest[]> {
  const response = await authHttpClient.get<PendingLeaveRequest[]>(
    '/leave-requests/monitoring/pending',
  );

  return response.data;
}

async function rejectLeaveRequest(leaveRequestId: string): Promise<void> {
  await authHttpClient.post(`/leave-requests/${leaveRequestId}/reject`);
}

async function getLeaveBalances(): Promise<LeaveBalance[]> {
  const response = await authHttpClient.get<LeaveBalance[]>('/leave-requests/balances');
  return response.data;
}

async function getEmployeeLeaveHistory(): Promise<EmployeeLeaveRequest[]> {
  const response = await authHttpClient.get<EmployeeLeaveRequest[]>('/leave-requests/history');
  return response.data;
}

async function cancelLeaveRequest(leaveRequestId: string): Promise<void> {
  await authHttpClient.post(`/leave-requests/${leaveRequestId}/cancel`);
}

/**
 * Retrieves the review queue from the server because its role-scoped contents can change outside
 * this page as other reviewers process requests.
 */
export function usePendingLeaveRequests() {
  const { user } = useAuth();

  return useQuery<PendingLeaveRequest[], Error>({
    queryKey: pendingLeaveRequestsQueryKey(user?.id ?? ''),
    queryFn: getPendingLeaveRequests,
    enabled: Boolean(user?.id),
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

  return useMutation<void, Error, string>({
    mutationFn: approveLeaveRequest,
    onSuccess: async () => {
      if (!user?.id || getSessionVersion() !== sessionVersion) return;
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: pendingLeaveRequestsQueryKey(user.id) }),
        queryClient.invalidateQueries({ queryKey: ['team-leave-summary', user.id] }),
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

  return useMutation<void, Error, string>({
    mutationFn: rejectLeaveRequest,
    onSuccess: async () => {
      if (!user?.id || getSessionVersion() !== sessionVersion) return;
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: pendingLeaveRequestsQueryKey(user.id) }),
        queryClient.invalidateQueries({ queryKey: ['team-leave-summary', user.id] }),
      ]);
    },
  });
}

/** Retrieves server-derived balances so the client never replicates leave-policy calculations. */
export function useLeaveBalances() {
  const { user } = useAuth();

  return useQuery<LeaveBalance[], Error>({
    queryKey: leaveBalancesQueryKey(user?.id ?? ''),
    queryFn: getLeaveBalances,
    enabled: Boolean(user?.id),
  });
}

/** Retrieves the signed-in employee's personal request timeline and recorded decisions. */
export function useEmployeeLeaveHistory() {
  const { user } = useAuth();

  return useQuery<EmployeeLeaveRequest[], Error>({
    queryKey: employeeLeaveHistoryQueryKey(user?.id ?? ''),
    queryFn: getEmployeeLeaveHistory,
    enabled: Boolean(user?.id),
  });
}

/** Refreshes history and balances after cancellation so the employee sees the authoritative state. */
export function useCancelLeaveRequest() {
  const queryClient = useQueryClient();
  const { user } = useAuth();

  return useMutation<void, Error, string>({
    mutationFn: cancelLeaveRequest,
    onSuccess: async () => {
      if (!user?.id) {
        return;
      }

      await Promise.all([
        queryClient.invalidateQueries({ queryKey: employeeLeaveHistoryQueryKey(user.id) }),
        queryClient.invalidateQueries({ queryKey: leaveBalancesQueryKey(user.id) }),
      ]);
    },
  });
}
