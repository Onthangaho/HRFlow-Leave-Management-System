import { useEffect, useRef, useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';

/** Authenticated blobs are scoped to a replacement-session generation and never become public image URLs. */
export function PrivateAvatar({ employeeId, imageVersion, fallback, className = '' }: { employeeId?: string; imageVersion?: string | null; fallback: string; className?: string }) {
  const { user, sessionVersion, getSessionVersion } = useAuth();
  const key = `${user?.id}:${sessionVersion}:${employeeId ?? 'me'}:${imageVersion ?? ''}`;
  const allowed = Boolean(user && (!employeeId || user.roles.includes('HR Administrator')));
  const container = useRef<HTMLSpanElement>(null);
  const [visible, setVisible] = useState(false);
  useEffect(() => {
    if (!employeeId || !container.current) return;
    if (!('IntersectionObserver' in window)) { setVisible(true); return; }
    const observer = new IntersectionObserver(entries => { if (entries.some(entry => entry.isIntersecting)) { setVisible(true); observer.disconnect(); } }, { rootMargin: '100px' });
    observer.observe(container.current); return () => observer.disconnect();
  }, [employeeId, imageVersion]);
  const enabled = allowed && (!employeeId || Boolean(imageVersion) && visible);
  const query = useQuery({ queryKey: ['profile-image-content', user?.id, sessionVersion, employeeId ?? 'me', imageVersion ?? null],
    queryFn: async ({ signal }) => (await authHttpClient.get<Blob>(employeeId ? `/employees/${employeeId}/image` : '/me/image/content', { signal, responseType: 'blob' })).data,
    enabled, retry: false, refetchOnMount: 'always', gcTime: 0 });
  const [image, setImage] = useState<{ key: string; url: string }>();
  useEffect(() => {
    if (!enabled || !query.data || query.isError || getSessionVersion() !== sessionVersion) return;
    const url = URL.createObjectURL(query.data); setImage({ key, url });
    return () => URL.revokeObjectURL(url);
  }, [query.data, query.isError, key, enabled, getSessionVersion, sessionVersion]);
  return <span ref={container} className="inline-flex">{enabled && image?.key === key && !query.isError ? <img className={`rounded-full object-cover ${className}`} src={image.url} alt={employeeId ? '' : 'Profile'} />
    : <span className={`inline-flex items-center justify-center rounded-full bg-indigo-100 font-bold text-indigo-800 ${className}`} aria-label="Profile initials">{fallback}</span>}</span>;
}
