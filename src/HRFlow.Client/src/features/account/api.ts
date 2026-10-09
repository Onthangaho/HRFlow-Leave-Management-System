import { useQuery } from '@tanstack/react-query';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';

/** Private own-account contract; canonical fields are read-only and HR versions are not included. */
export interface OwnProfile {
  employeeNumber: string | null; employmentStartDate: string | null; scheduleHistory: import('../employees/components/ScheduleWorkspace').ScheduleHistory;
  canonicalName: string; email: string; roles: string[]; departmentName: string; managerName: string | null;
  isActive: boolean; isActivated: boolean; preferredDisplayName: string | null; contactPhone: string | null; profileVersion: string;
}
/** Only categories implemented by the durable in-app worker are configurable. */
export interface OwnPreferences {
  theme: 'System' | 'Light' | 'Dark'; submissionNotifications: boolean; decisionNotifications: boolean;
  cancellationNotifications: boolean; reassignmentNotifications: boolean; preferencesVersion: string;
}

/** Reads private own fields through cancellable session-isolated queries, never the HR directory. */
export function useOwnProfile() {
  const { user, sessionVersion } = useAuth();
  return useQuery({ queryKey: ['own-profile', user?.id, sessionVersion],
    queryFn: async ({ signal }) => (await authHttpClient.get<OwnProfile>('/me', { signal })).data,
    enabled: Boolean(user?.roles.some(r => ['Employee', 'Manager', 'HR Administrator'].includes(r))),
    refetchOnMount: 'always', retry: false });
}

/** Saved preferences drive the shell; background refresh cannot rebase an open form. */
export function useOwnPreferences() {
  const { user, sessionVersion } = useAuth();
  return useQuery({ queryKey: ['own-preferences', user?.id, sessionVersion],
    queryFn: async ({ signal }) => (await authHttpClient.get<OwnPreferences>('/me/preferences', { signal })).data,
    enabled: Boolean(user?.roles.some(r => ['Employee', 'Manager', 'HR Administrator'].includes(r))),
    refetchOnMount: 'always', retry: false });
}
