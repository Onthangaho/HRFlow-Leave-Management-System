import axios, {
  AxiosError,
  AxiosHeaders,
  CanceledError,
  type InternalAxiosRequestConfig,
} from 'axios';
import type {
  LoginRequest,
  RefreshTokenRequest,
  TokenResponse,
} from './types.ts';

declare module 'axios' {
  interface AxiosRequestConfig {
    /** Lifecycle writes must not be replayed following an authentication failure. */
    skipAuthReplay?: boolean;
  }
}

const apiBaseUrl = import.meta.env.VITE_API_BASE_URL;

if (!apiBaseUrl || apiBaseUrl.trim() === '') {
  throw new Error(
    'VITE_API_BASE_URL environment variable is not configured. ' +
    'Please ensure .env file exists with a valid API base URL.'
  );
}

interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retryOnce?: boolean;
  _sessionVersion?: number;
}

/** Supplies live session refs so delayed responses cannot operate against a replacement login. */
export interface AuthInterceptorControls {
  getAccessToken: () => string | null;
  getSessionVersion: () => number;
  refreshAccessToken: () => Promise<string | null>;
  clearSession: () => void;
}

export const authHttpClient = axios.create({
  baseURL: apiBaseUrl,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

const refreshHttpClient = axios.create({
  baseURL: apiBaseUrl,
  headers: {
    'Content-Type': 'application/json',
  },
  timeout: 30000,
});

/**
 * Uses the real auth endpoints so the client behavior always matches backend token rotation rules.
 */
export async function login(request: LoginRequest): Promise<TokenResponse> {
  const response = await refreshHttpClient.post<TokenResponse>('/auth/login', request);
  return response.data;
}

/**
 * Keeps refresh isolated from interceptors to prevent recursive retries when refresh itself fails.
 */
export async function refresh(
  request: RefreshTokenRequest,
): Promise<TokenResponse> {
  const response = await refreshHttpClient.post<TokenResponse>(
    '/auth/refresh',
    request,
  );
  return response.data;
}

/**
 * Adds at most one same-session 401 refresh/retry while rejecting stale completions.
 * Explicitly non-replayable operations and refresh itself never enter authentication replay.
 */
export function configureAuthInterceptors(
  controls: AuthInterceptorControls,
): () => void {
  let refreshInFlight: {
    sessionVersion: number;
    promise: Promise<string>;
  } | null = null;

  const clearSessionForVersion = (sessionVersion: number) => {
    if (controls.getSessionVersion() === sessionVersion) {
      controls.clearSession();
    }
  };

  const getSharedRefreshPromise = (sessionVersion: number) => {
    if (refreshInFlight?.sessionVersion === sessionVersion) {
      return refreshInFlight.promise;
    }

    const refreshFlight: {
      sessionVersion: number;
      promise: Promise<string>;
    } = {
      sessionVersion,
      promise: Promise.resolve(''),
    };

    refreshFlight.promise = (async () => {
      const refreshedToken = await controls.refreshAccessToken();
      if (!refreshedToken) {
        throw new Error('Refresh endpoint did not return a new access token.');
      }

      if (controls.getSessionVersion() !== sessionVersion) {
        throw new Error('Refresh completed for a stale authentication session.');
      }

      return refreshedToken;
    })()
      .catch((refreshError) => {
        clearSessionForVersion(sessionVersion);
        throw refreshError;
      })
      .finally(() => {
        if (refreshInFlight === refreshFlight) {
          refreshInFlight = null;
        }
      });

    refreshInFlight = refreshFlight;
    return refreshFlight.promise;
  };

  const requestInterceptorId = authHttpClient.interceptors.request.use(
    (config) => {
      const request = config as RetryableRequestConfig;
      const sessionVersion = controls.getSessionVersion();
      // A retry retains its initiating epoch; never inject a new account's token into an old operation.
      if (request._sessionVersion !== undefined && request._sessionVersion !== sessionVersion) {
        throw new CanceledError('Authentication session changed.', config);
      }
      request._sessionVersion = sessionVersion;
      const token = controls.getAccessToken();
      if (!token) {
        return config;
      }

      if (config.headers instanceof AxiosHeaders) {
        config.headers.set('Authorization', `Bearer ${token}`);
      } else {
        const headers = AxiosHeaders.from(config.headers);
        headers.set('Authorization', `Bearer ${token}`);
        config.headers = headers;
      }

      return config;
    },
    (error) => { throw error; },
    { synchronous: true },
  );

  const responseInterceptorId = authHttpClient.interceptors.response.use(
    (response) => {
      if ((response.config as RetryableRequestConfig)._sessionVersion !== controls.getSessionVersion()) {
        throw new CanceledError('Authentication session changed.', response.config);
      }
      return response;
    },
    async (error: AxiosError) => {
      const originalRequest = error.config as RetryableRequestConfig | undefined;
      if (originalRequest?._sessionVersion !== undefined
        && originalRequest._sessionVersion !== controls.getSessionVersion()) {
        return Promise.reject(new CanceledError('Authentication session changed.', originalRequest));
      }
      if (!originalRequest || error.response?.status !== 401) {
        return Promise.reject(error);
      }

      const sessionVersion = originalRequest._sessionVersion;
      if (sessionVersion === undefined || sessionVersion !== controls.getSessionVersion()) {
        return Promise.reject(error);
      }

      const isRefreshRequest = originalRequest.url?.includes('/auth/refresh');
      if (isRefreshRequest || originalRequest._retryOnce || originalRequest.skipAuthReplay) {
        clearSessionForVersion(sessionVersion);
        return Promise.reject(error);
      }

      originalRequest._retryOnce = true;
      let nextToken: string;
      try {
        nextToken = await getSharedRefreshPromise(sessionVersion);
      } catch {
        return Promise.reject(error);
      }

      if (sessionVersion !== controls.getSessionVersion()) {
        return Promise.reject(error);
      }

      if (originalRequest.headers instanceof AxiosHeaders) {
        originalRequest.headers.set('Authorization', `Bearer ${nextToken}`);
      } else {
        const headers = AxiosHeaders.from(originalRequest.headers);
        headers.set('Authorization', `Bearer ${nextToken}`);
        originalRequest.headers = headers;
      }

      return authHttpClient(originalRequest);
    },
  );

  return () => {
    authHttpClient.interceptors.request.eject(requestInterceptorId);
    authHttpClient.interceptors.response.eject(responseInterceptorId);
  };
}
