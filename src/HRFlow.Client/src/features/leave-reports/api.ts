import { useQuery } from '@tanstack/react-query';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';

/** Separates summed overlapping durations from distinct people and identifies inactive historical contributions. */
export interface LeaveReportMetrics {
  pendingRequests: number;
  approvedRequests: number;
  employeesWithApprovedLeave: number;
  approvedRequestDays: number;
  inactiveApprovedRequests: number;
  inactiveEmployeesWithApprovedLeave: number;
  inactiveApprovedRequestDays: number;
}

/** Holds only the applied query selection; draft inputs are kept separately by the form. */
export interface LeaveReportFilters { start: string; end: string; departmentId: string }

/** Contains department aggregates without employee-level personal information. */
export interface DepartmentLeaveReport {
  start: string;
  end: string;
  departmentId: string | null;
  totals: LeaveReportMetrics;
  departments: { departmentId: string; departmentName: string; metrics: LeaveReportMetrics }[];
}

/** Refetches current reporting state on entry and cancels old-session reads rather than sharing cached results. */
export function useDepartmentLeaveReport(filters: LeaveReportFilters) {
  const { user, sessionVersion } = useAuth();
  return useQuery({
    queryKey: ['department-leave-report', user?.id ?? '', sessionVersion, filters.start, filters.end, filters.departmentId],
    queryFn: async ({ signal }) => (await authHttpClient.get<DepartmentLeaveReport>('/reports/department-leave', {
      signal, params: { start: filters.start, end: filters.end, ...(filters.departmentId ? { departmentId: filters.departmentId } : {}) },
    })).data,
    enabled: Boolean(user?.id && user.roles.includes('HR Administrator')),
    refetchOnMount: 'always', retry: false,
  });
}

/** Provides cancellable HR-only department choices without implementing department administration. */
export function useReportDepartments() {
  const { user, sessionVersion } = useAuth();
  return useQuery({
    queryKey: ['report-departments', user?.id ?? '', sessionVersion],
    queryFn: async ({ signal }) => (await authHttpClient.get<{ id: string; name: string }[]>('/departments', { signal })).data,
    enabled: Boolean(user?.id && user.roles.includes('HR Administrator')),
    refetchOnMount: 'always', retry: false,
  });
}
