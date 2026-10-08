import { useRef, useState } from 'react';
import { isAxiosError } from 'axios';
import { useQueryClient } from '@tanstack/react-query';
import { ConfirmationDialog } from '../../../components/ConfirmationDialog';
import { authHttpClient } from '../../auth/api';
import { useAuth } from '../../auth/hooks/useAuth';
import type { Employee } from '../types';
/** Original version and session guards prevent replay or stale invitation replacement. */
export function ResendActivationDialog({ employee, onClose, onSuccess }: { employee: Employee; onClose: () => void; onSuccess: (state: string) => void }) {
  const { user, sessionVersion, getSessionVersion } = useAuth();
  const query = useQueryClient();
  const saving = useRef(false);
  const [busy, setBusy] = useState(false);
  const [error, setError] = useState('');
  const submit = async () => {
    if (saving.current || !user || getSessionVersion() !== sessionVersion) return;
    saving.current = true; setBusy(true); setError('');
    try {
      const result = await authHttpClient.post('/employees/' + employee.id + '/activation/resend', { expectedVersion: employee.version }, { skipAuthReplay: true });
      if (getSessionVersion() !== sessionVersion) return;
      await query.invalidateQueries({ queryKey: ['employees', user.id] });
      if (getSessionVersion() === sessionVersion) onSuccess(result.data.invitationDeliveryState);
    } catch (failure) {
      if (getSessionVersion() === sessionVersion) setError(isAxiosError(failure) && typeof failure.response?.data?.detail === 'string' ? failure.response.data.detail : 'Unable to resend. Check delivery status before trying again.');
    } finally { saving.current = false; if (getSessionVersion() === sessionVersion) setBusy(false); }
  };
  return <ConfirmationDialog title="Resend activation invitation" confirmLabel="Resend invitation" neutral busy={busy} onCancel={onClose} onConfirm={() => void submit()}>
    <p className="break-words">Send a new invitation for <strong>{employee.fullName}</strong> ({employee.email})? The previous invitation will stop working. This does not change roles or active status.</p>
    <p className="mt-3 text-sm">Development invitations use private local pickup, not email.</p>
    {error && <div role="alert" className="mt-3 text-rose-800"><p>{error}</p><button type="button" disabled={busy} className="ui-secondary mt-2" onClick={() => { void query.invalidateQueries({ queryKey: ['employees', user?.id] }); onClose(); }}>Close and refresh employee status</button></div>}
  </ConfirmationDialog>;
}
