import { useEffect, useRef, type ReactNode } from 'react';
interface ConfirmationProps { title: string; children: ReactNode; confirmLabel: string; busy?: boolean; returnFocus?: HTMLElement | null; onCancel: () => void; onConfirm: () => void }
/** Native modal traps focus, supports Escape, starts on Cancel and restores the invoking control. */
export function ConfirmationDialog({ title, children, confirmLabel, busy, returnFocus, onCancel, onConfirm }: ConfirmationProps) {
  const dialog = useRef<HTMLDialogElement>(null);
  const cancel = useRef<HTMLButtonElement>(null);
  useEffect(() => {
    const opener = returnFocus ?? document.activeElement as HTMLElement | null;
    const element = dialog.current;
    element?.showModal();
    cancel.current?.focus();
    return () => { element?.close(); if (opener?.isConnected) opener.focus(); else document.querySelector<HTMLButtonElement>('[data-focus-fallback]')?.focus(); };
  }, [returnFocus]);
  return <dialog ref={dialog} aria-labelledby="confirmation-title" className="confirmation rounded-2xl border border-slate-200 bg-white p-6 shadow-xl"
    onKeyDown={event => {
      if (event.key !== 'Tab') return;
      const controls = Array.from(dialog.current?.querySelectorAll<HTMLElement>('button:not(:disabled), a[href], input:not(:disabled), select:not(:disabled), [tabindex="0"]') ?? []);
      const first = controls[0]; const last = controls[controls.length - 1];
      if (!first) { event.preventDefault(); return; }
      if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last?.focus(); }
      else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    }}
    onCancel={event => { event.preventDefault(); if (!busy) onCancel(); }}>
    <h2 id="confirmation-title" className="break-words text-xl font-bold text-slate-900">{title}</h2>
    <div className="my-4 space-y-3 text-sm leading-6 text-slate-600">{children}</div>
    <div className="flex flex-wrap justify-end gap-3">
      <button ref={cancel} type="button" className="ui-secondary" disabled={busy} onClick={onCancel}>Cancel</button>
      <button type="button" className="ui-danger" disabled={busy} onClick={onConfirm}>{busy ? 'Please wait…' : confirmLabel}</button>
    </div>
  </dialog>;
}
