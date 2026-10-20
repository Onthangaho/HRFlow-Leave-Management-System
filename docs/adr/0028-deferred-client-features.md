# ADR 0028: Defer client features behind a stable shell

Status: Accepted for this focused performance slice, 20 October 2026.

## Context and evidence

The readiness review identified eager route imports, not a measured production-capacity problem.
Main at `65b5143c46db74db0032829b67afe471be39fc5f` includes merged PR #125. A fresh baseline
production build emitted a 610.88 kB main chunk (179.21 kB gzip). App imported almost every route;
Overview imported Personal, Manager and HR workspaces regardless of capability. The shell imported
the inbox and image-editor modules merely to render a badge/avatar. Reports and HR charts already
used dynamic imports. Existing issue search found related #77/#76 and dashboard issues, but no
dedicated route-loading issue; no issues were created or edited and #77 is not declared complete.

## Decision

- Keep the existing Router, QueryClient, AuthProvider, capability guards and ApplicationShell stable.
  Login stays eager. Use module-level deferred feature definitions for substantial route pages and
  independently for each additive Overview workspace. Do not prefetch unauthorized workspaces.
  Bundled code availability is not authorization: unchanged API checks remain authoritative.
- Move the existing avatar and notification badge/visibility hook into small shared modules, without
  changing their queries, version keys, cancellation, polling, object-URL cleanup or permissions.
- Use React lazy/Suspense with a themed, announced, minimum-height loading panel inside the shell.
  A feature-local error boundary renders safe connection/retry guidance, never the error message.
  Explicit retry only reopens code; it does not reload the document, reset auth or replay a mutation.
- Browsers may cache a rejected module fetch. Explicit retry can import a fresh query URL for the
  failed same-origin built `/assets/<safe-name>.js` only (or a validated feature source module in
  Development). Project the expected named component from that namespace. Relative shared imports
  retain their URLs, including the single auth context. No external/user-selected URL is accepted.
  Missing deployed assets, incompatible releases and unrecognised import errors may still require
  a manual reopen after preserving drafts; do not promise every deployment failure is retryable.
- Keep existing report/chart splitting and normal bundler decisions. No arbitrary chunk rules,
  dependency changes, warning suppression, API/schema/business-rule changes or statutory claims.

## State and failure boundaries

Normal renders/token refresh do not recreate lazy components. Explicit retry replaces only that
feature, and replacement sessions reset its boundary using the existing session generation. Existing
shell Outlet and workspace keys still own session cleanup. Pending module imports contain code only;
leaving/signing out unmounts the old feature, so completion cannot restore its data or form.
Existing forms still unmount when intentionally navigating away; this does not add cross-route draft
persistence. Background query refresh retains original edit snapshots as before.

The error boundary also contains render failures. Retrying a failed feature remounts it, so guidance
does not claim draft recovery within a crashed feature. It never reloads or resets other mounted
workspaces. Retain previous release assets during rollout and use the supported SPA direct-route
fallback; route splitting is not a substitute for deployment design.

## Verification and trade-offs

[Executed measurements, failures, commands and limitations](../verification/client-route-loading.md)
distinguish resource bytes from gzip estimates, local timings from capacity, and real HTTP from
controlled module transport. More chunks mean extra requests and potentially slower first transitions.
API aggregates, load testing, accessibility certification and legal review remain separate work.
