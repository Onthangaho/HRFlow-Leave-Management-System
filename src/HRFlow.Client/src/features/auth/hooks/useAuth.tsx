import {
  createContext,
  useCallback,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from 'react';
import { useMutation } from '@tanstack/react-query';
import { configureAuthInterceptors, login, refresh } from '../api.ts';
import type { AuthSession, AuthUser, LoginRequest, TokenResponse } from '../types.ts';

const roleClaimType =
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role';

interface AuthContextValue {
  session: AuthSession | null;
  user: AuthUser | null;
  isAuthenticated: boolean;
  isLoggingIn: boolean;
  login: (request: LoginRequest) => Promise<void>;
  logout: () => void;
}

const AuthContext = createContext<AuthContextValue | undefined>(undefined);

interface JwtPayload {
  sub?: string;
  email?: string;
  [roleClaimType]?: string | string[];
}

function decodeJwtPayload(token: string): JwtPayload {
  const payloadSegment = token.split('.')[1] ?? '';
  const normalized = payloadSegment.replace(/-/g, '+').replace(/_/g, '/');
  const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');
  const decoded = atob(padded);
  return JSON.parse(decoded) as JwtPayload;
}

function createSession(tokenResponse: TokenResponse): AuthSession {
  const payload = decodeJwtPayload(tokenResponse.accessToken);
  const roles = payload[roleClaimType];

  return {
    accessToken: tokenResponse.accessToken,
    refreshToken: tokenResponse.refreshToken,
    accessTokenExpiresAtUtc: tokenResponse.accessTokenExpiresAtUtc,
    user: {
      id: payload.sub ?? '',
      email: payload.email ?? '',
      roles: Array.isArray(roles) ? roles : roles ? [roles] : [],
    },
  };
}

/**
 * Stores auth session in-memory only to reduce token exposure to XSS at the cost of losing session on full page reload.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [session, setSession] = useState<AuthSession | null>(null);
  const sessionRef = useRef<AuthSession | null>(null);
  const refreshInFlightRef = useRef<Promise<string | null> | null>(null);

  const updateSession = useCallback((newSession: AuthSession | null) => {
    sessionRef.current = newSession;
    setSession(newSession);
  }, []);

  const clearSession = useCallback(() => {
    updateSession(null);
  }, [updateSession]);

  const refreshAccessToken = useCallback(async () => {
    if (!sessionRef.current?.refreshToken) {
      return null;
    }

    if (refreshInFlightRef.current) {
      return refreshInFlightRef.current;
    }

    refreshInFlightRef.current = (async () => {
      try {
        const tokenResponse = await refresh({
          refreshToken: sessionRef.current?.refreshToken ?? '',
        });
        const nextSession = createSession(tokenResponse);
        updateSession(nextSession);
        return nextSession.accessToken;
      } catch {
        updateSession(null);
        return null;
      } finally {
        refreshInFlightRef.current = null;
      }
    })();

    return refreshInFlightRef.current;
  }, [updateSession]);

  const loginMutation = useMutation({
    mutationFn: login,
    onSuccess: (tokenResponse) => {
      updateSession(createSession(tokenResponse));
    },
  });

  const loginUser = useCallback(
    async (request: LoginRequest) => {
      await loginMutation.mutateAsync(request);
    },
    [loginMutation],
  );

  useEffect(() => {
    const tearDown = configureAuthInterceptors({
      getAccessToken: () => sessionRef.current?.accessToken ?? null,
      refreshAccessToken,
      clearSession,
    });

    return tearDown;
  }, [clearSession, refreshAccessToken]);

  const value = useMemo<AuthContextValue>(
    () => ({
      session,
      user: session?.user ?? null,
      isAuthenticated: Boolean(session?.accessToken),
      isLoggingIn: loginMutation.isPending,
      login: loginUser,
      logout: clearSession,
    }),
    [clearSession, loginMutation.isPending, loginUser, session],
  );

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

/**
 * Centralizes auth consumption so route guards and pages share one source of truth for session and role checks.
 */
export function useAuth() {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within AuthProvider');
  }

  return context;
}
