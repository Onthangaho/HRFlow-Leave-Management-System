import { useVisible } from './useVisible';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';
const pollingInterval = 30_000;

/** Polls only the initiating authenticated visible session; no previous-session placeholder data. */
function useUnreadCount() {
  const { user, sessionVersion } = useAuth();
  const visible = useVisible();
  return useQuery({
    queryKey: ['notifications-count', user?.id, sessionVersion],
    queryFn: async ({ signal }) => (await authHttpClient.get<{ unreadCount: number }>('/notifications/unread-count', { signal })).data,
    enabled: Boolean(user?.roles.some(role => ['Employee', 'Manager', 'HR Administrator'].includes(role))), refetchOnMount: 'always', retry: false,
    refetchInterval: visible ? pollingInterval : false, refetchIntervalInBackground: false,
  });
}

/** A real inbox destination, with an accurate badge only after a successful count response. */
export function NotificationControl() {
  const count = useUnreadCount();
  const unread = !count.isError ? count.data?.unreadCount : undefined;
  return <Link to="/notifications" className="ui-secondary shrink-0" aria-label={unread === undefined ? 'Notifications; count unavailable' : `Notifications; ${unread} unread`}>
    <svg aria-hidden="true" className="h-5 w-5" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7"><path d="M18 8a6 6 0 0 0-12 0v5l-2 4h16l-2-4V8ZM10 21h4" /></svg>
    {unread !== undefined && unread > 0 && <span className="rounded-full bg-indigo-700 px-2 py-0.5 text-xs text-white">{unread > 99 ? '99+' : unread}</span>}
  </Link>;
}
