import { useRef, useState, type FormEvent } from 'react';
import { Link } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../../auth/hooks/useAuth';
import { authHttpClient } from '../../auth/api';
import { problemMessage } from '../../leave-configuration/problems';
import type { TypeSelection } from '../../leave-configuration/types';
import { isConflict } from '../../leave-configuration/problems';
import { SupportingDocuments } from './SupportingDocuments';

/** Minimal personal submission connects HR-created types to real requests without duplicating policy calculations. */
export function RequestLeavePage() {
  const { user, sessionVersion, getSessionVersion } = useAuth();
  const account = useRef(user?.id); account.current = user?.id;
  const client = useQueryClient();
  const submitting = useRef(false);
  const [typeId, setTypeId] = useState('');
  const [reviewedType, setReviewedType] = useState<TypeSelection | null>(null);
  const [description, setDescription] = useState('');
  const [reviewError, setReviewError] = useState('');
  const [reviewNotice, setReviewNotice] = useState('');
  const [startDate, setStartDate] = useState('');
  const [endDate, setEndDate] = useState('');
  const [validation, setValidation] = useState('');
  const [success, setSuccess] = useState(false);
  const [documents, setDocuments] = useState<string[]>([]);
  const [documentsBusy, setDocumentsBusy] = useState(false);
  const types = useQuery({ queryKey: ['leave-types', user?.id ?? '', sessionVersion],
    enabled: Boolean(user?.id && user.roles.some(role => role === 'Employee' || role === 'Manager')),
    queryFn: async ({ signal }) => (await authHttpClient.get<TypeSelection[]>('/leave-types', { signal })).data,
    staleTime: 0, refetchOnMount: 'always',
  });
  const submit = useMutation({
    onSettled: () => { submitting.current = false; },
    mutationFn: async (values: { accountId: string; leaveTypeId: string; startDate: string; endDate: string }) => {
      if (account.current !== values.accountId || getSessionVersion() !== sessionVersion) throw new Error('The session changed.');
      return (await authHttpClient.post<string>('/leave-requests', { leaveTypeId: values.leaveTypeId,
        startDate: `${values.startDate}T00:00:00`, endDate: `${values.endDate}T00:00:00`, documentIds: documents, description: description.trim() || null, expectedTypeVersion: reviewedType?.version, expectedPolicyVersion: reviewedType?.policyVersion }, { skipAuthReplay: true })).data;
    },
    onSuccess: async (_, values) => {
      if (account.current !== values.accountId || getSessionVersion() !== sessionVersion) return;
      setSuccess(true);
      await Promise.all(['employee-leave-history', 'leave-balances', 'pending-leave-requests', 'organisation-pending-leave-requests', 'managed-leave-types', 'leave-policies', 'department-leave-report', 'leave-request-timeline']
        .map(key => client.invalidateQueries({ queryKey: [key, values.accountId] })));
    },
  });
  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    if (submitting.current || documentsBusy || types.isFetching || !user?.id || success) return;
    if (!typeId || !types.data?.some(type => type.id === typeId)) { setValidation('Choose an available leave type.'); return; }
    if (!reviewedType || reviewedType.id !== typeId) { setValidation('Review this leave type before submitting.'); return; }
    if (description.trim().length > 1000 || (reviewedType.descriptionMode === 'Required' && !description.trim())) { setValidation('Enter the required description, up to 1000 trimmed characters. Do not include diagnoses.'); return; }
    if (reviewedType.descriptionMode === 'NotRequested' && description.trim()) { setValidation('This type does not request a description. Clear the retained description explicitly before submitting.'); return; }
    if ((reviewedType.evidenceMode === 'NotRequested' && documents.length) || (reviewedType.evidenceMode === 'Required' && !documents.length)) { setValidation(reviewedType.evidenceMode === 'Required' ? 'Select at least one clean document.' : 'Deselect retained evidence explicitly before submitting; uploaded drafts are preserved.'); return; }
    if (!startDate || !endDate || endDate < startDate) { setValidation('Choose an end date on or after the start date.'); return; }
    setValidation(''); submitting.current = true; submit.mutate({ accountId: user.id, leaveTypeId: typeId, startDate, endDate });
  };
  return <div className="workspace-page space-y-6">
    <section className="page-intro ui-panel"><p className="mt-3 text-sm leading-6 text-slate-600">Choose your leave type and dates. Calendar days include both the start and end date, including weekends. Pending requests do not reserve balance.</p></section>
    {success ? <section className="ui-panel" role="status"><h2 className="text-xl font-bold text-emerald-800">Request submitted</h2><p className="mt-2 text-slate-600">Your request is Pending and ready for your manager to review.</p><Link to="/leave-requests/history" className="mt-5 inline-flex ui-primary">View request and balances</Link><button className="mt-5 ml-3 ui-secondary" onClick={() => { setSuccess(false); setDocuments([]); setDocumentsBusy(false); setTypeId(''); setReviewedType(null); setDescription(''); setStartDate(''); setEndDate(''); submit.reset(); void types.refetch(); }}>Request more leave</button></section>
      : <form className="ui-panel space-y-5" onSubmit={onSubmit}>
        {types.isFetching && <p role="status">Refreshing available leave types…</p>}
        {types.error && <div role="alert" className="ui-error">{problemMessage(types.error)} <button type="button" className="underline" onClick={() => void types.refetch()}>Try again</button></div>}
        {!types.isPending && !types.error && types.data?.length === 0 && <p role="status" className="ui-warning">No leave types are available. Contact HR to set up a policy and leave type.</p>}
        <fieldset disabled={submit.isPending || documentsBusy || types.isFetching || !!types.error} className="space-y-5">
          <label className="ui-label">Leave type<select aria-label="Leave type" className="ui-input" value={typeId} onChange={e => { setTypeId(e.target.value); setReviewedType(types.data?.find(type => type.id === e.target.value) ?? null); submit.reset(); setValidation(''); setReviewError(''); }} required><option value="">Choose a leave type</option>{types.data?.map(type => <option key={type.id} value={type.id}>{type.name}</option>)}</select></label>
          <div className="grid gap-5 sm:grid-cols-2"><label className="ui-label">Start date<input className="ui-input" type="date" required value={startDate} onChange={e => setStartDate(e.target.value)} /></label>
            <label className="ui-label">End date<input className="ui-input" type="date" required min={startDate || undefined} value={endDate} onChange={e => setEndDate(e.target.value)} /></label></div>
          {reviewedType && <div className="space-y-4 rounded-xl border p-4">
            <p className="text-sm">Company submission requirements: description {reviewedType.descriptionMode === 'NotRequested' ? 'not requested' : reviewedType.descriptionMode.toLowerCase()}; evidence {reviewedType.evidenceMode === 'NotRequested' ? 'not requested' : reviewedType.evidenceMode.toLowerCase()} ({reviewedType.evidenceClass}). Medical content is private to you and authorised HR. Absence reporting is separate from later proof for payment.</p>
            {reviewedType.requirementInstructions && <p className="whitespace-pre-wrap break-words">{reviewedType.requirementInstructions}</p>}
            {(reviewedType.descriptionMode !== 'NotRequested' || description) && <label className="ui-label">Description {reviewedType.descriptionMode === 'Required' ? '(required)' : reviewedType.descriptionMode === 'NotRequested' ? '(not requested; clear retained draft)' : '(optional)'}<textarea className="ui-input" rows={4} maxLength={1000} value={description} onChange={e => setDescription(e.target.value)} required={reviewedType.descriptionMode === 'Required'} aria-describedby="description-help" /></label>}
            <p id="description-help" className="text-sm">{description.trim().length}/1000 trimmed characters. Use plain text. Do not include diagnoses or sensitive medical details. Dates, descriptions and uploaded drafts are retained when changing types; review selections before submitting.</p>
          </div>}
        </fieldset>
        <SupportingDocuments disabled={submit.isPending || !reviewedType} configuredClass={reviewedType?.evidenceClass} selectionDisabled={reviewedType?.evidenceMode === 'NotRequested'} onSelection={(ids, busy) => { setDocuments(ids); setDocumentsBusy(busy); }} />
        {reviewedType?.evidenceMode === 'NotRequested' && <p role="status">Evidence is not requested. Deselect any retained attachments explicitly. Drafts are not deleted.</p>}
        {documentsBusy && <p role="status">Finish document processing before submitting.</p>}
        {validation && <p role="alert" className="ui-error">{validation}</p>}
        {submit.error && <div role="alert" className="ui-error">{problemMessage(submit.error)}<button type="button" className="mt-2 block underline" disabled={types.isFetching} onClick={async () => {
          const result = await types.refetch(); if (getSessionVersion() !== sessionVersion) return;
          const current = result.data?.find(type => type.id === typeId);
          if (result.error || !current) { setReviewError('Unable to reload this type. Your draft is retained.'); return; }
          setReviewedType(current); submit.reset(); setReviewError(''); setValidation(''); setReviewNotice('Current requirements loaded. Review them and your retained description and documents before submitting again.');
        }}>{isConflict(submit.error) ? 'Reload and review changed requirements' : 'Refresh and review requirements'}</button></div>}
        {reviewError && <p role="alert" className="ui-error">{reviewError}</p>}
        {reviewNotice && <p role="status" className="ui-warning">{reviewNotice}</p>}
        <div className="flex flex-wrap gap-3"><button type="submit" className="ui-primary" disabled={submit.isPending || documentsBusy || types.isFetching || !!types.error || !types.data?.length}>{submit.isPending ? 'Submitting…' : 'Submit request'}</button><Link to="/leave-requests/history" className="ui-secondary">My leave</Link></div>
      </form>}
  </div>;
}
