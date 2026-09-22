# Run this in an ELEVATED PowerShell (Run as Administrator) ON THE IIS SERVER ITSELF - this script
# updates the server's own local clone of the repo to the latest `master` from Gitea, then builds
# and hosts every UBIS2 service on this machine's IIS. Not meant for a developer workstation - use
# Setup-AllServices.ps1 (first-time local setup) or Others/publish-staging/Republish-Microservices.ps1
# (redeploy your own local working copy) for that instead.
#
# HOW TO RUN (from an elevated PowerShell, at the repo root on the server):
#   powershell -ExecutionPolicy Bypass -File .\Deploy-FromMaster.ps1
# (The Set-ExecutionPolicy line below only takes effect once the script is already running, so it
# can't rescue its own launch on a machine where script execution is disabled outright - use the
# command above for the very first run, or run once, permanently:
#   Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
# )
#
# WHAT THIS DOES, IN ORDER:
#   1. Refuses to run if the server's working copy has uncommitted local changes (pass -Force to
#      discard them instead - see the warning below, this is destructive).
#   2. git fetch + hard-resets the local checkout to origin/master (exactly what Gitea has, not
#      whatever a developer happened to leave the server's clone pointed at). appsettings*.json is
#      tracked in git, so this DOES overwrite it with whatever is committed on master - that's fine,
#      since step 3 immediately rewrites ConnectionStrings:DefaultConnection with this server's own
#      $SqlServerInstance/$DatabaseName below regardless of whatever value master happened to have.
#      If any OTHER setting (Redis/RabbitMQ host, etc.) genuinely differs on this server from what's
#      committed, re-apply it by hand after this script finishes.
#   3. Calls Publish-Common.ps1's Invoke-Deploy (same shared build+host+
#      grant-SQL logic every other deploy script in this repo uses - see that file's own header for
#      why deploy logic lives in exactly one place) to publish and host every service on this
#      machine's IIS.
#
# HOW TO EDIT FOR YOUR SERVER: change the variables right below this comment block. Git credentials
# are deliberately NOT hardcoded here - passing a plaintext password on the command line or storing
# it in this file (which is version-controlled) would leak it into shell history and Gitea itself.
# Supply -GitUsername/-GitPassword as SecureString params when you run this, or leave them unset to
# be prompted interactively.

[CmdletBinding()]
param(
    # Gitea HTTP username. Prompted for if not supplied.
    [string]$GitUsername,

    # Gitea HTTP password, as a SecureString. Prompted for (masked) if not supplied. Never pass this
    # as a plain string literal on the command line - that leaks it into shell history.
    [SecureString]$GitPassword,

    # Skips the "uncommitted local changes" safety check and hard-resets over them anyway. Only use
    # this if you are certain nothing on this server's working copy needs to be kept - git reset
    # --hard is not recoverable from this script.
    [switch]$Force
)

Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force
$ErrorActionPreference = "Stop"

# ====================================================================================================
# EDIT THESE FOR YOUR SERVER
# ====================================================================================================

$GitRemoteUrl = "http://10.19.116.66:7070/UBIS/UBIS_DEV"
# Fixed 2026-08-31: was pointed at "http://10.19.116.66/gitea/UBIS/UBIS_DEV" - an older, stale path
# on the same host (missing the :7070 port, wrong /gitea/ prefix), not the real repo - see this
# repo's own root CLAUDE.md ("an older /gitea/UBIS/UBIS_DEV path on the same host is stale/wrong,
# do not use it"). This meant every run of this script fetched from the wrong place, so a deploy
# could silently republish stale code even though the real Gitea repo (this URL) had the latest
# master - root cause of a report that the Allocation screen wasn't live on the server after this
# script's -Force run, when master on the real repo already had it.
$GitBranch = "master"

# SQL Server instance and database this server's services should connect to. Written into every
# service's source appsettings.json (ConnectionStrings:DefaultConnection) before publishing - see
# Publish-Common.ps1's Set-ConnectionString.
$SqlServerInstance = "$env:COMPUTERNAME\SQLEXPRESS"
$DatabaseName = "UBIS-Dev"

# IP address IIS sites bind to. Corporate security policy blocks IIS from binding to "*"/a specific
# LAN IP for the backend microservices - only loopback (127.0.0.1) works. UBIS_Web itself still
# binds "*" regardless (handled inside Invoke-Deploy) so it stays reachable from other machines on
# the network. Do NOT hardcode a machine-specific address here.
$IPAddress = "127.0.0.1"

# Only deploy some services (leave empty to deploy all of them). Example: @("AIM", "UBIS_Web")
$Only = @()

# ====================================================================================================
# Nothing below this line needs editing.
# ====================================================================================================

# --- Step 1: refuse to clobber uncommitted work on the server's own clone -------------------------
Write-Host "== Checking server working copy for uncommitted changes ==" -ForegroundColor Cyan
$dirty = git -C $PSScriptRoot status --porcelain
if ($dirty -and -not $Force) {
    Write-Host "Refusing to continue: the server's working copy at $PSScriptRoot has uncommitted changes:" -ForegroundColor Red
    Write-Host $dirty
    Write-Host "Commit/stash them first, or re-run with -Force to discard them and reset to origin/$GitBranch anyway." -ForegroundColor Red
    exit 1
}

# --- Step 2: authenticate and fetch -----------------------------------------------------------------
if (-not $GitUsername) { $GitUsername = Read-Host "Gitea username" }
if (-not $GitPassword) { $GitPassword = Read-Host "Gitea password" -AsSecureString }

$plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($GitPassword))
try {
    $encodedUser = [Uri]::EscapeDataString($GitUsername)
    $encodedPassword = [Uri]::EscapeDataString($plainPassword)
    $remoteUri = [Uri]$GitRemoteUrl
    $authRemoteUrl = "$($remoteUri.Scheme)://${encodedUser}:${encodedPassword}@$($remoteUri.Authority)$($remoteUri.PathAndQuery)"

    Write-Host "== Fetching latest $GitBranch from Gitea ==" -ForegroundColor Cyan
    git -C $PSScriptRoot fetch $authRemoteUrl "${GitBranch}:refs/remotes/origin/$GitBranch"
    if ($LASTEXITCODE -ne 0) { throw "git fetch failed (exit $LASTEXITCODE) - check the Gitea URL/credentials above." }
} finally {
    # Never let the plaintext password linger in memory longer than the one git invocation that needs it.
    $plainPassword = $null
    [System.GC]::Collect()
}

Write-Host "== Resetting to origin/$GitBranch ==" -ForegroundColor Cyan
git -C $PSScriptRoot checkout $GitBranch
if ($LASTEXITCODE -ne 0) { throw "git checkout $GitBranch failed (exit $LASTEXITCODE)." }
git -C $PSScriptRoot reset --hard "origin/$GitBranch"
if ($LASTEXITCODE -ne 0) { throw "git reset --hard origin/$GitBranch failed (exit $LASTEXITCODE)." }

$deployedCommit = git -C $PSScriptRoot rev-parse --short HEAD
Write-Host "Now at $GitBranch @ $deployedCommit" -ForegroundColor Green

# --- Step 3: build, host, and smoke-test every service -----------------------------------------------
. "$PSScriptRoot\Publish-Common.ps1"
Invoke-Deploy -SqlServerInstance $SqlServerInstance -DatabaseName $DatabaseName -IPAddress $IPAddress -Only $Only
