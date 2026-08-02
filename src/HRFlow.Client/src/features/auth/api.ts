import axios, {
  AxiosError,
  AxiosHeaders,
  type InternalAxiosRequestConfig,
} from 'axios';
import type {
  LoginRequest,
  RefreshTokenRequest,
  TokenResponse,
} from './types.ts';

const apiBaseUrl =
  import.meta.env.VITE_API_BASE_URL ?? 'http://127.0.0.1:5250/api/v1';

interface RetryableRequestConfig extends InternalAxiosRequestConfig {
  _retryOnce?: boolean;
}

export interface AuthInterceptorControls {
  getAccessToken: () => string | null;
  refreshAccessToken: () => Promise<string | null>;
  clearSession: () => void;
}

export const authHttpClient = axios.create({
  baseURL: apiBaseUrl,
  headers: {
    'Content-Type': 'application/json',
  },
});

const refreshHttpClient = axios.create({
  baseURL: apiBaseUrl,
  headers: {
    'Content-Type': 'application/json',
  },
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
 * Adds bearer injection and a single 401 retry; retry is skipped for /auth/refresh to avoid loops.
 */
export function configureAuthInterceptors(
  controls: AuthInterceptorControls,
): () => void {
  let refreshInFlightPromise: Promise<string> | null = null;

  const getSharedRefreshPromise = () => {
    if (!refreshInFlightPromise) {
      refreshInFlightPromise = (async () => {
        const refreshedToken = await controls.refreshAccessToken();
        if (!refreshedToken) {
          throw new Error('Refresh endpoint did not return a new access token.');
        }

        return refreshedToken;
      })()
        .catch((refreshError) => {
          controls.clearSession();
          throw refreshError;
        })
        .finally(() => {
          refreshInFlightPromise = null;
        });
    }

    return refreshInFlightPromise;
  };

  const requestInterceptorId = authHttpClient.interceptors.request.use(
    (config) => {
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
  );

  const responseInterceptorId = authHttpClient.interceptors.response.use(
    (response) => response,
    async (error: AxiosError) => {
      const originalRequest = error.config as RetryableRequestConfig | undefined;
      if (!originalRequest || error.response?.status !== 401) {
        return Promise.reject(error);
      }

      const isRefreshRequest = originalRequest.url?.includes('/auth/refresh');
      if (isRefreshRequest || originalRequest._retryOnce) {
        controls.clearSession();
        return Promise.reject(error);
      }

      originalRequest._retryOnce = true;
      let nextToken: string;
      try {
        nextToken = await getSharedRefreshPromise();
      } catch {
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
