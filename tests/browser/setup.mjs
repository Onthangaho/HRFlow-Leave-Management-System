import fs from 'node:fs';
import path from 'node:path';
import os from 'node:os';
import net from 'node:net';
import { spawn } from 'node:child_process';
import { pathToFileURL } from 'node:url';

/** Uses the same C# SQLite/Identity fixture as xUnit; no scanner, diagnostic API, copied database or fixed port dependency. */
export default async function setup() {
  const repo = path.resolve(import.meta.dirname, '../..');
  const directory = process.env.HRFLOW_BROWSER_RUN_DIR;
  if (!directory || path.dirname(directory) !== os.tmpdir() || !path.basename(directory).startsWith('hrflow-browser-')) throw new Error('Invalid private browser directory.');
  const statePath = path.join(directory, 'private-state.json');
  const listener = net.createServer();
  await new Promise(resolve => listener.listen(0, '127.0.0.1', resolve));
  const port = listener.address().port;
  const origin = `http://127.0.0.1:${port}`;
  const child = spawn('dotnet', [path.join(repo, 'tests/HRFlow.RegressionTests/bin/Debug/net8.0/HRFlow.RegressionTests.dll'), '--serve', statePath, origin],
    { cwd: repo, stdio: ['pipe', 'ignore', 'ignore'], windowsHide: true, detached: process.platform !== 'win32' });
  let server;
  async function stop() {
    if (server) await server.close();
    if (listener.listening) await new Promise(resolve => listener.close(resolve));
    const exited = new Promise(resolve => child.once('exit', resolve));
    if (child.exitCode === null) {
      child.stdin.end('\n');
      await Promise.race([exited, new Promise(resolve => setTimeout(resolve, 8000))]);
      if (child.exitCode === null) {
        // Kill only the owned fixture tree/group; never leave its API children running after a timeout.
        if (process.platform === 'win32') {
          const killer = spawn('taskkill', ['/PID', String(child.pid), '/T', '/F'], { stdio: 'ignore', windowsHide: true });
          await Promise.race([new Promise(resolve => killer.once('exit', resolve)), new Promise(resolve => setTimeout(resolve, 5000))]);
        } else { try { process.kill(-child.pid, 'SIGKILL'); } catch { /* Already exited. */ } }
        await Promise.race([exited, new Promise(resolve => setTimeout(resolve, 5000))]);
        if (child.exitCode === null) throw new Error('Owned fixture process cleanup failed; private files retained.');
      }
    }
    if (path.dirname(statePath) !== directory || !path.basename(directory).startsWith('hrflow-browser-')) throw new Error('Unsafe fixture cleanup path.');
    fs.rmSync(directory, { recursive: true, force: true });
  }
  try {
    const deadline = Date.now() + 60_000;
    while (!fs.existsSync(statePath)) {
      if (child.exitCode !== null || Date.now() > deadline) throw new Error('Disposable fixture did not become ready.');
      await new Promise(resolve => setTimeout(resolve, 25)); // Readiness only, not interleaving coordination.
    }
    const state = JSON.parse(fs.readFileSync(statePath, 'utf8'));
    state.client = origin; fs.writeFileSync(statePath, JSON.stringify(state));
    process.env.HRFLOW_TEST_STATE = statePath;
    process.env.VITE_API_BASE_URL = state.api + '/api/v1';
    const { createServer } = await import(pathToFileURL(path.join(repo, 'src/HRFlow.Client/node_modules/vite/dist/node/index.js')));
    server = await createServer({ root: path.join(repo, 'src/HRFlow.Client'), server: { host: '127.0.0.1', port, strictPort: true } });
    await new Promise(resolve => listener.close(resolve));
    await server.listen();
    return stop;
  } catch { await stop(); throw new Error('Disposable browser setup failed; no secrets or payloads reported.'); }
}
