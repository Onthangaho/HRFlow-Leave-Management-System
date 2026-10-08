import { useQuery } from '@tanstack/react-query';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';

/** Immutable transition facts; sensitive context is already filtered by current server permissions. */
export interface TimelineEvent {
  id: string; actorId: string; actorName: string; action: string; timestampUtc: string;
  oldStatus: string | null; newStatus: string; explanation: string | null; correlationId: string | null;
}
/** Minimal authorized single-request summary, with an honest legacy submission indicator. */
export interface RequestTimeline {
  requestId: string; employeeName: string; employeeIsActive: boolean; leaveTypeName: string;
  startDate: string; endDate: string; status: string; submissionRecorded: boolean; events: TimelineEvent[];
}
/** Session-specific reads cannot reuse another login's cached request, including same-account relogin. */
export function useRequestTimeline(requestId: string) {
  const { user, sessionVersion } = useAuth();
  return useQuery({
    queryKey: ['leave-request-timeline', user?.id ?? '', sessionVersion, requestId],
    queryFn: async ({ signal }) => (await authHttpClient.get<RequestTimeline>(
      '/leave-requests/' + encodeURIComponent(requestId) + '/timeline', { signal })).data,
    enabled: Boolean(requestId && user?.roles.some(role => ['Employee', 'Manager', 'HR Administrator'].includes(role))),
    refetchOnMount: 'always', retry: false,
  });
}
