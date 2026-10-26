import fs from 'node:fs';
import path from 'node:path';
import net from 'node:net';
import { spawn, spawnSync, type ChildProcess } from 'node:child_process';
import { createHash, randomUUID } from 'node:crypto';
import { DatabaseSync } from 'node:sqlite';
import { test, expect } from '@playwright/test';

interface State { api: string; client: string; password: string; database: string; privateRoot: string;
  ordinaryRequest: string; medicalRequest: string; employees: Record<string, { email: string; id: string }> }

/** Inspect restricted runtime storage without putting private paths in child arguments, reports or assertion values. */
function privatePermissions(root: string, database: string) {
  if (process.platform === 'win32') {
    const result = spawnSync('powershell.exe', ['-NoProfile', '-NonInteractive', '-Command',
      "$ErrorActionPreference='Stop'; try { $v=([Console]::In.ReadToEnd()|ConvertFrom-Json); $allowed=@([Security.Principal.WindowsIdentity]::GetCurrent().User.Value,'S-1-5-18','S-1-5-32-544'); $items=@(Get-Item -LiteralPath $v.root)+@(Get-Item -LiteralPath $v.database)+@(Get-ChildItem -LiteralPath $v.root -Recurse -Force); foreach($item in $items){if($item.Attributes -band [IO.FileAttributes]::ReparsePoint){exit 2}; foreach($rule in (Get-Acl -LiteralPath $item.FullName).GetAccessRules($true,$true,[Security.Principal.SecurityIdentifier])){if($rule.AccessControlType -eq 'Allow' -and $allowed -notcontains $rule.IdentityReference.Value){exit 3}}}; exit 0 } catch {exit 4}"],
    { input: JSON.stringify({ root, database }), windowsHide: true, timeout: 10_000, encoding: 'utf8' });
    expect(result.status).toBe(0);
  } else {
    const visit = (entry: string) => {
      const stat = fs.lstatSync(entry); expect(stat.isSymbolicLink()).toBe(false); expect(stat.mode & 0o077).toBe(0);
      if (stat.isDirectory()) for (const child of fs.readdirSync(entry)) visit(path.join(entry, child));
    }; visit(root); visit(database);
  }
}

/** Real scanner opt-in only: complete PDF structure, harmless text, no external file or medical content. */
function pdf(testSignature = false) {
  const content = 'BT /F1 14 Tf 40 740 Td (Synthetic evidence only.) Tj ET';
  // Standard harmless EICAR string is assembled only in memory; never output or create a fixture artifact.
  const signature = ['X5O!P%@AP[4', '\\PZX54(P^)7CC)7}', '$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*'].join('');
  const objects = [testSignature ? '<< /Type /Catalog /Pages 2 0 R /Names << /EmbeddedFiles << /Names [(test.txt) 6 0 R] >> >> >>' : '<< /Type /Catalog /Pages 2 0 R >>', '<< /Type /Pages /Kids [3 0 R] /Count 1 >>',
    '<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>',
    '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>', `<< /Length ${Buffer.byteLength(content)} >>\nstream\n${content}\nendstream`];
  if (testSignature) objects.push('<< /Type /Filespec /F (test.txt) /EF << /F 7 0 R >> >>',
    `<< /Type /EmbeddedFile /Length ${signature.length} >>\nstream\n${signature}\nendstream`);
  let text = '%PDF-1.4\n'; const offsets: number[] = [];
  for (const [i, object] of objects.entries()) { offsets.push(Buffer.byteLength(text)); text += `${i + 1} 0 obj\n${object}\nendobj\n`; }
  const xref = Buffer.byteLength(text);
  text += `xref\n0 ${objects.length + 1}\n0000000000 65535 f \n` + offsets.map(n => String(n).padStart(10, '0') + ' 00000 n \n').join('');
  return Buffer.from(text + `trailer\n<< /Size ${objects.length + 1} /Root 1 0 R >>\nstartxref\n${xref}\n%%EOF\n`, 'ascii');
}

/** Owns only the explicitly configured daemon; readiness uses actual loopback PING, never a clean-verdict substitute. */
class Scanner {
  private child?: ChildProcess;
  private scratch?: string;
  private scratchParent?: string;
  private removedScratch: string[] = [];
  private protectedFiles = new Map<string, string>();
  /** Refuses occupied ports and waits for the real engine to finish loading its official databases. */
  async start() {
    if (!process.env.HRFLOW_REAL_CLAMD || !process.env.HRFLOW_REAL_CLAMD_CONFIG) throw new Error('Real scanner configuration required.');
    const port = Number(process.env.HRFLOW_TEST_CLAMAV_PORT);
    if (!Number.isInteger(port) || port < 1024 || port > 65535) throw new Error('Explicit unprivileged scanner port required.');
    const config = fs.readFileSync(process.env.HRFLOW_REAL_CLAMD_CONFIG, 'utf8');
    const values = (option: string) => [...config.matchAll(new RegExp(`^[ \\t]*${option}[ \\t]+([^\\r\\n]+)`, 'gmi'))].map(m => m[1].trim());
    if (JSON.stringify(values('TCPAddr')) !== '["127.0.0.1"]' || JSON.stringify(values('TCPSocket')) !== JSON.stringify([String(port)])
      || JSON.stringify(values('Foreground')) !== '["yes"]' || /^[ \t]*Include\b/im.test(config)
      || values('LogFile').length || values('PidFile').length) throw new Error('Private loopback foreground scanner configuration required.');
    const definitions = values('DatabaseDirectory');
    if (definitions.length !== 1 || !path.isAbsolute(definitions[0])) throw new Error('Explicit private definition directory required.');
    privatePermissions(definitions[0], definitions[0]);
    const definitionFiles = fs.readdirSync(definitions[0]).filter(name => /^(daily|main|bytecode)\.(cvd|cld)$/.test(name));
    if (definitionFiles.length !== 3 || new Set(definitionFiles.map(name => name.split('.')[0])).size !== 3)
      throw new Error('Complete verified official definition set required.');
    for (const file of [process.env.HRFLOW_REAL_CLAMD, process.env.HRFLOW_REAL_CLAMD_CONFIG,
      ...definitionFiles.map(name => path.join(definitions[0], name))])
      this.protectedFiles.set(file, createHash('sha256').update(fs.readFileSync(file)).digest('hex'));
    await new Promise<void>((resolve, reject) => {
      const listener = net.createServer(); listener.once('error', () => reject(new Error('Scanner port is already occupied.')));
      listener.listen(port, '127.0.0.1', () => listener.close(() => resolve()));
    });
    const parent = path.resolve(path.dirname(process.env.HRFLOW_REAL_CLAMD_CONFIG));
    privatePermissions(parent, process.env.HRFLOW_REAL_CLAMD_CONFIG);
    this.scratchParent = parent;
    this.scratch = fs.mkdtempSync(path.join(parent, 'hrflow-evidence-scanner-'));
    if (process.platform !== 'win32') fs.chmodSync(this.scratch, 0o700);
    const privateConfig = path.join(this.scratch, 'clamd.conf');
    fs.writeFileSync(privateConfig, config.replace(/^[ \t]*TemporaryDirectory[ \t]+[^\r\n]*\r?$/gmi, '') + `\nTemporaryDirectory ${this.scratch}\n`, { flag: 'wx', mode: 0o600 });
    privatePermissions(this.scratch, privateConfig);
    this.child = spawn(process.env.HRFLOW_REAL_CLAMD, ['--config-file=' + privateConfig],
      { stdio: 'ignore', windowsHide: true, env: { ...process.env, TMPDIR: this.scratch, TMP: this.scratch, TEMP: this.scratch } });
    let failed = false; this.child.once('error', () => { failed = true; });
    const deadline = Date.now() + 45_000;
    while (Date.now() < deadline && !failed && this.child.exitCode === null) {
      if (await new Promise<boolean>(resolve => {
        const socket = net.connect({ host: '127.0.0.1', port: Number(process.env.HRFLOW_TEST_CLAMAV_PORT) });
        let reply = ''; let settled = false;
        const finish = (ok: boolean) => { if (!settled) { settled = true; socket.destroy(); resolve(ok); } };
        socket.setTimeout(500, () => finish(false)); socket.on('error', () => finish(false));
        socket.on('connect', () => socket.write('zPING\0'));
        socket.on('data', b => { reply += b.toString(); if (reply.includes('\0')) finish(reply === 'PONG\0'); });
        socket.on('close', () => finish(false));
      })) return;
      await new Promise(resolve => setTimeout(resolve, 100)); // Bounded readiness, not workflow synchronization.
    }
    throw new Error('Real scanner readiness failed.');
  }
  /** Waits for this owned foreground daemon to exit before API fixture teardown may delete private files. */
  async stop() {
    const child = this.child;
    if (child && child.exitCode === null && child.signalCode === null) {
      const exited = new Promise<void>(resolve => child.once('exit', () => resolve()));
      child.kill();
      let timer: ReturnType<typeof setTimeout> | undefined;
      try { await Promise.race([exited, new Promise<never>((_, reject) => { timer = setTimeout(() => reject(new Error('Owned scanner shutdown failed.')), 5000); })]); }
      finally { clearTimeout(timer); }
    }
    this.child = undefined;
    if (this.scratch) {
      privatePermissions(this.scratch, path.join(this.scratch, 'clamd.conf'));
      if (!path.isAbsolute(this.scratch) || path.dirname(this.scratch) !== this.scratchParent
        || !path.basename(this.scratch).startsWith('hrflow-evidence-scanner-') || fs.lstatSync(this.scratch).isSymbolicLink()) throw new Error('Unsafe scanner scratch cleanup.');
      fs.rmSync(this.scratch, { recursive: true }); this.removedScratch.push(this.scratch); this.scratch = undefined;
    }
    this.assertStopped();
  }
  /** Proves owned scratch is gone while operator engine/configuration/definitions remain byte-identical. */
  assertStopped() {
    expect(this.child === undefined && this.scratch === undefined).toBe(true);
    for (const removed of this.removedScratch) expect(fs.existsSync(removed)).toBe(false);
    for (const [file, checksum] of this.protectedFiles)
      expect(createHash('sha256').update(fs.readFileSync(file)).digest('hex')).toBe(checksum);
  }
}

test('Real ClamAV supplemental evidence: Ordinary, Medical, outage retry, EICAR and persisted invariants', async ({ page }) => {
  test.setTimeout(180_000); // Includes two real database-loaded scanner startups; normal CI never selects this opt-in.
  const f = JSON.parse(fs.readFileSync(process.env.HRFLOW_TEST_STATE!, 'utf8')) as State;
  const db = new DatabaseSync(f.database, { readOnly: true });
  // Inspect alongside real API writers with the same bounded lock budget as the existing fixture connections.
  db.exec('PRAGMA busy_timeout=3000');
  const scanner = new Scanner();
  const row = (sql: string, id: string) => db.prepare(sql).get(id.toUpperCase())!;
  const before = (id: string) => JSON.stringify({ request: row('SELECT * FROM LeaveRequests WHERE Id=?', id),
    audits: db.prepare('SELECT * FROM AuditEntries WHERE LeaveRequestId=? ORDER BY Id').all(id.toUpperCase()) });
  const original = [before(f.ordinaryRequest), before(f.medicalRequest)];
  const loginApi = async (actor: string) => {
    const result = await page.request.post(f.api + '/api/v1/auth/login', { data: { email: f.employees[actor].email, password: f.password } });
    expect(result.status()).toBe(200); return { Authorization: 'Bearer ' + (await result.json()).accessToken };
  };
  try {
    await scanner.start();
    await page.goto(f.client + '/login');
    await page.getByLabel('Email', { exact: true }).fill(f.employees.employee.email);
    await page.getByLabel('Password', { exact: true }).fill(f.password);
    await page.getByRole('button', { name: 'Sign in', exact: true }).click();
    const owner = await loginApi('employee'); const hr = await loginApi('hr'); const manager = await loginApi('manager');
    const bytes = pdf(); const checksum = createHash('sha256').update(bytes).digest('hex').toUpperCase();
    const open = async (id: string) => {
      await page.getByRole('navigation', { name: 'Primary', exact: true }).getByRole('link', { name: 'My leave and balances', exact: true }).click();
      await page.locator(`a[href="/leave-requests/${id}/history"]`).click();
      const area = page.getByRole('region', { name: 'Later evidence additions', exact: true });
      await area.getByRole('button', { name: 'Add evidence', exact: true }).click(); return area;
    };
    for (const [index, id] of [f.ordinaryRequest, f.medicalRequest].entries()) {
      const area = await open(id);
      if (index === 0) await area.getByLabel('Evidence class', { exact: true }).selectOption('Ordinary');
      const uploaded = page.waitForResponse(r => r.url().endsWith('/documents') && r.request().method() === 'POST');
      await area.getByLabel('Choose document', { exact: true }).setInputFiles({ name: 'synthetic.pdf', mimeType: 'application/pdf', buffer: bytes });
      const response = await uploaded; expect(response.status()).toBe(200);
      const document = await response.json(); expect(document.status).toBe('Clean');
      const persisted = row('SELECT * FROM SupportingDocuments WHERE Id=?', document.id);
      expect(persisted.Status).toBe('Clean'); expect(persisted.Checksum).toBe(checksum); expect(persisted.ScannedUtc !== null).toBe(true);
      expect(persisted.OwnerId).toBe(f.employees.employee.id.toUpperCase());
      await area.getByRole('checkbox', { name: 'Attach to request', exact: true }).check();
      const bound = page.waitForResponse(r => r.url().endsWith(`/requests/${id}/additions`) && r.request().method() === 'POST');
      await area.getByRole('button', { name: 'Attach selected evidence', exact: true }).click(); expect((await bound).status()).toBe(200);
      await expect(area.getByText(/Evidence addition confirmed/)).toBeVisible();
      expect(row('SELECT count(*) AS n FROM EvidenceAdditions WHERE RequestId=?', id).n).toBe(1);
      expect(row('SELECT ActorId FROM EvidenceAdditions WHERE RequestId=?', id).ActorId).toBe(f.employees.employee.id.toUpperCase());
      expect(row('SELECT count(*) AS n FROM SupportingDocuments WHERE RequestId=?', id).n).toBe(1);
      expect(row("SELECT count(*) AS n FROM DocumentAccessEntries WHERE DocumentId=? AND Action='SupplementalBind'", document.id).n).toBe(1);
      expect(before(id)).toBe(original[index]);
      for (const headers of [owner, hr]) {
        const download = await page.request.get(f.api + `/api/v1/documents/${document.id}/download`, { headers });
        expect(download.status()).toBe(200); expect(createHash('sha256').update(await download.body()).digest('hex').toUpperCase()).toBe(checksum);
        expect(download.headers()['cache-control']).toBe('no-store'); expect(download.headers()['x-content-type-options']).toBe('nosniff');
      }
      const managerDownload = await page.request.get(f.api + `/api/v1/documents/${document.id}/download`, { headers: manager });
      expect(managerDownload.status()).toBe(index === 0 ? 200 : 404);
      if (index === 1) {
        const metadata = await page.request.get(f.api + `/api/v1/documents?requestId=${id}`, { headers: manager });
        expect(metadata.status()).toBe(200); const medical = (await metadata.json())[0];
        expect(medical.canDownload).toBe(false); expect(medical.mediaType).toBeNull();
      }
      const download = page.waitForEvent('download');
      await page.getByRole('region', { name: 'Supporting documents', exact: true }).getByRole('button', { name: 'Download', exact: true }).click();
      const received = await download;
      const stream = await received.createReadStream(); const chunks: Buffer[] = []; for await (const chunk of stream!) chunks.push(Buffer.from(chunk));
      expect(createHash('sha256').update(Buffer.concat(chunks)).digest('hex').toUpperCase()).toBe(checksum); await received.delete();
    }
    const area = await open(f.medicalRequest);
    await scanner.stop();
    const uploaded = page.waitForResponse(r => r.url().endsWith('/documents') && r.request().method() === 'POST');
    await area.getByLabel('Choose document', { exact: true }).setInputFiles({ name: 'synthetic.pdf', mimeType: 'application/pdf', buffer: bytes });
    const failedResponse = await uploaded; expect(failedResponse.status()).toBe(200); const failed = await failedResponse.json();
    expect(failed.status).toBe('ScanUnavailable');
    expect((await page.request.get(f.api + `/api/v1/documents/${failed.id}/download`, { headers: owner })).status()).toBe(404);
    const refused = await page.request.post(f.api + `/api/v1/documents/requests/${f.medicalRequest}/additions`, { headers: owner,
      data: { key: randomUUID(), documents: [{ id: failed.id, version: failed.version }] } }); expect(refused.status()).toBe(409);
    await scanner.start();
    const retry = page.waitForResponse(r => r.url().endsWith(`/documents/${failed.id}/scan`) && r.request().method() === 'POST');
    await area.getByRole('button', { name: 'Retry scan', exact: true }).click(); expect((await retry).status()).toBe(200);
    await area.getByRole('checkbox', { name: 'Attach to request', exact: true }).check();
    await area.getByRole('button', { name: 'Attach selected evidence', exact: true }).click();
    await expect(area.getByText(/Evidence addition confirmed/)).toBeVisible();
    expect(row('SELECT count(*) AS n FROM EvidenceAdditions WHERE RequestId=?', f.medicalRequest).n).toBe(2);
    expect(before(f.medicalRequest)).toBe(original[1]);
    const bad = await page.request.post(f.api + '/api/v1/documents', { headers: owner, multipart: {
      file: { name: 'synthetic.pdf', mimeType: 'application/pdf', buffer: pdf(true) }, classification: 'Ordinary', uploadKey: randomUUID() } });
    expect(bad.status()).toBe(200); const rejected = await bad.json(); expect(rejected.status).toBe('Rejected');
    expect((await page.request.get(f.api + `/api/v1/documents/${rejected.id}/download`, { headers: owner })).status()).toBe(404);
    expect((await page.request.post(f.api + `/api/v1/documents/requests/${f.ordinaryRequest}/additions`, { headers: owner,
      data: { key: randomUUID(), documents: [{ id: rejected.id, version: rejected.version }] } })).status()).toBe(409);
    const clean = db.prepare("SELECT Id, Checksum, Size FROM SupportingDocuments WHERE Status='Clean'").all();
    expect(clean.length).toBe(3);
    expect(db.prepare('SELECT count(*) AS n FROM EvidenceAdditions').get()!.n).toBe(3);
    expect(db.prepare("SELECT count(*) AS n FROM DocumentAccessEntries WHERE Action='SupplementalBind'").get()!.n).toBe(3);
    expect(row('SELECT RequestId FROM SupportingDocuments WHERE Id=?', rejected.id).RequestId).toBeNull();
    for (const document of clean) {
      const stored = fs.readFileSync(path.join(f.privateRoot, 'documents', 'clean', String(document.Id).replaceAll('-', '').toLowerCase() + '.blob'));
      expect(stored.length).toBe(document.Size); expect(createHash('sha256').update(stored).digest('hex').toUpperCase()).toBe(document.Checksum);
    }
    privatePermissions(f.privateRoot, f.database);
  } catch (error) {
    if (error && typeof error === 'object' && 'errcode' in error && Number.isInteger(error.errcode)
      && Number(error.errcode) >= 0 && Number(error.errcode) <= 65535) console.log('real-scanner-sqlite-code ' + Number(error.errcode));
    throw error;
  } finally { db.close(); await scanner.stop(); }
});

test('Real ClamAV lifecycle: exception cleanup preserves operator definitions and configuration', async () => {
  test.setTimeout(60_000);
  const scanner = new Scanner(); const marker = new Error('Controlled private scanner failure');
  let observed = false;
  try {
    try { await scanner.start(); throw marker; }
    finally { await scanner.stop(); }
  } catch (error) { expect(error === marker).toBe(true); observed = true; }
  expect(observed).toBe(true); scanner.assertStopped();
});
