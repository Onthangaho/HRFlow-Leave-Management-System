/** Extracts only bounded relative source coordinates; raw Playwright errors may contain private form data. */
export function safeEvidenceLocations(errors: readonly { stack?: string }[]): string[] {
  const locations = new Set<string>();
  for (const error of errors) {
    for (const match of (error.stack ?? '').matchAll(/[/\\]evidence\.spec\.ts:(\d+):(\d+)/g)) {
      const line = Number(match[1]), column = Number(match[2]);
      if (!Number.isSafeInteger(line) || !Number.isSafeInteger(column) || line < 1 || column < 1 || line > 100_000 || column > 100_000) continue;
      locations.add('evidence-diagnostic ' + JSON.stringify({ source: { file: 'evidence.spec.ts', line, column } }));
    }
  }
  return [...locations];
}
