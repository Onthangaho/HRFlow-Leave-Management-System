import { isAxiosError } from 'axios';
/** Uses only sanitized ProblemDetails, avoiding raw transport errors and sensitive payloads. */
export function problemMessage(error: unknown): string {
  if (!isAxiosError(error)) return 'Something went wrong. Please try again.';
  if (error.response?.status === 403) return 'Your account no longer has permission for this action. Contact HR or sign out and sign in again.';
  const data = error.response?.data as { detail?: string; errors?: Record<string, string[]> } | undefined;
  if (error.response?.status && error.response.status < 500) {
    return data?.detail || Object.values(data?.errors ?? {}).flat().join(' ') || 'This action could not be completed. Refresh the available records and try again.';
  }
  return 'Unable to reach this service. Your input has been kept. Please try again.';
}
/** Conflicts require deliberate recovery, not automatic replay of an old edit. */
export function isConflict(error: unknown): boolean {
  return isAxiosError(error) && error.response?.status === 409;
}
