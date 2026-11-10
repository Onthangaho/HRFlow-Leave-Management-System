import type { Reporter, TestCase, TestResult, FullResult, FullConfig } from '@playwright/test/reporter';
import fs from 'node:fs';
import path from 'node:path';
import { safeDirectoryDiagnostics } from './directory-diagnostics.js';
import { safeEvidenceLocations, safeWorkflowLocations } from './evidence-diagnostics.js';

/** Failure call logs can contain typed passwords; report only test identifiers/status/duration, never payloads or attachments. */
export default class SafeReporter implements Reporter {
  private output = '';
  onBegin(config: FullConfig) { this.output = path.resolve(config.projects[0].outputDir) + path.sep; }
  onTestEnd(test: TestCase, result: TestResult) {
    console.log(`${result.status}: ${test.title} (${result.duration}ms)`);
    for (const output of result.stdout) for (const line of output.toString().split('\n'))
      if (/^submission-recovery-stage (check-entered|signed-out|signed-in|released|settled)$/.test(line)) console.log(line);
    for (const error of result.errors) for (const match of (error.stack ?? '').matchAll(/[/\\]submission-recovery\.spec\.ts:(\d{1,5}):(\d{1,5})/g))
      console.log(`submission-recovery-source submission-recovery.spec.ts:${Number(match[1])}:${Number(match[2])}`);
    for (const error of result.errors) for (const match of (error.stack ?? '').matchAll(/[/\\]leave-preview\.spec\.ts:(\d{1,5}):(\d{1,5})/g))
      console.log(`leave-preview-source leave-preview.spec.ts:${Number(match[1])}:${Number(match[2])}`);
    for (const output of result.stdout) for (const line of output.toString().split('\n'))
      if (/^preview-review-field count=\d{1,3} valueMatches=(true|false) labelCount=\d{1,3} roleCount=\d{1,3}$/.test(line)) console.log(line);
    for (const error of result.errors) for (const match of (error.stack ?? '').matchAll(/[/\\]personal-history\.spec\.ts:(\d{1,5}):(\d{1,5})/g))
      console.log(`personal-history-source personal-history.spec.ts:${Number(match[1])}:${Number(match[2])}`);
    for (const output of result.stdout) for (const line of output.toString().split('\n'))
      if (/^history-controls exact=\d{1,3} selects=\d{1,3}$/.test(line)) console.log(line);
    for (const output of result.stdout) for (const line of output.toString().split('\n'))
      if (/^history-link matches=\d{1,3} headings=\d{1,3}$/.test(line)) console.log(line);
    for (const error of result.errors)
      if ((error.stack ?? '').includes('evidence.spec.ts:') && (error.message ?? '').includes('strict mode violation'))
        console.log('history-navigation ambiguous-target');
    for (const location of safeEvidenceLocations(result.errors)) console.log(location);
    for (const location of safeWorkflowLocations(result.errors)) console.log(location);
    for (const output of result.stdout) for (const line of output.toString().split('\n'))
      if (/^token-refresh-stage (expired-rejected|refresh-entered|request-opened|request-failed)$/.test(line)) console.log(line);
    for (const error of result.errors) for (const match of (error.stack ?? '').matchAll(/[/\\]token-refresh\.spec\.ts:(\d{1,5}):(\d{1,5})/g))
      console.log(`token-refresh-source token-refresh.spec.ts:${Number(match[1])}:${Number(match[2])}`);
    // Opt-in scanner failures expose fixed source coordinates only, never their payload-bearing call logs.
    for (const error of result.errors) for (const match of (error.stack ?? '').matchAll(/[/\\]evidence\.real-scanner\.ts:(\d{1,5}):(\d{1,5})/g))
      console.log(`real-scanner-source evidence.real-scanner.ts:${Number(match[1])}:${Number(match[2])}`);
    for (const output of result.stdout) for (const diagnostic of safeDirectoryDiagnostics(output)) console.log(diagnostic);
    for (const output of result.stdout) for (const line of output.toString().split('\n'))
      if (/^real-scanner-sqlite-code \d{1,5}$/.test(line)) console.log(line);
    // Playwright may create error-context even with tracing off; never retain or publish those payload-bearing attachments.
    for (const attachment of result.attachments) if (attachment.path && path.resolve(attachment.path).startsWith(this.output) && fs.existsSync(attachment.path)) fs.unlinkSync(attachment.path);
    result.attachments.length = 0;
  }
  onError() { console.log('Browser runner failed; sensitive call logs are intentionally withheld.'); }
  onEnd(result: FullResult) { console.log(`Browser suite ${result.status}; duration ${result.duration}ms.`); }
}
