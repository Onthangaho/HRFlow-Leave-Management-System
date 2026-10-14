import { useEffect, useRef, useState } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { authHttpClient } from '../auth/api';
import { useAuth } from '../auth/hooks/useAuth';
import { ConfirmationDialog } from '../../components/ConfirmationDialog';
import { problemMessage, isConflict } from '../leave-configuration/problems';

interface ImageMetadata { version: string; hasImage: boolean }

/** Authenticated blobs are scoped to a replacement-session generation and never become public image URLs. */
export function PrivateAvatar({ employeeId, fallback, className = '' }: { employeeId?: string; fallback: string; className?: string }) {
  const { user, sessionVersion } = useAuth();
  const key = `${user?.id}:${sessionVersion}:${employeeId ?? 'me'}`;
  const allowed = Boolean(user && (!employeeId || user.roles.includes('HR Administrator')));
  const query = useQuery({ queryKey: ['profile-image-content', user?.id, sessionVersion, employeeId ?? 'me'],
    queryFn: async ({ signal }) => (await authHttpClient.get<Blob>(employeeId ? `/employees/${employeeId}/image` : '/me/image/content', { signal, responseType: 'blob' })).data,
    enabled: allowed, retry: false, refetchOnMount: 'always' });
  const [image, setImage] = useState<{ key: string; url: string }>();
  useEffect(() => {
    if (!query.data || query.isError) return;
    const url = URL.createObjectURL(query.data); setImage({ key, url });
    return () => URL.revokeObjectURL(url);
  }, [query.data, query.isError, key]);
  return allowed && image?.key === key && !query.isError ? <img className={`rounded-full object-cover ${className}`} src={image.url} alt="Profile" />
    : <span className={`inline-flex items-center justify-center rounded-full bg-indigo-100 font-bold text-indigo-800 ${className}`} aria-label="Profile initials">{fallback}</span>;
}

/** A keyboard-operable square crop retains the committed image until an explicit replacement succeeds. */
export function ProfileImageEditor() {
  const { user, sessionVersion, getSessionVersion } = useAuth();
  const cache = useQueryClient();
  const meta = useQuery({ queryKey: ['profile-image', user?.id, sessionVersion], queryFn: async ({ signal }) =>
    (await authHttpClient.get<ImageMetadata>('/me/image', { signal })).data, enabled: Boolean(user), retry: false });
  const [snapshot, setSnapshot] = useState<ImageMetadata>();
  const [file, setFile] = useState<File>();
  const [preview, setPreview] = useState('');
  const [dimensions, setDimensions] = useState({ width: 0, height: 0 });
  const [zoom, setZoom] = useState(1);
  const [horizontal, setHorizontal] = useState(50);
  const [vertical, setVertical] = useState(50);
  const [saving, setSaving] = useState(false);
  const [progress, setProgress] = useState(0);
  const [message, setMessage] = useState('');
  const [conflict, setConflict] = useState(false);
  const [confirmRemove, setConfirmRemove] = useState(false);
  const controller = useRef<AbortController | undefined>(undefined);
  const busy = useRef(false);
  const removeButton = useRef<HTMLButtonElement>(null);
  const reloadButton = useRef<HTMLButtonElement>(null);
  const fileInput = useRef<HTMLInputElement>(null);
  const current = () => getSessionVersion() === sessionVersion;
  useEffect(() => { if (!snapshot && meta.data) setSnapshot(meta.data); }, [meta.data, snapshot]);
  useEffect(() => () => { controller.current?.abort(); }, [sessionVersion]);
  useEffect(() => {
    if (!file) { setPreview(''); return; }
    const url = URL.createObjectURL(file); setPreview(url);
    return () => URL.revokeObjectURL(url);
  }, [file]);
  const refresh = async () => {
    const result = await meta.refetch();
    if (current() && !result.isError && result.data) {
      setSnapshot(result.data); setConflict(false); setMessage('Image version reloaded. Your selected file and crop are retained; review before uploading.');
      await cache.invalidateQueries({ queryKey: ['profile-image-content', user?.id, sessionVersion] });
    }
    else if (current() && result.isError) setMessage(`Image could not be reloaded. ${problemMessage(result.error)} Your selected file and crop are retained.`);
  };
  const complete = async () => {
    await Promise.all([cache.invalidateQueries({ queryKey: ['profile-image-content', user?.id, sessionVersion] }), cache.invalidateQueries({ queryKey: ['profile-image', user?.id, sessionVersion] })]);
    if (current()) { const result = await meta.refetch(); if (current() && !result.isError && result.data) { setSnapshot(result.data); return true; } }
    return false;
  };
  const upload = async () => {
    if (busy.current || !file || !snapshot || conflict || !dimensions.width) return;
    busy.current = true; setSaving(true); setProgress(0); setMessage(''); controller.current = new AbortController();
    const side = Math.min(dimensions.width, dimensions.height) / zoom;
    const form = new FormData(); form.append('file', file); form.append('expectedVersion', snapshot.version);
    form.append('x', String((dimensions.width - side) * horizontal / 100 / dimensions.width));
    form.append('y', String((dimensions.height - side) * vertical / 100 / dimensions.height));
    form.append('size', String(1 / zoom));
    try {
      const metadata = (await authHttpClient.put<ImageMetadata>('/me/image', form, { headers: { 'Content-Type': undefined }, signal: controller.current.signal, skipAuthReplay: true,
        onUploadProgress: event => { if (current()) setProgress(Math.round((event.loaded / (event.total ?? file.size)) * 100)); } })).data;
      if (!current()) return;
      setSnapshot(metadata);
      await complete(); if (current()) { setFile(undefined); if (fileInput.current) fileInput.current.value = ''; setMessage('Profile image updated.'); }
    } catch (error) { if (current()) { setMessage(controller.current?.signal.aborted ? 'Upload cancelled. It may already have completed; reload the image version before retrying.' : problemMessage(error)); setConflict(isConflict(error) || Boolean(controller.current?.signal.aborted)); } }
    finally { if (current()) { busy.current = false; setSaving(false); } }
  };
  const remove = async () => {
    if (busy.current || !snapshot) return;
    busy.current = true; setSaving(true); setMessage(''); controller.current = new AbortController();
    try {
      await authHttpClient.delete('/me/image', { params: { expectedVersion: snapshot.version }, signal: controller.current.signal, skipAuthReplay: true });
      if (!current()) return;
      const refreshed = await complete();
      if (current()) { setConfirmRemove(false); setConflict(!refreshed); setMessage(refreshed ? 'Profile image removed.' : 'Profile image removed. Reload its version before another change.'); }
    } catch (error) { if (current()) { setMessage(problemMessage(error)); setConflict(isConflict(error)); } }
    finally { if (current()) { busy.current = false; setSaving(false); } }
  };
  const side = Math.min(dimensions.width, dimensions.height) / zoom;
  return <section className="ui-panel space-y-4"><h2 className="text-xl font-bold">Profile image</h2>
    <PrivateAvatar fallback={user?.email?.slice(0, 2).toUpperCase() ?? 'HF'} className="h-20 w-20" />
    {meta.isPending && <p role="status">Loading image information…</p>}
    <p id="profile-image-help" className="text-sm text-slate-600">JPEG or PNG, up to 2 MiB, 4096 pixels per side and 4 megapixels. A square 256×256 image is privately stored after processing and malware scanning. HR can view it; other employees cannot. Do not upload medical evidence or supporting documents here.</p>
    <label className="ui-label block">Choose an image<input ref={fileInput} aria-describedby="profile-image-help profile-image-feedback" className="mt-2 block w-full rounded-lg border border-slate-300 p-2 text-sm" type="file" accept="image/jpeg,image/png" disabled={saving} onChange={event => {
      const selected = event.target.files?.[0]; if (!selected) return;
      if (selected.size > 2 * 1024 * 1024 || !['image/jpeg', 'image/png'].includes(selected.type)) { setMessage('Choose a JPEG or PNG up to 2 MiB.'); return; }
      setFile(selected); setDimensions({ width: 0, height: 0 }); setZoom(1); setHorizontal(50); setVertical(50);
    }} /></label>
    {preview && <><div className="relative w-64 max-w-full overflow-hidden rounded-lg border" style={{ aspectRatio: '1' }}>
      <img src={preview} alt="Selected image crop preview" onLoad={event => setDimensions({ width: event.currentTarget.naturalWidth, height: event.currentTarget.naturalHeight })}
        style={{ maxWidth: 'none', width: dimensions.width && side ? `${dimensions.width / side * 100}%` : '100%', transform: `translate(-${(dimensions.width - side) * horizontal / 100 / dimensions.width * 100}%, -${(dimensions.height - side) * vertical / 100 / dimensions.height * 100}%)` }} />
    </div>
      <p className="text-sm text-slate-600">Use the arrow keys on each slider to adjust the square crop. Camera and location metadata are removed.</p>
      <label className="ui-label block">Crop zoom<input className="mt-2 block w-full" type="range" min="1" max={Math.max(1, Math.min(4, dimensions.width, dimensions.height))} step="0.1" value={zoom} disabled={saving} onChange={e => setZoom(Number(e.target.value))} /></label>
      <label className="ui-label block">Horizontal crop position<input className="mt-2 block w-full" type="range" min="0" max="100" value={horizontal} disabled={saving} onChange={e => setHorizontal(Number(e.target.value))} /></label>
      <label className="ui-label block">Vertical crop position<input className="mt-2 block w-full" type="range" min="0" max="100" value={vertical} disabled={saving} onChange={e => setVertical(Number(e.target.value))} /></label>
    </>}
    <p id="profile-image-feedback" role="status" aria-live="polite">{message || (meta.isError ? problemMessage(meta.error) : '')}</p>
    {saving && <p role="status">{progress < 100 ? `Uploading ${progress}%` : 'Processing and scanning…'}</p>}
    <div className="flex flex-wrap gap-3"><button className="ui-primary" disabled={!file || !snapshot || !dimensions.width || saving || conflict} onClick={() => void upload()}>Upload image</button>
      {saving && <button className="ui-secondary" onClick={() => controller.current?.abort()}>Cancel upload</button>}
      <button ref={reloadButton} className="ui-secondary" disabled={saving} onClick={() => void refresh()}>Reload image version</button>
      <button ref={removeButton} className="ui-danger" disabled={!snapshot?.hasImage || saving || conflict} onClick={() => { setMessage(''); setConfirmRemove(true); }}>Remove image</button></div>
    {conflict && <p className="ui-warning">The image changed. Explicitly reload its version and review your retained crop before trying again.</p>}
    {confirmRemove && <ConfirmationDialog title="Remove your profile image?" confirmLabel="Remove image" busy={saving} confirmDisabled={conflict} returnFocus={conflict ? reloadButton.current : removeButton.current} onCancel={() => { if (!saving) setConfirmRemove(false); }} onConfirm={() => void remove()}>
      <p>Your account returns to initials. Leave evidence and history are unchanged.</p>
      {message && <p role="alert">{message}</p>}
      {conflict && <p>Cancel to review the current image and explicitly reload its version before trying again.</p>}
    </ConfirmationDialog>}
  </section>;
}
