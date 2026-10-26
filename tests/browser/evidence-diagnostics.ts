/** Extracts only bounded relative source coordinates; raw Playwright errors may contain private form data. */
export function safeEvidenceLocations(errors: readonly { stack?: string }[]): string[] {
  return safeLocations(errors, 'evidence.spec.ts');
}

/** Existing workflow failures need the same source-only boundary, without exposing Playwright call logs. */
export function safeWorkflowLocations(errors: readonly { stack?: string }[]): string[] {
  return safeLocations(errors, 'workflows.spec.ts');
}

function safeLocations(errors: readonly { stack?: string }[], file: 'evidence.spec.ts' | 'workflows.spec.ts'): string[] {
  const locations = new Set<string>();
  for (const error of errors) {
    const pattern = file === 'evidence.spec.ts' ? /[/\\]evidence\.spec\.ts:(\d+):(\d+)/g : /[/\\]workflows\.spec\.ts:(\d+):(\d+)/g;
    for (const match of (error.stack ?? '').matchAll(pattern)) {
      const line = Number(match[1]), column = Number(match[2]);
      if (!Number.isSafeInteger(line) || !Number.isSafeInteger(column) || line < 1 || column < 1 || line > 100_000 || column > 100_000) continue;
      locations.add((file === 'evidence.spec.ts' ? 'evidence-diagnostic ' : 'workflow-diagnostic ') + JSON.stringify({ source: { file, line, column } }));
    }
  }
  return [...locations];
}
