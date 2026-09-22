<#
.SYNOPSIS
    One-time (or repeatable) setup for the UBIS Ocelot Gateway on a developer machine.

.DESCRIPTION
    The Gateway (Gateway/UBIS.ApiGateway) is the single entry point every consumer (UBIS_Web,
    MenuScaffolder, Postman, etc.) should call instead of AIM/MenuGenerator/Email/LogWriter/
    UserProfile directly. Routing rules (ocelot.json) are shared and should not be edited
    per-machine. The only thing that legitimately differs machine-to-machine is *where each
    service actually is* - that lives in MicroserviceSettings.json at the repo root, and this
    script is the supported way to edit it.

    Run this once when you first pull the repo, and again any time a service's address changes
    (e.g. you start running AIM locally instead of hitting the shared IIS instance).

.PARAMETER AimHost
.PARAMETER AimPort
    Where AIM is reachable from this machine. Defaults to the shared IIS deployment
    (172.18.160.1:5000) - leave these alone unless you are running AIM locally yourself.

.PARAMETER MenuGeneratorHost
.PARAMETER MenuGeneratorPort
    Same idea, defaults to the shared IIS deployment (172.18.160.1:5001).

.PARAMETER EmailHost
.PARAMETER EmailPort
.PARAMETER LogWriterHost
.PARAMETER LogWriterPort
.PARAMETER UserProfileHost
.PARAMETER UserProfilePort
    Default to the shared IIS deployment on 172.18.160.1 (ports 5004 / 5002 / 5104), same as
    Aim/MenuGenerator above (as of Deploy-AllServices-IIS.ps1, 2026-07-23). Only override to
    localhost:<port> for a service you are deliberately running yourself via "dotnet run" instead.

.PARAMETER GatewayPort
    The port the Gateway itself listens on (default 5150). Only change this if you know why.

.PARAMETER Build
    Build the Gateway project after writing MicroserviceSettings.json.

.PARAMETER Run
    Start the Gateway (dotnet run, Development environment) after configuring/building it.

.PARAMETER Test
    After -Run, wait for the Gateway to come up and smoke-test it against AIM's anonymous
    financial-years route. Implies -Run.

.EXAMPLE
    # First-time setup, everything on shared IIS/defaults, just write the file:
    .\Configure-Gateway.ps1

.EXAMPLE
    # I'm running AIM locally instead of hitting the shared IIS instance:
    .\Configure-Gateway.ps1 -AimHost localhost -AimPort 5101

.EXAMPLE
    # Configure, build, run, and verify in one go:
    .\Configure-Gateway.ps1 -Build -Test
#>
[CmdletBinding()]
param(
    [string]$AimHost = "172.18.160.1",
    [int]$AimPort = 5000,

    [string]$MenuGeneratorHost = "172.18.160.1",
    [int]$MenuGeneratorPort = 5001,

    # Defaults corrected 2026-07-23: Email/LogWriter/UserProfile/UBIS_Web are all IIS-hosted on
    # 172.18.160.1 now (Deploy-AllServices-IIS.ps1), same as Aim/MenuGenerator/Gateway below.
    # "localhost" does NOT work against these real IIS sites - HTTP.sys rejects it outright with a
    # raw 400 "Invalid Hostname" (confirmed by direct curl test; this caused a real bug where the
    # UserProfile page failed to load for every user). Only override back to localhost:<port> for
    # a service you are deliberately running yourself via "dotnet run" instead of IIS.
    [string]$EmailHost = "172.18.160.1",
    [int]$EmailPort = 5004,

    [string]$LogWriterHost = "172.18.160.1",
    [int]$LogWriterPort = 5002,

    [string]$UserProfileHost = "172.18.160.1",
    [int]$UserProfilePort = 5104,

    [string]$UbisWebHost = "172.18.160.1",
    [int]$UbisWebPort = 80,

    # New 2026-07-23 (PreBudget Meeting module build-out). Same shared-IIS-by-default convention
    # as Email/LogWriter/UserProfile/UBIS_Web above. PreBudget was originally assigned 5106, then
    # briefly moved to 5201 under a planned "Domain\-service 5201+" port range — but landed on
    # 5010 instead after a same-day diagnostic (a stale ASPNETCORE_URLS-style Kestrel self-bind
    # attempt kept targeting the old port; a fresh manual IIS rebind to 5010 cleared it). There is
    # no longer a reserved "5201+" range for future Domain\ services — pick any free port and
    # register it here the same way.
    [string]$ReferenceDataHost = "172.18.160.1",
    [int]$ReferenceDataPort = 5105,

    [string]$PreBudgetHost = "172.18.160.1",
    [int]$PreBudgetPort = 5010,

    # New 2026-08-18 (ECL scheme-outlay/DOE-approval module build-out). Same shared-IIS-by-default
    # convention as PreBudget above.
    [string]$EclHost = "172.18.160.1",
    [int]$EclPort = 5011,

    [string]$GatewayHost = "172.18.160.1",
    [int]$GatewayPort = 5150,

    [switch]$Build,
    [switch]$Run,
    [switch]$Test
)

$ErrorActionPreference = "Stop"
if ($Test) { $Run = $true }

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..\..")
$settingsPath = Join-Path $repoRoot "MicroserviceSettings.json"

Write-Host "== UBIS Gateway configuration ==" -ForegroundColor Cyan

# ----------------------------------------------------------------------------------------------
# 1. Write MicroserviceSettings.json - the ONLY file this script (or a developer) should edit to
#    change where a service is reachable. ocelot.json's routing rules are untouched by this.
# ----------------------------------------------------------------------------------------------
$commentText = "Central, single-source-of-truth microservice locations. Consumed at startup by " +
    "Gateway/UBIS.ApiGateway/MicroserviceSettingsLoader.cs, which overrides ocelot.json's " +
    "DownstreamHostAndPorts per route. This is the ONLY file a developer machine should edit to " +
    "point the Gateway at different service addresses - do not hand-edit ocelot.json's " +
    "DownstreamHostAndPorts directly, it will be overwritten by this file at startup. " +
    "Regenerate/update via Gateway/UBIS.ApiGateway/Configure-Gateway.ps1."

$settings = [ordered]@{
    _comment      = $commentText
    Microservices = @(
        [ordered]@{ Name = "AIM";           IpAddress = $AimHost;           Port = $AimPort;           Status = "" }
        [ordered]@{ Name = "MenuGenerator";  IpAddress = $MenuGeneratorHost; Port = $MenuGeneratorPort;  Status = "" }
        [ordered]@{ Name = "Email";          IpAddress = $EmailHost;        Port = $EmailPort;          Status = "" }
        [ordered]@{ Name = "LogWriter";      IpAddress = $LogWriterHost;    Port = $LogWriterPort;      Status = "" }
        [ordered]@{ Name = "UserProfile";    IpAddress = $UserProfileHost;  Port = $UserProfilePort;    Status = "" }
        [ordered]@{ Name = "UBIS_Web";       IpAddress = $UbisWebHost;      Port = $UbisWebPort;        Status = "" }
        [ordered]@{ Name = "ReferenceData"; IpAddress = $ReferenceDataHost; Port = $ReferenceDataPort;  Status = "" }
        [ordered]@{ Name = "PreBudget";      IpAddress = $PreBudgetHost;    Port = $PreBudgetPort;      Status = "" }
        [ordered]@{ Name = "ECL";            IpAddress = $EclHost;          Port = $EclPort;            Status = "" }
        [ordered]@{ Name = "Gateway";        IpAddress = $GatewayHost;      Port = $GatewayPort;        Status = "" }
    )
}

# Status is informational only (not read by the loader) - fill it in from whether the address
# looks like the shared IIS host or a local dev port, just so the file stays self-documenting.
foreach ($svc in $settings.Microservices) {
    $svc.Status = if ($svc.IpAddress -eq "172.18.160.1") { "Shared IIS deployment" } else { "Local dev instance (dotnet run)" }
}

$json = $settings | ConvertTo-Json -Depth 5
Set-Content -Path $settingsPath -Value $json -Encoding utf8
Write-Host "Wrote $settingsPath" -ForegroundColor Green

# Validate it actually parses (catches hand-editing mistakes if this script is re-run after
# someone tweaked the file directly instead of via parameters).
try {
    Get-Content $settingsPath -Raw | ConvertFrom-Json | Out-Null
    Write-Host "MicroserviceSettings.json is valid JSON." -ForegroundColor Green
} catch {
    Write-Host "MicroserviceSettings.json is NOT valid JSON: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Configured addresses:" -ForegroundColor Cyan
$settings.Microservices | ForEach-Object { Write-Host ("  {0,-15} {1}:{2}  ({3})" -f $_.Name, $_.IpAddress, $_.Port, $_.Status) }

# ----------------------------------------------------------------------------------------------
# 2. Reminder for whoever's consuming the Gateway (UBIS_Web, MenuScaffolder, etc.)
# ----------------------------------------------------------------------------------------------
Write-Host ""
Write-Host "Reminder: point consumers at the Gateway, not at services directly." -ForegroundColor Yellow
Write-Host "  UBIS_Web/appsettings.json -> MicroserviceUrls:" -ForegroundColor Yellow
Write-Host "    `"Aim`": `"http://$GatewayHost`:$GatewayPort/aim/`"," -ForegroundColor Yellow
Write-Host "    `"MenuGenerator`": `"http://$GatewayHost`:$GatewayPort/menu/`"" -ForegroundColor Yellow
Write-Host "  The trailing slash is REQUIRED - HttpClient.BaseAddress silently drops the last" -ForegroundColor Yellow
Write-Host "  path segment ('/aim') without it. This bit us once already; don't repeat it." -ForegroundColor Yellow

# ----------------------------------------------------------------------------------------------
# 3. Optional build
# ----------------------------------------------------------------------------------------------
if ($Build -or $Run) {
    Write-Host ""
    Write-Host "== Building Gateway ==" -ForegroundColor Cyan
    Push-Location $PSScriptRoot
    try {
        dotnet build --nologo
        if ($LASTEXITCODE -ne 0) { throw "Gateway build failed (exit $LASTEXITCODE)." }
    } finally {
        Pop-Location
    }
}

# ----------------------------------------------------------------------------------------------
# 4. Optional run
# ----------------------------------------------------------------------------------------------
if ($Run) {
    Write-Host ""
    Write-Host "== Starting Gateway on http://$GatewayHost`:$GatewayPort ==" -ForegroundColor Cyan
    $env:ASPNETCORE_ENVIRONMENT = "Development"
    Start-Process -FilePath "dotnet" `
        -ArgumentList "run --no-launch-profile --urls http://$GatewayHost`:$GatewayPort" `
        -WorkingDirectory $PSScriptRoot -WindowStyle Hidden

    if (-not $Test) {
        Write-Host "Gateway starting in the background. Re-run with -Test to verify it came up." -ForegroundColor Green
    }
}

# ----------------------------------------------------------------------------------------------
# 5. Optional smoke test - hits AIM's anonymous financial-years route through the Gateway.
# ----------------------------------------------------------------------------------------------
if ($Test) {
    Write-Host ""
    Write-Host "== Verifying Gateway ==" -ForegroundColor Cyan
    $ready = $false
    for ($i = 0; $i -lt 15; $i++) {
        Start-Sleep -Seconds 2
        try {
            $r = Invoke-WebRequest -Uri "http://$GatewayHost`:$GatewayPort/aim/api/authentication/financial-years" -UseBasicParsing -TimeoutSec 5
            if ($r.StatusCode -eq 200) { $ready = $true; break }
        } catch { }
    }

    if ($ready) {
        Write-Host "Gateway is up and AIM is reachable through it (200 on /aim/api/authentication/financial-years)." -ForegroundColor Green
    } else {
        Write-Host "Gateway did not respond successfully within 30s. Check that AIM is actually reachable at $AimHost`:$AimPort, and that nothing else is bound to port $GatewayPort." -ForegroundColor Red
        exit 1
    }
}

Write-Host ""
Write-Host "Done." -ForegroundColor Cyan
