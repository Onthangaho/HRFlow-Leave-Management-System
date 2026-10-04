import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { authHttpClient } from '../auth/api';
import type { EmployeeLeaveRequest, LeaveBalance, PendingLeaveRequest } from './types';

const pendingLeaveRequestsQueryKey = ['pending-leave-requests'] as const;
const organisationPendingLeaveRequestsQueryKey = ['organisation-pending-leave-requests'] as const;
const leaveBalancesQueryKey = ['leave-balances'] as const;
const employeeLeaveHistoryQueryKey = ['employee-leave-history'] as const;

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
  return useQuery<PendingLeaveRequest[], Error>({
    queryKey: pendingLeaveRequestsQueryKey,
    queryFn: getPendingLeaveRequests,
  });
}

/** Retrieves HR's read-only organisation-wide pending-request monitoring data. */
export function useOrganisationPendingLeaveRequests() {
  return useQuery<PendingLeaveRequest[], Error>({
    queryKey: organisationPendingLeaveRequestsQueryKey,
    queryFn: getOrganisationPendingLeaveRequests,
  });
}

/**
 * Refreshes the role-scoped queue after approval so a processed request disappears without reload.
 */
export function useApproveLeaveRequest() {
  const queryClient = useQueryClient();

  return useMutation<void, Error, string>({
    mutationFn: approveLeaveRequest,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: pendingLeaveRequestsQueryKey }),
  });
}

/**
 * Refreshes the role-scoped queue after rejection so it remains a faithful pending-work view.
 */
export function useRejectLeaveRequest() {
  const queryClient = useQueryClient();

  return useMutation<void, Error, string>({
    mutationFn: rejectLeaveRequest,
    onSuccess: () =>
      queryClient.invalidateQueries({ queryKey: pendingLeaveRequestsQueryKey }),
  });
}

/** Retrieves server-derived balances so the client never replicates leave-policy calculations. */
export function useLeaveBalances() {
  return useQuery<LeaveBalance[], Error>({
    queryKey: leaveBalancesQueryKey,
    queryFn: getLeaveBalances,
  });
}

/** Retrieves the signed-in employee's personal request timeline and recorded decisions. */
export function useEmployeeLeaveHistory() {
  return useQuery<EmployeeLeaveRequest[], Error>({
    queryKey: employeeLeaveHistoryQueryKey,
    queryFn: getEmployeeLeaveHistory,
  });
}

/** Refreshes history and balances after cancellation so the employee sees the authoritative state. */
export function useCancelLeaveRequest() {
  const queryClient = useQueryClient();

  return useMutation<void, Error, string>({
    mutationFn: cancelLeaveRequest,
    onSuccess: async () => {
      await Promise.all([
        queryClient.invalidateQueries({ queryKey: employeeLeaveHistoryQueryKey }),
        queryClient.invalidateQueries({ queryKey: leaveBalancesQueryKey }),
      ]);
    },
  });
}
