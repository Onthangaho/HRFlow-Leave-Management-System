import { useEffect, useRef, useState } from 'react';
import { useAuth } from '../../auth/hooks/useAuth';
import { authHttpClient } from '../../auth/api';
import { problemMessage } from '../../leave-configuration/problems';
import type { TypeSelection } from '../../leave-configuration/types';

interface Preview {
  capturedAtUtc: string; leaveTypeId: string; startDate: string; endDate: string; unit: string; submissionDateBasis: string;
  requestedDays: number | null; entitledDays: number; approvedUsedDays: number; remainingDays: number;
  projectedRemainingDaysIfApproved: number | null;
  requirements: { typeVersion: string; policyVersion: string; descriptionMode: string; evidenceMode: string; evidenceClass: string; instructions: string | null };
  findings: { code: string; message: string }[];
}
type Result = { key: string; loading?: boolean; data?: Preview; error?: string };

/** Explicit read-only assessment keeps its captured inputs separate from live form drafts and never reviews submission versions. */
export function LeaveRequestPreview({ type, startDate, endDate, disabled, onReviewRequirements }: {
  type: TypeSelection | null; startDate: string; endDate: string; disabled: boolean; onReviewRequirements: () => Promise<void>;
}) {
  const { user, sessionVersion, getSessionVersion } = useAuth();
  const key = JSON.stringify([user?.id, sessionVersion, type?.id, type?.version, type?.policyVersion, startDate, endDate]);
  const current = useRef(key); current.current = key;
  const controller = useRef<AbortController | null>(null);
  const sequence = useRef(0);
  const summary = useRef<HTMLDivElement>(null);
  const [result, setResult] = useState<Result | null>(null);
  useEffect(() => () => { sequence.current++; controller.current?.abort(); }, [key]);
  const visible = result?.key === key ? result : null;
  useEffect(() => { if (visible && !visible.loading) summary.current?.focus(); }, [visible]);

  async function preview() {
    if (!type || !user || visible?.loading) return;
    controller.current?.abort();
    const abort = new AbortController(); controller.current = abort;
    const attempt = ++sequence.current;
    const eligible = () => !abort.signal.aborted && current.current === key && sequence.current === attempt && getSessionVersion() === sessionVersion;
    setResult({ key, loading: true });
    try {
      const response = await authHttpClient.post<Preview>('/leave-requests/preview', {
        leaveTypeId: type.id, startDate, endDate,
      }, { signal: abort.signal });
      if (eligible()) setResult({ key, data: response.data });
    } catch (error) {
      if (eligible()) setResult({ key, error: problemMessage(error) });
    }
  }

  return <section className="space-y-3 rounded-xl border p-4" aria-labelledby="preview-title">
    <h2 id="preview-title" className="text-lg font-semibold">Request preview</h2>
    <p className="text-sm">Check dates and current balance before submitting. Preview does not upload, scan or validate descriptions or evidence. Pending requests reserve no balance. Preview guarantees neither submission nor approval.</p>
    <button type="button" className="ui-secondary" disabled={disabled || !type || !startDate || !endDate || visible?.loading} onClick={() => void preview()}>
      {visible?.loading ? 'Previewing request…' : visible?.error ? 'Retry preview' : 'Preview request'}
    </button>
    {!result && <p className="text-sm">Choose a type and dates, then preview explicitly.</p>}
    {result && !visible && <p role="status" className="ui-warning">Previous preview is outdated. Preview the current inputs explicitly.</p>}
    {visible?.loading && <p role="status">Loading a current read-only assessment…</p>}
    {visible && !visible.loading && <div ref={summary} role="group" tabIndex={-1} className="space-y-3 outline-offset-4 focus:outline focus:outline-3 focus:outline-[color:var(--focus)]" aria-label="Request preview result">
      {visible.error && <p role="alert" className="ui-error">{visible.error} Your draft is retained. Retry preview explicitly.</p>}
      {visible.data && <>
        <p role="status">Preview captured for {visible.data.startDate} through {visible.data.endDate}, inclusive.</p>
        <dl className="grid gap-3 sm:grid-cols-2">
          <div><dt>Requested calendar days</dt><dd>{visible.data.requestedDays ?? 'Unavailable: correct the date range'}</dd></div>
          <div><dt>Current configured entitlement</dt><dd>{visible.data.entitledDays} calendar days</dd></div>
          <div><dt>Approved usage</dt><dd>{visible.data.approvedUsedDays} calendar days</dd></div>
          <div><dt>Current remaining balance</dt><dd>{visible.data.remainingDays} calendar days</dd></div>
          <div><dt>Projected remaining IF later approved</dt><dd>{visible.data.projectedRemainingDaysIfApproved === null ? 'Unavailable' : `${visible.data.projectedRemainingDaysIfApproved} calendar days`}</dd></div>
        </dl>
        <p className="text-sm">{visible.data.unit}. Current configured entitlement minus all Approved calendar days; no annual accrual or paid-leave promise. Submission date eligibility uses {visible.data.submissionDateBasis}.</p>
        {visible.data.findings.length > 0 ? <div className="ui-warning"><p>Correct these findings before submission:</p><ul className="list-disc pl-5">{visible.data.findings.map(f => <li key={f.code}>{f.message}</li>)}</ul></div>
          : <p>No date, manager or balance problems found at capture time. Description, evidence and all current rules are still checked on submission and approval.</p>}
        <p>Current requirements: description {visible.data.requirements.descriptionMode}; evidence {visible.data.requirements.evidenceMode} ({visible.data.requirements.evidenceClass}).</p>
        {visible.data.requirements.instructions && <p className="whitespace-pre-wrap break-words">{visible.data.requirements.instructions}</p>}
        {(visible.data.requirements.typeVersion !== type?.version || visible.data.requirements.policyVersion !== type?.policyVersion) && <div className="ui-warning"><p>Requirements changed since you selected this type. This preview has not refreshed your reviewed versions. Explicitly refresh and review requirements before submitting.</p><button type="button" className="ui-secondary mt-2" disabled={disabled} onClick={() => void onReviewRequirements()}>Refresh and review requirements</button></div>}
        <p className="text-sm">Captured {new Date(visible.data.capturedAtUtc).toISOString()} (UTC). History, permissions and policy may change afterward.</p>
      </>}
    </div>}
  </section>;
}
