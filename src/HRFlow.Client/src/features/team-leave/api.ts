import { useQuery } from '@tanstack/react-query';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';

/** Contains only coverage-planning data; inactive reports retain their approved history. */
export interface TeamLeave {
  requestId: string;
  employeeId: string;
  employeeName: string;
  isActive: boolean;
  leaveTypeId: string;
  leaveTypeName: string;
  startDate: string;
  endDate: string;
}

/** Cancels old-session reads and refreshes on entry because reporting and approvals can change elsewhere. */
export function useTeamLeave(start: string, end: string) {
  const { user, sessionVersion } = useAuth();
  return useQuery({
    queryKey: ['team-leave-summary', user?.id ?? '', sessionVersion, start, end],
    queryFn: async ({ signal }) => (await authHttpClient.get<TeamLeave[]>('/leave-requests/team-summary', {
      params: { start, end }, signal,
    })).data,
    enabled: Boolean(user?.id && user.roles.includes('Manager')),
    refetchOnMount: 'always',
    retry: false,
  });
}
