# Git Branch Workflow — HRFlow

## Why this document exists

On 2026-08-30 the repo experienced a **140+ file carryover of uncommitted changes**
across three sequential branch switches (`feature/leave-request-approval → main →
feature/leave-request-approval`). Two root causes were found:

1. 214 tracked .NET build outputs (`src/*/bin`, `src/*/obj`) had been committed
   **before** the `.gitignore` was added. Git never auto-untracks already-tracked
   files when an ignore rule is later added — producing 69+ "tracked modifications"
   on every `dotnet build` + branch switch. Fixed in commit `eb275c6`.
2. Large in-progress work (39 tracked source files + ~265 untracked new source
   files) was never committed onto its feature branch — the working tree travels
   with you by default, producing silent cross-branch carryover.
   Fixed in commits `f66f71d` + `4ca3ae0` (committed properly).

This document formalizes the steps that prevent both classes of recurring bugs.

---

## Non-negotiable ground rules

1. **Never use `git add .` or blanket adds.** Always use `git add <exact files>`
   with an explicit list.
2. **Never switch branches with a dirty working tree** — commit, stash, or
   restore first. The git working tree is NOT per-branch: uncommitted changes
   travel with every checkout.
3. **Never commit to `main`.** Always use a feature branch created off an
   up-to-date `main`.
4. Run `git status` **before AND after** every branch switch. If the output is
   anything other than `nothing to commit, working tree clean`, stop and resolve.
5. Use Conventional Commits format: `<type>(<scope>): <description>`.
   One commit = one logical change.

---

## One-time setup (run after every fresh clone)

```bash
# From repo root (git bash / WSL / macOS / Linux):
bash scripts/git-hooks/install-hooks.sh

# OR on Windows PowerShell:
powershell -File scripts/git-hooks/install-hooks.ps1
```

This does four things:

- Installs the `pre-commit` hook into `.git/hooks/pre-commit`. It **blocks** any
  staged files matching `bin/`, `obj/`, `*.dll`, `*.pdb`, `*.db`, `.cache`, etc.
  If the working tree is dirty, it also prints a non-blocking warning.
- Sets `fetch.prune = true` so stale remote refs don't confuse checkouts.
- Enables `advice.checkoutAmbiguousRefs` and `advice.detachedHead`.
- Adds a **safety alias: `git co <branch>`** (safe-checkout):

```bash
git co feature/leave-request-approval     # safe-checkout: REFUSES if working tree dirty
git checkout <branch>                      # standard git checkout, no warnings (use carefully)
git co -f feature/leave-request-approval   # force the switch anyway (not recommended)
```

---

## Branch lifecycle

### 1. Start a new feature

```bash
git co main
git pull origin main            # always 100% upstream before branching
git co -b feat/<short-description>
# implement work on this branch only
```

### 2. Before every commit

```bash
git status                       # expect: only the files you intentionally edited
git add src/HRFlow.Api/Controllers/X.cs \
        src/HRFlow.Application/Features/X/... \
        src/HRFlow.Domain/...   # explicit list; never `git add .`
git commit -m "feat(scope): description"
```

If `git status` shows files you did not touch — investigate.
If `git diff --name-only --diff-filter=ACM | grep -E '/bin/|/obj/|\.dll$'` returns anything,
run `git restore --staged <path>` and investigate. The pre-commit hook will also
catch this and abort with a helpful message.

### 3. Before every branch switch

```bash
git status
# MUST read: nothing to commit, working tree clean

# If NOT clean:
#   Option A: git commit -m "wip(scope): checkpoint"   (coherent enough)
#   Option B: git stash push -u -m "WIP: <reason>"       (back in <24h)
#   Option C: git restore <files>                        (accidental edits)

git co main
git pull origin main
git co my-feature-branch
# If you chose Option B:
git stash pop
```

---

## When you see carryover

If `git checkout main` prints `M`/`D` lines when you expected a clean switch —
STOP. You brought uncommitted source files from your previous branch:

```bash
git checkout previous-branch     # go back
git status                       # confirm the dirt
git commit -m "wip: checkpoint"  # OR git stash push -u -m "WIP checkpoint"
git checkout main                # now switch — should be clean.
```

Do NOT continue working on `main` or a new feature branch on top of a dirty
carryover — that's exactly how the 2026-08-30 bug started.
