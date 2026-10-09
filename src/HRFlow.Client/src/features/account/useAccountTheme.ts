import { useLayoutEffect } from 'react';
import { useAuth } from '../auth/hooks/useAuth';
import { useOwnPreferences } from './api';

/** Applies only the current session's server preference and cleans up before replacement/login rendering. */
export function useAccountTheme() {
  const { user, sessionVersion } = useAuth();
  const preferences = useOwnPreferences();
  const mode = preferences.data?.theme ?? 'System';
  useLayoutEffect(() => {
    const root = document.documentElement;
    const system = window.matchMedia('(prefers-color-scheme: dark)');
    const apply = () => {
      const dark = mode === 'Dark' || mode === 'System' && system.matches;
      root.dataset.theme = dark ? 'dark' : 'light';
      root.style.colorScheme = dark ? 'dark' : 'light';
    };
    apply();
    system.addEventListener('change', apply);
    return () => { system.removeEventListener('change', apply); delete root.dataset.theme; root.style.colorScheme = 'light'; };
  }, [mode, user?.id, sessionVersion]);
}
