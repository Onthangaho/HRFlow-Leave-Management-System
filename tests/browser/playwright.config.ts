import { defineConfig } from '@playwright/test';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';

const realEvidence = process.env.HRFLOW_EVIDENCE_REAL_SCANNER === '1';
if (realEvidence) {
  const privateTemp = process.env.HRFLOW_EVIDENCE_PRIVATE_TEMP;
  if (!privateTemp || !path.isAbsolute(privateTemp) || path.resolve(os.tmpdir()) !== path.resolve(privateTemp)
    || ['TMPDIR', 'TMP', 'TEMP', 'ASPNETCORE_TEMP'].some(name => !process.env[name]
      || path.resolve(process.env[name]!) !== path.resolve(privateTemp)))
    throw new Error('Real-scanner verification requires explicit matching private browser/application temporary locations.');
}

const directory = process.env.HRFLOW_BROWSER_RUN_DIR ?? fs.mkdtempSync(path.join(os.tmpdir(), 'hrflow-browser-'));
if (path.dirname(directory) !== os.tmpdir() || !path.basename(directory).startsWith('hrflow-browser-') || fs.lstatSync(directory).isSymbolicLink()) throw new Error('Invalid private browser directory.');
process.env.HRFLOW_BROWSER_RUN_DIR = directory;
if (process.platform !== 'win32') fs.chmodSync(directory, 0o700);

/** One bounded Chromium suite; retries and sensitive screenshot/trace/video artifacts are deliberately disabled. */
export default defineConfig({
  testDir: '.', testMatch: process.env.HRFLOW_EVIDENCE_REAL_SCANNER === '1' ? 'evidence.real-scanner.ts' : process.env.HRFLOW_LOADING_MEASURE === '1' ? 'loading.measure.ts' : process.env.HRFLOW_IMAGE_SCANNER_TESTS === 'all' ? ['*.spec.ts', '*.scanner.ts'] : process.env.HRFLOW_IMAGE_SCANNER_TESTS === '1' ? '*.scanner.ts' : '*.spec.ts', workers: 1, retries: 0, timeout: 60_000, outputDir: path.join(directory, 'artifacts'),
  expect: { timeout: 10_000 }, globalSetup: './setup.mjs', reporter: './safe-reporter.ts',
  use: { browserName: 'chromium', channel: process.env.HRFLOW_BROWSER_CHANNEL || undefined,
    screenshot: 'off', trace: 'off', video: 'off', reducedMotion: 'reduce' },
});
