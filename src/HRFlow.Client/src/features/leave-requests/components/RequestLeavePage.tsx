import { useRef, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../../auth/hooks/useAuth';
import { authHttpClient } from '../../auth/api';
import { problemMessage } from '../../leave-configuration/problems';

/** Minimal personal submission connects HR-created types to real requests without duplicating policy calculations. */
export function RequestLeavePage() {
  const { user } = useAuth();
  const account = useRef(user?.id); account.current = user?.id;
  const client = useQueryClient();
  const submitting = useRef(false);
  const [typeId, setTypeId] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [validation, setValidation] = useState('');
  const [success, setSuccess] = useState(false);
  const types = useQuery({ queryKey: ['leave-types', user?.id ?? ''],
    enabled: Boolean(user?.id && user.roles.some(role => role === 'Employee' || role === 'Manager')),
    queryFn: async ({ signal }) => (await authHttpClient.get<{ id: string; name: string }[]>('/leave-types', { signal })).data,
    staleTime: 0, refetchOnMount: 'always',
  });
  const submit = useMutation({
    onSettled: () => { submitting.current = false; },
    mutationFn: async (values: { accountId: string; leaveTypeId: string; startDate: string; endDate: string }) => {
      if (account.current !== values.accountId) throw new Error('The session changed.');
      return (await authHttpClient.post<string>('/leave-requests', { leaveTypeId: values.leaveTypeId,
        startDate: `${values.startDate}T00:00:00`, endDate: `${values.endDate}T00:00:00` })).data;
    },
    onSuccess: async (_, values) => {
      if (account.current !== values.accountId) return;
      setSuccess(true);
      await Promise.all(['employee-leave-history', 'leave-balances', 'pending-leave-requests', 'organisation-pending-leave-requests', 'managed-leave-types', 'leave-policies']
        .map(key => client.invalidateQueries({ queryKey: [key, values.accountId] })));
    },
  });
  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    if (submitting.current || types.isFetching || !user?.id || success) return;
    if (!typeId || !types.data?.some(type => type.id === typeId)) { setValidation('Choose an available leave type.'); return; }
    if (!startDate || !endDate || endDate < startDate) { setValidation('Choose an end date on or after the start date.'); return; }
    setValidation(''); submitting.current = true; submit.mutate({ accountId: user.id, leaveTypeId: typeId, startDate, endDate });
  };
  return <main className="mx-auto w-full max-w-2xl space-y-6 px-4 py-8 sm:px-6">
    <header className="ui-panel"><Link to="/" className="text-sm font-semibold text-indigo-700 underline">Back to dashboard</Link><h1 className="mt-5 text-3xl font-bold">Request leave</h1><p className="mt-3 text-sm leading-6 text-slate-600">Choose your leave type and dates. Calendar days include both the start and end date, including weekends. Pending requests do not reserve balance.</p></header>
    {success ? <section className="ui-panel" role="status"><h2 className="text-xl font-bold text-emerald-800">Request submitted</h2><p className="mt-2 text-slate-600">Your request is Pending and ready for your manager to review.</p><Link to="/leave-requests/history" className="mt-5 inline-flex ui-primary">View request and balances</Link><button className="mt-5 ml-3 ui-secondary" onClick={() => { setSuccess(false); setTypeId(''); setStartDate(''); setEndDate(''); submit.reset(); void types.refetch(); }}>Request more leave</button></section>
      : <form className="ui-panel space-y-5" onSubmit={onSubmit}>
        {types.isFetching && <p role="status">Refreshing available leave types…</p>}
        {types.error && <div role="alert" className="ui-error">{problemMessage(types.error)} <button type="button" className="underline" onClick={() => void types.refetch()}>Try again</button></div>}
        {!types.isPending && !types.error && types.data?.length === 0 && <p role="status" className="ui-warning">No leave types are available. Contact HR to set up a policy and leave type.</p>}
        <fieldset disabled={submit.isPending || types.isFetching || !!types.error} className="space-y-5">
          <label className="ui-label">Leave type<select aria-label="Leave type" className="ui-input" value={typeId} onChange={e => setTypeId(e.target.value)} required><option value="">Choose a leave type</option>{types.data?.map(type => <option key={type.id} value={type.id}>{type.name}</option>)}</select></label>
          <div className="grid gap-5 sm:grid-cols-2"><label className="ui-label">Start date<input className="ui-input" type="date" required value={startDate} onChange={e => setStartDate(e.target.value)} /></label>
            <label className="ui-label">End date<input className="ui-input" type="date" required min={startDate || undefined} value={endDate} onChange={e => setEndDate(e.target.value)} /></label></div>
        </fieldset>
        {validation && <p role="alert" className="ui-error">{validation}</p>}
        {submit.error && <div role="alert" className="ui-error">{problemMessage(submit.error)}<button type="button" className="mt-2 block underline" onClick={() => void types.refetch()}>Refresh available types</button></div>}
        <div className="flex flex-wrap gap-3"><button type="submit" className="ui-primary" disabled={submit.isPending || types.isFetching || !!types.error || !types.data?.length}>{submit.isPending ? 'Submitting…' : 'Submit request'}</button><Link to="/leave-requests/history" className="ui-secondary">My leave</Link></div>
      </form>}
  </main>;
}
