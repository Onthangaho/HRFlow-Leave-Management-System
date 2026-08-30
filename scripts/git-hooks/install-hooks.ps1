
<#
.SYNOPSIS
    Installs the tracked pre-commit hook for HRFlow (Windows PowerShell path).
.DESCRIPTION
    Copies scripts/git-hooks/pre-commit to .git/hooks/pre-commit and sets
    recommended local git settings (fetch.prune = true, etc.). On Windows,
    the bash hook runs when git-for-windows invokes it via its bundled bash.
.EXAMPLE
    powershell -File scripts/git-hooks/install-hooks.ps1
#>

$ErrorActionPreference = "Stop"

$RepoRoot = Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$HookSrc = Join-Path $PSScriptRoot "pre-commit"
$HookDir = Join-Path $RepoRoot ".git/hooks"
$HookDst = Join-Path $HookDir "pre-commit"

if (-not (Test-Path $HookSrc)) {
    throw "install-hooks.ps1: ERROR - Hook source not found at $HookSrc"
}

New-Item -ItemType Directory -Force -Path $HookDir | Out-Null
Copy-Item -Force -Path $HookSrc -Destination $HookDst
Write-Host "install-hooks.ps1: installed $HookDst"

& git config --local fetch.prune true
& git config --local advice.checkoutAmbiguousRefs true
& git config --local advice.detachedHead true
& git config --local init.defaultBranch main

Write-Host "install-hooks.ps1: configured local git settings."
Write-Host "  (Note: the 'git co' safe-checkout alias is only installed by install-hooks.sh.)"
