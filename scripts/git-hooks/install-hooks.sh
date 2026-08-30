#!/usr/bin/env bash
#
# Installs the tracked pre-commit hook into .git/hooks/ and sets repo-wide
# recommended git settings (fetch.prune=true, checkout warnings).
#
# Run from the repo root, any time after a fresh clone:
#   bash scripts/git-hooks/install-hooks.sh

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
HOOK_SRC="${SCRIPT_DIR}/pre-commit"
HOOK_DST="${REPO_ROOT}/.git/hooks/pre-commit"

if [ ! -f "$HOOK_SRC" ]; then
    echo "install-hooks: ERROR — cannot find $HOOK_SRC"
    exit 1
fi

mkdir -p "$(dirname "$HOOK_DST")"
cp "$HOOK_SRC" "$HOOK_DST"
chmod +x "$HOOK_DST"

echo "install-hooks: installed $HOOK_DST"

# Recommended repo settings (local, stored in .git/config, not versioned).
git config --local fetch.prune true
git config --local advice.checkoutAmbiguousRefs true
git config --local advice.detachedHead true
git config --local init.defaultBranch main

# Add a local alias for "safe checkout": refuses to switch branches when the
# working tree has uncommitted non-staged changes (avoids cross-branch carryover).
# Forces the caller to either commit, stash, or pass -f as the first arg.
SAFE_CHECKOUT_FN=$(cat <<'ALIAS_BODY'
!f() {
  if [ "$1" = "-f" ]; then
    shift
    git checkout "$@"
    return $?
  fi
  local dirty
  dirty=$(git status --porcelain=v1 | grep -c '^.[^ ]' || true)
  if [ "$dirty" -gt 0 ]; then
    echo "git co: $dirty uncommitted file(s) in working tree — refuse to switch." >&2
    echo "  Resolve first:" >&2
    echo "    git commit ...  (preferred, if changes are coherent)" >&2
    echo "    git stash push -u -m 'WIP: before switching'" >&2
    echo "    git restore <file>  (if accidental edits)" >&2
    echo "  Or force (not recommended):" >&2
    echo "    git co -f <branch>" >&2
    return 1
  fi
  git checkout "$@"
}
f
ALIAS_BODY
)
git config --local alias.co "$SAFE_CHECKOUT_FN"

echo "install-hooks: set local repo settings"
echo "  fetch.prune = true"
echo "  advice.checkoutAmbiguousRefs = true"
echo "  init.defaultBranch = main"
echo "  alias.co = safe-checkout (refuses dirty switches; bypass with 'git co -f <branch>')"
echo ""
echo "Done. Next time you run 'git commit', the pre-commit hook validates staged files."
