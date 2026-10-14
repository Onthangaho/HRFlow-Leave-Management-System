import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import net from 'node:net';
import crypto from 'node:crypto';
import { spawn, spawnSync } from 'node:child_process';
import { pathToFileURL } from 'node:url';

const repo = path.resolve(import.meta.dirname, '../..');
const marker = 'HRFlowSyntheticPitchV1';
const command = process.argv[2];
const root = path.resolve(process.argv[3] ?? '');
const api = 'http://127.0.0.1:5540', client = 'http://127.0.0.1:5541';
const stateFile = path.join(root, 'private-state.json');

/** Only deliberately safe operator messages may reach normal output; library errors can contain private data. */
function refusal(message) { return Object.assign(new Error(message), { pitchSafe: true }); }

/** Validate an isolated local destination before reading secrets or deleting owned files. */
function validateRoot() {
  const compare = value => process.platform === 'win32' ? value.toLowerCase() : value;
  if (!process.argv[3] || compare(path.dirname(root)) !== compare(path.resolve(os.tmpdir())) || root === path.parse(root).root || compare(root).startsWith(compare(repo + path.sep))
    || !path.basename(root).startsWith('hrflow-pitch-')) throw refusal('Use a new private direct child of the OS temp directory named hrflow-pitch-<unique>.');
  for (let part = root; part !== path.dirname(part); part = path.dirname(part))
    if (fs.existsSync(part) && fs.lstatSync(part).isSymbolicLink()) throw refusal('Symlink/reparse destinations are unsupported.');
}

/** Restrict generated secrets before they are written; no credential is printed. */
function restrict() {
  if (process.platform === 'win32') {
    const result = spawnSync('powershell', ['-NoProfile', '-Command', "$a=[Security.AccessControl.DirectorySecurity]::new();$a.SetAccessRuleProtection($true,$false);$id=[Security.Principal.WindowsIdentity]::GetCurrent().User;$a.AddAccessRule([Security.AccessControl.FileSystemAccessRule]::new($id,'FullControl','ContainerInherit,ObjectInherit','None','Allow'));Set-Acl -LiteralPath $env:HRFLOW_PITCH_ROOT -AclObject $a"], { env: { ...process.env, HRFLOW_PITCH_ROOT: root }, stdio: 'ignore', windowsHide: true });
    if (result.status !== 0) throw refusal('Could not establish private destination permissions.');
  } else fs.chmodSync(root, 0o700);
}

/** A bounded real clamd protocol probe; absence never substitutes a clean verdict. */
async function scanner() {
  return new Promise((resolve, reject) => {
    const socket = net.connect({ host: '127.0.0.1', port: 3310 }); let output = '';
    const timer = setTimeout(() => { socket.destroy(); reject(refusal('ClamAV readiness timed out; repair the real scanner on loopback port 3310.')); }, 5000);
    socket.on('close', () => clearTimeout(timer));
    socket.on('connect', () => socket.write('zVERSION\0'));
    socket.on('data', bytes => {
      output += bytes.toString('ascii');
      if (output.length > 1024) { socket.destroy(); reject(refusal('ClamAV readiness response was too large.')); }
      else if (output.includes('\0')) { socket.destroy(); if (/^ClamAV \d/.test(output)) resolve(); else reject(refusal('ClamAV identity was unexpected.')); }
    });
    socket.on('error', () => reject(refusal('Real ClamAV unavailable: provision clamd with signatures on loopback port 3310; see docs/operations/supporting-documents.md.')));
    socket.on('end', () => { socket.destroy(); reject(refusal('ClamAV readiness was ambiguous.')); });
  });
}

/** Only marked synthetic destinations can be restarted or cleaned; no existing production store is accepted. */
function state() {
  validateRoot(); const value = JSON.parse(fs.readFileSync(stateFile, 'utf8').replace(/^\uFEFF/, ''));
  if (value.marker !== marker || value.root !== root) throw refusal('Not an owned pitch destination.');
  return value;
}

/** Use a fresh South African business date, never a hardcoded pitch month. */
function businessDate() { return new Intl.DateTimeFormat('en-CA', { timeZone: 'Africa/Johannesburg', year: 'numeric', month: '2-digit', day: '2-digit' }).format(new Date()); }

/** Generate a complete harmless one-page PDF with correct xref offsets, not a renamed text file. */
function syntheticPdf() {
  const content = 'BT /F1 14 Tf 40 740 Td (Synthetic ordinary evidence - no employee or medical information.) Tj ET';
  const objects = ['<< /Type /Catalog /Pages 2 0 R >>', '<< /Type /Pages /Kids [3 0 R] /Count 1 >>',
    '<< /Type /Page /Parent 2 0 R /MediaBox [0 0 595 842] /Resources << /Font << /F1 4 0 R >> >> /Contents 5 0 R >>',
    '<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>', `<< /Length ${Buffer.byteLength(content)} >>\nstream\n${content}\nendstream`];
  let pdf = '%PDF-1.4\n'; const offsets = [0];
  for (const [index, object] of objects.entries()) { offsets.push(Buffer.byteLength(pdf)); pdf += `${index + 1} 0 obj\n${object}\nendobj\n`; }
  const xref = Buffer.byteLength(pdf);
  pdf += 'xref\n0 6\n0000000000 65535 f \n' + offsets.slice(1).map(offset => String(offset).padStart(10, '0') + ' 00000 n \n').join('');
  return Buffer.from(pdf + `trailer\n<< /Size 6 /Root 1 0 R >>\nstartxref\n${xref}\n%%EOF\n`, 'ascii');
}

/** Create only a new owner-restricted synthetic store, after real scanner readiness; never reset an existing destination. */
async function setup() {
  validateRoot(); if (fs.existsSync(root)) throw refusal('Destination already exists; choose a new one. No store was changed.');
  await scanner(); fs.mkdirSync(root, { recursive: false }); restrict();
  const password = 'Aa!9' + crypto.randomBytes(24).toString('hex');
  const config = { marker, root, api, client, date: businessDate(), password, employeePassword: 'Aa!9' + crypto.randomBytes(24).toString('hex'),
    signing: crypto.randomBytes(48).toString('hex'), pepper: crypto.randomBytes(48).toString('hex'), pickupProfile: 'Pitch-' + crypto.randomUUID(), pid: null, apiPid: null };
  fs.mkdirSync(path.join(root, 'private')); fs.mkdirSync(path.join(root, 'fixtures'));
  fs.writeFileSync(stateFile, JSON.stringify(config), { mode: 0o600, flag: 'wx' });
  const header = 'FirstName,LastName,EmailAddress,EmployeeNumber,Roles,Department,ManagerEmail,EmploymentStartDate,WorkScheduleCode,Gender\n';
  fs.writeFileSync(path.join(root, 'fixtures', 'synthetic-onboarding.csv'), header + `Avery,Example,avery@example.invalid,PITCH-001,Employee,Human Resources,manager@hrflow.local,${config.date},,\n`, { flag: 'wx', mode: 0o600 });
  fs.writeFileSync(path.join(root, 'fixtures', 'synthetic-evidence.pdf'), syntheticPdf(), { flag: 'wx', mode: 0o600 });
  console.log(`Prepared NEW synthetic Development destination. South African date: ${config.date}. Credentials remain in the private descriptor; do not project it.`);
}

/** Run actual Development migrations/Identity seeding and own API/client processes, with no product bypasses or secret logs. */
async function start() {
  const config = state(); if (config.date !== businessDate()) throw refusal('Use a new destination for today; prior scenario dates are preserved.'); await scanner();
  for (const pid of [config.pid, config.apiPid].filter(Boolean)) { try { process.kill(pid, 0); throw refusal('An owned rehearsal may still be running; stop it first.'); } catch (error) { if (error.code !== 'ESRCH') throw error; } }
  // Explicit Development-only environment. All built-in seed passwords are privately generated.
  const inherited = Object.fromEntries(['PATH', 'DOTNET_ROOT', 'HOME', 'USERPROFILE', 'LOCALAPPDATA', 'SystemRoot', 'TEMP', 'TMP'].filter(name => process.env[name]).map(name => [name, process.env[name]]));
  const env = { ...inherited, DOTNET_ENVIRONMENT: 'Development', ASPNETCORE_ENVIRONMENT: 'Development', ASPNETCORE_URLS: api, AllowedHosts: '127.0.0.1',
    ConnectionStrings__DefaultConnection: `Data Source=${path.join(root, 'pitch.sqlite')};Foreign Keys=True;Default Timeout=3`,
    Storage__LocalFileSystem: 'true', Storage__PrivateFilesRoot: path.join(root, 'private'),
    Authentication__Jwt__Issuer: 'HRFlow.Pitch', Authentication__Jwt__Audience: 'HRFlow.Pitch.Client',
    Authentication__Jwt__SigningKey: config.signing, Authentication__Jwt__RefreshTokenPepper: config.pepper,
    Activation__ApplicationOrigin: client, Activation__Delivery: 'PrivatePickup', Activation__PickupProfile: config.pickupProfile,
    Deployment__CorsOrigins__0: client, Documents__ClamAvPort: '3310', Documents__ScanTimeoutSeconds: '30',
    Seeding__HrAdministratorPassword: config.password, Seeding__EmployeePassword: config.password,
    Seeding__ManagerPassword: config.password, Seeding__SeniorManagerPassword: config.password, Logging__LogLevel__Default: 'None', 'Logging__LogLevel__Microsoft.Hosting.Lifetime': 'Information' };
  config.pid = process.pid; fs.writeFileSync(stateFile, JSON.stringify(config));
  const child = spawn('dotnet', [path.join(repo, 'src/HRFlow.Api/bin/Debug/net8.0/HRFlow.Api.dll')], { cwd: path.join(repo, 'src/HRFlow.Api'), env, stdio: ['ignore', 'pipe', 'ignore'], windowsHide: true });
  let ownListening = false, launchFailed = false, lifetimeTail = '';
  child.on('error', () => { launchFailed = true; });
  child.stdout.on('data', bytes => { lifetimeTail = (lifetimeTail + bytes.toString()).slice(-1024); if (lifetimeTail.includes('Now listening on: ' + api)) ownListening = true; });
  config.apiPid = child.pid; fs.writeFileSync(stateFile, JSON.stringify(config));
  let server, stopping = false;
  async function stop() {
    if (stopping) return; stopping = true;
    let clientClosed = true;
    if (server) {
      let timer;
      try { await Promise.race([server.close(), new Promise((_, reject) => { timer = setTimeout(() => reject(refusal('Owned client shutdown timed out.')), 5000); })]); }
      catch { clientClosed = false; }
      finally { clearTimeout(timer); }
    }
    if (child.pid && child.exitCode === null && !launchFailed) {
      if (process.platform === 'win32') spawnSync('taskkill', ['/PID', String(child.pid), '/T', '/F'], { stdio: 'ignore', windowsHide: true, timeout: 5000 });
      else child.kill('SIGTERM');
      await Promise.race([new Promise(resolve => child.once('exit', resolve)), new Promise(resolve => setTimeout(resolve, 5000))]);
      if (child.exitCode === null) throw refusal('Owned API did not stop; retain the private store and inspect it.');
    }
    if (!clientClosed) throw refusal('Owned client did not close; API stopped, but retain ownership and inspect the client process.');
    config.pid = null; config.apiPid = null; fs.writeFileSync(stateFile, JSON.stringify(config));
  }
  process.stdin.setEncoding('utf8');
  process.stdin.on('data', value => { if (value.trim() === 'stop') void stop().then(() => process.exit(0)); });
  process.stdin.resume();
  process.once('SIGINT', () => void stop().then(() => process.exit(0)));
  process.once('SIGTERM', () => void stop().then(() => process.exit(0)));
  try {
    const deadline = Date.now() + 45000; let ready = false;
    while (Date.now() < deadline && child.exitCode === null && !launchFailed) {
      try { ready = ownListening && (await fetch(api + '/health/ready', { signal: AbortSignal.timeout(2000) })).ok; if (ready) break; } catch { /* Bounded readiness only. */ }
      await new Promise(resolve => setTimeout(resolve, 100));
    }
    if (!ready) throw refusal('Disposable API readiness failed within 45 seconds; review configuration/build/scanner setup privately.');
    if (!config.csvPrepared) {
      const signedIn = await fetch(api + '/api/v1/auth/login', { method: 'POST', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ email: 'hr.administrator@hrflow.local', password: config.password }), signal: AbortSignal.timeout(5000) });
      if (!signedIn.ok) throw refusal('Private Development bootstrap failed.');
      const token = (await signedIn.json()).accessToken;
      const response = await fetch(api + '/api/v1/employees', { headers: { Authorization: 'Bearer ' + token }, signal: AbortSignal.timeout(5000) });
      if (!response.ok) throw refusal('Private fixture relationship read failed.');
      const manager = (await response.json()).find(employee => employee.email === 'manager@hrflow.local');
      if (!manager?.isActive || manager.requiresActivation || !manager.roles.includes('Manager')) throw refusal('Synthetic seeded manager unavailable.');
      const csv = path.join(root, 'fixtures', 'synthetic-onboarding.csv');
      const header = fs.readFileSync(csv, 'utf8').split('\n')[0];
      const cells = ['Avery', 'Example', 'avery@example.invalid', 'PITCH-001', 'Employee', manager.departmentName, 'manager@hrflow.local', config.date, '', ''];
      fs.writeFileSync(csv, header + '\n' + cells.map(value => '"' + value.replaceAll('"', '""') + '"').join(',') + '\n');
      config.csvPrepared = true; fs.writeFileSync(stateFile, JSON.stringify(config));
    }
    process.env.VITE_API_BASE_URL = api + '/api/v1';
    const { createServer } = await import(pathToFileURL(path.join(repo, 'src/HRFlow.Client/node_modules/vite/dist/node/index.js')));
    server = await createServer({ root: path.join(repo, 'src/HRFlow.Client'), server: { host: '127.0.0.1', port: 5541, strictPort: true } }); await server.listen();
    console.log(`Development-only pitch ready. Client ${client}. South African scenario date ${config.date}. Stop with Ctrl+C or type stop; cleanup is explicit.`);
    await new Promise(resolve => child.once('exit', resolve));
    if (!stopping) throw refusal('Owned API exited; restart only this marked synthetic destination.');
  } finally { await stop(); }
}

/** Remove only an exited marked fixture and its unique pickup profile; live writers or reparse objects cause refusal. */
async function cleanup() {
  const config = state();
  for (const pid of [config.pid, config.apiPid].filter(Boolean)) { try { process.kill(pid, 0); throw refusal('Stop the owned start command first; cleanup refuses potentially live writers.'); } catch (error) { if (error.code !== 'ESRCH') throw error; } }
  const pickupBase = process.platform === 'win32' ? process.env.LOCALAPPDATA : path.join(os.homedir(), '.local/share');
  const pickup = path.join(pickupBase, 'HRFlow', 'ActivationPickup', config.pickupProfile);
  if (!config.pickupProfile.startsWith('Pitch-') || !/^Pitch-[a-f0-9-]{36}$/.test(config.pickupProfile)) throw refusal('Unsafe pickup identity.');
  if (fs.existsSync(pickup) && fs.lstatSync(pickup).isSymbolicLink()) throw refusal('Pickup reparse path refused.');
  function noLinks(directory) {
    if (!fs.existsSync(directory)) return;
    for (const entry of fs.readdirSync(directory, { withFileTypes: true })) {
      const full = path.join(directory, entry.name);
      if (fs.lstatSync(full).isSymbolicLink()) throw refusal('Unsafe reparse object in owned cleanup destination.');
      if (entry.isDirectory()) noLinks(full);
    }
  }
  noLinks(root); noLinks(pickup);
  fs.rmSync(pickup, { recursive: true, force: true }); fs.rmSync(root, { recursive: true });
  console.log('Owned synthetic store, files, private descriptor and pickup invitations removed. Scanner service was not changed.');
}

try { if (command === 'setup') await setup(); else if (command === 'start') await start(); else if (command === 'cleanup') await cleanup(); else throw refusal('Usage: node tools/pitch/pitch-demo.mjs setup|start|cleanup <new-private-hrflow-pitch-destination>'); }
catch (error) { console.error(error.pitchSafe ? error.message : 'Pitch operation failed safely. Inspect prerequisites privately; no secrets reported.'); process.exitCode = 1; }
