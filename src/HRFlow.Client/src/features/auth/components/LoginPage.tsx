import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { useLocation, useNavigate } from 'react-router-dom';
import { useAuth } from '../hooks/useAuth.tsx';

const loginSchema = z.object({
  email: z.email('Please enter a valid email address.'),
  password: z.string().min(1, 'Password is required.'),
});

type LoginFormData = z.infer<typeof loginSchema>;

interface LoginLocationState {
  from?: {
    pathname?: string;
  };
}

/**
 * Sends credentials to the backend login endpoint and then returns users to the route they originally requested.
 */
export function LoginPage() {
  const navigate = useNavigate();
  const location = useLocation();
  const { login, isLoggingIn, isAuthenticated } = useAuth();
  const [authError, setAuthError] = useState<string | null>(null);

  const fromPath =
    (location.state as LoginLocationState | null)?.from?.pathname ?? '/';

  const form = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema),
  });

  useEffect(() => {
    if (isAuthenticated) {
      navigate(fromPath, { replace: true });
    }
  }, [fromPath, isAuthenticated, navigate]);

  const onSubmit = form.handleSubmit(async (values) => {
    setAuthError(null);

    try {
      await login(values);
      navigate(fromPath, { replace: true });
    } catch {
      setAuthError('Login failed. Please check your credentials and try again.');
    }
  });

  return (
    <main className="mx-auto mt-16 w-full max-w-md rounded-2xl border border-slate-200 bg-white p-8 shadow-sm">
      <h1 className="text-2xl font-bold text-slate-900">Sign in to HRFlow</h1>
      <p className="mt-2 text-sm text-slate-600">
        Use your account to access protected HR pages.
      </p>

      <form className="mt-6 space-y-4" onSubmit={onSubmit}>
        <label className="block text-sm font-medium text-slate-700" htmlFor="email">
          Email
        </label>
        <input
          id="email"
          type="email"
          autoComplete="email"
          className="w-full rounded-lg border border-slate-300 px-3 py-2 text-slate-900 outline-none focus:border-slate-500"
          {...form.register('email')}
        />
        {form.formState.errors.email ? (
          <p className="text-sm text-rose-600">{form.formState.errors.email.message}</p>
        ) : null}

        <label className="block text-sm font-medium text-slate-700" htmlFor="password">
          Password
        </label>
        <input
          id="password"
          type="password"
          autoComplete="current-password"
          className="w-full rounded-lg border border-slate-300 px-3 py-2 text-slate-900 outline-none focus:border-slate-500"
          {...form.register('password')}
        />
        {form.formState.errors.password ? (
          <p className="text-sm text-rose-600">{form.formState.errors.password.message}</p>
        ) : null}

        {authError ? <p className="text-sm text-rose-600">{authError}</p> : null}

        <button
          type="submit"
          disabled={isLoggingIn}
          className="w-full rounded-lg bg-slate-900 px-4 py-2 font-semibold text-white transition hover:bg-slate-700 disabled:cursor-not-allowed disabled:bg-slate-400"
        >
          {isLoggingIn ? 'Signing in...' : 'Sign in'}
        </button>

        <p className="text-xs text-slate-500">Dev credentials: see README.</p>
      </form>
    </main>
  );
}
