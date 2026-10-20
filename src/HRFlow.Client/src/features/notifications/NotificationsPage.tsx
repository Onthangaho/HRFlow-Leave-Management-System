import { useVisible } from './useVisible';
import { useRef, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';

interface Notification { id: string; message: string; requestId: string | null; createdAtUtc: string; readAtUtc: string | null }
interface NotificationPage { items: Notification[]; total: number; page: number; pageSize: number }
const pollingInterval = 30_000;
const pageSize = 20;

/** Recipient-only inbox; callbacks cannot update another login, including same-account relogin. */
export function NotificationsPage() {
  const { user, sessionVersion, getSessionVersion } = useAuth();
  const queries = useQueryClient();
  const visible = useVisible();
  const [filter, setFilter] = useState('all');
  const [page, setPage] = useState(1);
  const [saving, setSaving] = useState<string | null>(null);
  const busy = useRef(false);
  const [feedback, setFeedback] = useState('');
  const inbox = useQuery({
    queryKey: ['notifications', user?.id, sessionVersion, filter, page],
    queryFn: async ({ signal }) => (await authHttpClient.get<NotificationPage>('/notifications', { params: { filter, page, pageSize }, signal })).data,
    enabled: Boolean(user?.roles.some(role => ['Employee', 'Manager', 'HR Administrator'].includes(role))), refetchOnMount: 'always', retry: false,
    refetchInterval: visible ? pollingInterval : false, refetchIntervalInBackground: false,
  });
  const markRead = async (id: string) => {
    if (busy.current) return;
    busy.current = true; setSaving(id); setFeedback('');
    const account = user?.id; const epoch = sessionVersion;
    const current = () => getSessionVersion() === epoch;
    try {
      await authHttpClient.patch(`/notifications/${id}/read`, undefined, { skipAuthReplay: true });
      if (!current()) return;
      await Promise.all([
        queries.invalidateQueries({ queryKey: ['notifications', account, epoch] }),
        queries.invalidateQueries({ queryKey: ['notifications-count', account, epoch] }),
      ]);
      if (current()) setFeedback('Notification marked read.');
    } catch {
      if (current()) setFeedback('Read status could not be confirmed. Refresh before trying again.');
    } finally {
      if (current()) { busy.current = false; setSaving(null); }
    }
  };
  return <div className="workspace-page mx-auto max-w-4xl space-y-5">
    <section className="ui-panel space-y-3">
      <h2 className="text-xl font-bold">Your inbox</h2>
      <p className="text-slate-600">Leave updates delivered after changes are saved. Request access is checked again when you open history. Unavailable unread items still count until marked read.</p>
      <div className="flex flex-wrap items-end gap-3">
        <label className="space-y-1"><span className="block text-sm font-semibold">Show notifications</span><select className="ui-input" value={filter} onChange={e => { setFilter(e.target.value); setPage(1); setFeedback(''); }}><option value="all">All</option><option value="unread">Unread</option><option value="read">Read</option></select></label>
        <button className="ui-secondary" disabled={inbox.isFetching} onClick={() => void inbox.refetch()}>Refresh</button>
      </div>
    </section>
    <p role="status" aria-live="polite">{feedback || (inbox.isFetching ? inbox.data ? 'Refreshing notifications…' : 'Loading notifications…' : '')}</p>
    {inbox.isError && <section className="ui-panel space-y-3" role="alert"><p>Notifications could not be loaded. Your filter is unchanged. Previously loaded items may be out of date.</p><button className="ui-primary" disabled={inbox.isFetching} onClick={() => void inbox.refetch()}>Try again</button></section>}
    {!inbox.isError && inbox.data && <>
      {inbox.data.items.length === 0 ? <section className="ui-panel">No {filter === 'all' ? '' : filter + ' '}notifications on this page.</section> : <ul className="space-y-3" aria-label="Notifications">
        {inbox.data.items.map(item => <li key={item.id} className="ui-panel flex flex-wrap items-start justify-between gap-4">
          <div className="min-w-0 space-y-2"><span className="text-xs font-bold text-indigo-700">{item.readAtUtc ? 'Read' : 'Unread'}</span><p className="font-semibold">{item.message}</p><time className="block text-sm text-slate-500" dateTime={item.createdAtUtc}>{new Intl.DateTimeFormat('en', { dateStyle: 'medium', timeStyle: 'short', timeZone: 'UTC' }).format(new Date(item.createdAtUtc))} UTC</time>
            {item.requestId && <Link className="font-semibold text-indigo-700 underline" to={`/leave-requests/${item.requestId}/history`}>View request history</Link>}</div>
          {!item.readAtUtc && <button className="ui-secondary" disabled={saving !== null} onClick={() => void markRead(item.id)}>{saving === item.id ? 'Saving…' : 'Mark read'}</button>}
        </li>)}
      </ul>}
      <nav className="flex flex-wrap items-center justify-between gap-3" aria-label="Inbox pages"><button className="ui-secondary" disabled={page === 1 || inbox.isFetching} onClick={() => setPage(p => p - 1)}>Previous</button><p>Page {page} · {inbox.data.total} matching notifications</p><button className="ui-secondary" disabled={page * pageSize >= inbox.data.total || inbox.isFetching} onClick={() => setPage(p => p + 1)}>Next</button></nav>
    </>}
  </div>;
}
