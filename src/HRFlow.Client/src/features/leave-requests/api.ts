import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { authHttpClient } from '../auth/api';
import type { PendingLeaveRequest } from './types';

const pendingLeaveRequestsQueryKey = ['pending-leave-requests'] as const;

async function getPendingLeaveRequests(): Promise<PendingLeaveRequest[]> {
  const response = await authHttpClient.get<PendingLeaveRequest[]>('/leave-requests', {
    params: { status: 'Pending' },
  });

  return response.data;
}

async function approveLeaveRequest(leaveRequestId: string): Promise<void> {
  await authHttpClient.post(`/leave-requests/${leaveRequestId}/approve`);
}

async function rejectLeaveRequest(leaveRequestId: string): Promise<void> {
  await authHttpClient.post(`/leave-requests/${leaveRequestId}/reject`);
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
