import { NotificationControl } from '../../features/notifications/NotificationsPage';
import { useEffect, useId, useRef, useState } from 'react';
import { NavLink, Outlet, useLocation } from 'react-router-dom';
import { useAuth } from '../../features/auth/hooks/useAuth';
import { navigationFor, titleFor } from './navigation';

const iconPaths: Record<string, string> = {
  home: 'M3 10 12 3l9 7M5 9v12h5v-7h4v7h5V9',
  plus: 'M12 5v14M5 12h14',
  history: 'M4 5h16v16H4zM8 3v4m8-4v4M8 11h8m-8 4h5',
  check: 'M5 12l4 4L19 6',
  team: 'M9 12a4 4 0 1 0 0-8 4 4 0 0 0 0 8ZM2 21v-2a7 7 0 0 1 14 0v2m1-16a4 4 0 0 1 0 8m2 3a5 5 0 0 1 3 5',
  policy: 'M5 3h14v18H5zM9 8h6m-6 4h6m-6 4h4',
  chart: 'M4 3v18h17M8 17v-5m5 5V7m5 10V4',
};

function NavigationIcon({ name }: { name: string }) {
  return <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="1.7" strokeLinecap="round" strokeLinejoin="round" className="h-5 w-5 shrink-0"><path d={iconPaths[name]} /></svg>;
}

/** One stable authenticated layout; only replacement sessions reset protected page drafts. */
export function ApplicationShell() {
  const { user, logout, sessionVersion } = useAuth();
  const location = useLocation();
  const drawer = useRef<HTMLDialogElement>(null);
  const trigger = useRef<HTMLButtonElement>(null);
  const closeButton = useRef<HTMLButtonElement>(null);
  const content = useRef<HTMLElement>(null);
  const navigated = useRef(false);
  const [open, setOpen] = useState(false);
  const drawerId = useId();
  const groups = navigationFor(user?.roles ?? []);
  const title = titleFor(location.pathname);
  const initials = (user?.email.split('@')[0].split(/[._\s-]+/).filter(Boolean).slice(0, 2).map(part => part[0]).join('') ?? 'H').toUpperCase();

  useEffect(() => {
    drawer.current?.close();
    document.title = `${title} · HRFlow`;
  }, [location.pathname, title]);

  useEffect(() => {
    const media = window.matchMedia('(min-width: 1024px)');
    const resize = () => { if (media.matches) drawer.current?.close(); };
    media.addEventListener('change', resize);
    return () => media.removeEventListener('change', resize);
  }, []);

  useEffect(() => {
    if (!open) return;
    const previous = document.body.style.overflow;
    document.body.style.overflow = 'hidden';
    return () => { document.body.style.overflow = previous; };
  }, [open]);

  const navigation = (mobile: boolean) => <>
    <div className="shell-brand"><span className="shell-monogram" aria-hidden="true">H</span><div><span className="font-bold text-slate-900">HRFlow</span><p className="text-xs text-slate-500">Leave &amp; people</p></div></div>
    <nav aria-label={mobile ? 'Mobile primary' : 'Primary'} className="space-y-6">
      {groups.map(group => <div key={group.label}>
        <p className="mb-2 px-3 text-xs font-semibold uppercase tracking-wider text-slate-500">{group.label}</p>
        <ul className="space-y-1">{group.items.map(item => <li key={item.path}>
          <NavLink to={item.path} end onClick={() => { if (mobile) { navigated.current = true; drawer.current?.close(); } }}
            className={({ isActive }) => `shell-link${isActive ? ' shell-link-active' : ''}`}>
            <NavigationIcon name={item.icon} /><span>{item.label}</span>
          </NavLink>
        </li>)}</ul>
      </div>)}
    </nav>
    <div className="mt-auto border-t border-slate-200 pt-5">
      <button type="button" className="ui-secondary w-full" onClick={logout}>Sign out</button>
    </div>
  </>;

  return <div className="application-shell">
    <a href="#workspace-content" className="skip-link">Skip to content</a>
    <aside className="desktop-sidebar">{navigation(false)}</aside>
    <div className="shell-workspace">
      <header className="shell-header">
        <div className="flex min-w-0 items-center gap-3">
          <button ref={trigger} type="button" className="ui-secondary lg:hidden" aria-label="Open navigation" aria-controls={drawerId} aria-expanded={open}
            onClick={() => { navigated.current = false; drawer.current?.showModal(); setOpen(true); closeButton.current?.focus(); }}>
            <svg aria-hidden="true" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" className="h-5 w-5"><path d="M4 6h16M4 12h16M4 18h16" /></svg>
          </button>
          <div className="min-w-0"><p className="text-xs font-semibold uppercase tracking-wider text-slate-500">Your workspace</p><h1 className="mt-1 break-words text-xl font-bold text-slate-900 sm:text-2xl">{title}</h1></div>
        </div>
        <NotificationControl />
        <div className="shell-account"><span className="shell-avatar" aria-hidden="true">{initials}</span><div className="min-w-0"><p className="break-all text-sm font-semibold text-slate-800">{user?.email}</p><p className="mt-1 text-xs leading-5 text-slate-500">{user?.roles.join(' · ') || 'No assigned capabilities'}</p></div></div>
      </header>
      <main ref={content} id="workspace-content" tabIndex={-1} className="shell-content">
        <Outlet key={`${user?.id}:${sessionVersion}`} />
      </main>
    </div>
    <dialog ref={drawer} id={drawerId} aria-label="Navigation" className="navigation-drawer"
      onClick={event => { if (event.target === event.currentTarget) { const rect = event.currentTarget.getBoundingClientRect(); if (event.clientX > rect.right || event.clientY > rect.bottom) drawer.current?.close(); } }}
      onCancel={event => { event.preventDefault(); drawer.current?.close(); }}
      onClose={() => { setOpen(false); if (navigated.current) content.current?.focus(); else if (trigger.current?.getClientRects().length) trigger.current.focus(); }}
      onKeyDown={event => {
        if (event.key !== 'Tab') return;
        const controls = Array.from(drawer.current?.querySelectorAll<HTMLElement>('button:not(:disabled), a[href]') ?? []);
        const first = controls[0]; const last = controls[controls.length - 1];
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first?.focus(); }
      }}>
      <button ref={closeButton} type="button" className="ui-secondary self-end" onClick={() => drawer.current?.close()}>Close navigation</button>
      {navigation(true)}
    </dialog>
  </div>;
}
