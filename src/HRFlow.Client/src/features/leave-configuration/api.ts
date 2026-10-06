import { useRef } from 'react';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../auth/hooks/useAuth';
import { authHttpClient } from '../auth/api';
import type { ConfigurationWrite, LeavePolicy, ManagedLeaveType } from './types';

/** Loads HR snapshots in the current account namespace; cancellation prevents abandoned reads. */
export function useLeaveConfiguration() {
  const { user } = useAuth();
  const enabled = Boolean(user?.id && user.roles.includes('HR Administrator'));
  const policies = useQuery({ queryKey: ['leave-policies', user?.id ?? ''], enabled,
    queryFn: async ({ signal }) => (await authHttpClient.get<LeavePolicy[]>('/management/leave-policies', { signal })).data });
  const types = useQuery({ queryKey: ['managed-leave-types', user?.id ?? ''], enabled,
    queryFn: async ({ signal }) => (await authHttpClient.get<ManagedLeaveType[]>('/management/leave-types', { signal })).data });
  return { policies, types };
}
/** Keeps late completion callbacks in their initiating account, including during account switches. */
export function useConfigurationWrite() {
  const { user } = useAuth();
  const currentAccount = useRef(user?.id);
  currentAccount.current = user?.id;
  const client = useQueryClient();
  return useMutation({
    mutationFn: async (write: ConfigurationWrite & { accountId: string }) => {
      if (currentAccount.current !== write.accountId) throw new Error('The session changed.');
      const url = `/management/${write.kind}${write.id ? `/${write.id}` : ''}`;
      if (write.method === 'delete') await authHttpClient.delete(url, { params: { expectedVersion: write.expectedVersion } });
      else await authHttpClient[write.method](url, write.body);
    },
    onSuccess: async (_, write) => {
      if (currentAccount.current !== write.accountId) return;
      await Promise.all(['leave-policies', 'managed-leave-types', 'leave-types', 'leave-balances',
        'employee-leave-history', 'pending-leave-requests', 'organisation-pending-leave-requests']
        .map(key => client.invalidateQueries({ queryKey: [key, write.accountId] })));
    },
  });
}
