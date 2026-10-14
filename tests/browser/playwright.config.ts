import { defineConfig } from '@playwright/test';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';

const directory = process.env.HRFLOW_BROWSER_RUN_DIR ?? fs.mkdtempSync(path.join(os.tmpdir(), 'hrflow-browser-'));
if (path.dirname(directory) !== os.tmpdir() || !path.basename(directory).startsWith('hrflow-browser-') || fs.lstatSync(directory).isSymbolicLink()) throw new Error('Invalid private browser directory.');
process.env.HRFLOW_BROWSER_RUN_DIR = directory;
if (process.platform !== 'win32') fs.chmodSync(directory, 0o700);

/** One bounded Chromium suite; retries and sensitive screenshot/trace/video artifacts are deliberately disabled. */
export default defineConfig({
  testDir: '.', testMatch: '*.spec.ts', workers: 1, retries: 0, timeout: 60_000, outputDir: path.join(directory, 'artifacts'),
  expect: { timeout: 10_000 }, globalSetup: './setup.mjs', reporter: './safe-reporter.ts',
  use: { browserName: 'chromium', channel: process.env.HRFLOW_BROWSER_CHANNEL || undefined,
    screenshot: 'off', trace: 'off', video: 'off', reducedMotion: 'reduce' },
});
