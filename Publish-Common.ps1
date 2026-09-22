# Shared deploy logic dot-sourced by every UBIS2 publish/deploy entry point: root-level
# Deploy-FromMaster.ps1 and Setup-AllServices.ps1, plus Others/publish-staging/
# Publish-All-Microservices.ps1, Republish-Microservices.ps1, Redeploy-AIM-PreBudget.ps1, and
# Redeploy-PreBudget.ps1. Not meant to be run directly - each entry point sets the SQL/IP/Only
# variables and then calls Invoke-Deploy. Editing anything in here changes behavior for every one
# of those at once, which is the point - one place to fix a bug or add a service, instead of
# several scripts silently drifting apart.
#
# Moved to the repo ROOT 2026-08-31 (was Others/publish-staging/Publish-Common.ps1) - Others/ is
# entirely gitignored (client decision, 2026-08-14: "not meant for the shared team repo"), which
# meant this file could never actually be distributed to a server via Deploy-FromMaster.ps1's own
# `git fetch` + `git reset --hard origin/master` step - it had to already exist by some earlier
# manual copy and would silently go stale there forever after. Tracking it at the root (not
# gitignored) fixes that: a `git reset --hard origin/master` on any server now genuinely delivers
# whatever is committed here, same as every other tracked file.

$ErrorActionPreference = "Stop"
Import-Module WebAdministration

function Get-UbisServiceInventory {
    @(
        @{ Name = "AIM";           ProjectPath = "C:\UBIS2\Core\AIM\WebApi\AIM.WebApi.csproj";                     AppSettings = "C:\UBIS2\Core\AIM\WebApi\appsettings.json";                     Site = "ubis-aim";         DefaultPool = "aim-pool";         Path = "C:\inetpub\wwwroot\ubis-aim";         Port = 5000; NeedsSql = $true;  SmokePath = "/swagger/index.html" }
        @{ Name = "MenuGenerator";  ProjectPath = "C:\UBIS2\Core\MenuGenerator\WebApi\MenuGenerator.WebApi.csproj"; AppSettings = "C:\UBIS2\Core\MenuGenerator\WebApi\appsettings.json";           Site = "ubis-menuservice"; DefaultPool = "MenuGenerator";     Path = "C:\inetpub\wwwroot\ubis-menuservice"; Port = 5001; NeedsSql = $true;  SmokePath = "/swagger/index.html" }
        @{ Name = "LogWriter";      ProjectPath = "C:\UBIS2\Core\LogWriter\LogWriter.csproj";                        AppSettings = "C:\UBIS2\Core\LogWriter\appsettings.json";                      Site = "ubis-logwriter";   DefaultPool = "ubis_LogWriter";   Path = "C:\inetpub\wwwroot\ubis-logwriter";   Port = 5002; NeedsSql = $true;  SmokePath = "/swagger/index.html" }
        @{ Name = "PreBudget";      ProjectPath = "C:\UBIS2\Domain\PreBudget\WebApi\PreBudget.WebApi.csproj";        AppSettings = "C:\UBIS2\Domain\PreBudget\WebApi\appsettings.json";             Site = "ubis-prebudget";   DefaultPool = "ubis-prebudget";   Path = "C:\inetpub\wwwroot\ubis-prebudget";   Port = 5010; NeedsSql = $true;  SmokePath = "/swagger/index.html" }
        @{ Name = "ECL";            ProjectPath = "C:\UBIS2\Domain\ECL\WebApi\ECL.WebApi.csproj";                    AppSettings = "C:\UBIS2\Domain\ECL\WebApi\appsettings.json";                   Site = "ubis-ecl";         DefaultPool = "ubis-ecl";         Path = "C:\inetpub\wwwroot\ubis-ecl";         Port = 5011; NeedsSql = $true;  SmokePath = "/swagger/index.html" }
        @{ Name = "Reporting";      ProjectPath = "C:\UBIS2\Core\Reporting\WebApi\Reporting.WebApi.csproj";         AppSettings = "C:\UBIS2\Core\Reporting\WebApi\appsettings.json";               Site = "ubis-reporting";   DefaultPool = "ubis-reporting";   Path = "C:\inetpub\wwwroot\ubis-reporting";   Port = 5003; NeedsSql = $true;  SmokePath = "/health" }
        @{ Name = "Gateway";        ProjectPath = "C:\UBIS2\Gateway\UBIS.ApiGateway\UBIS.ApiGateway.csproj";         AppSettings = $null;                                                            Site = "ubis-gateway";     DefaultPool = "ubis_Gateway";     Path = "C:\inetpub\wwwroot\ubis-gateway";     Port = 5150; NeedsSql = $false; SmokePath = "/aim/api/authentication/financial-years" }
        @{ Name = "Email";          ProjectPath = "C:\UBIS2\Core\Email\WebApi\Email.WebApi.csproj";                  AppSettings = "C:\UBIS2\Core\Email\WebApi\appsettings.json";                   Site = "ubis-email";       DefaultPool = "ubis_Email";       Path = "C:\inetpub\wwwroot\ubis-email";       Port = 5004; NeedsSql = $true;  SmokePath = "/health" }
        @{ Name = "UserProfile";    ProjectPath = "C:\UBIS2\Shared\UserProfile\WebApi\UserProfile.WebApi.csproj";    AppSettings = "C:\UBIS2\Shared\UserProfile\WebApi\appsettings.json";           Site = "ubis-userprofile"; DefaultPool = "ubis_UserProfile"; Path = "C:\inetpub\wwwroot\ubis-userprofile"; Port = 5104; NeedsSql = $true;  SmokePath = "/swagger/index.html" }
        @{ Name = "UBIS_Web";       ProjectPath = "C:\UBIS2\UBIS_Web\UBIS_Web.csproj";                                AppSettings = $null;                                                            Site = "ubis-web";         DefaultPool = "ubis_Web";         Path = "C:\inetpub\wwwroot\ubis-web";         Port = 80;   NeedsSql = $false; SmokePath = "/healthz" }
    )
}

function Resolve-PoolName($svc) {
    $site_obj = Get-Website -Name $svc.Site -ErrorAction SilentlyContinue
    if ($site_obj -and $site_obj.applicationPool) { return $site_obj.applicationPool }
    return $svc.DefaultPool
}

function Stop-SiteIfExists($SiteName) {
    $site_obj = Get-Website -Name $SiteName -ErrorAction SilentlyContinue
    if (-not $site_obj) { return }
    try { Stop-Website -Name $SiteName -ErrorAction Stop } catch { }
    $poolName = $site_obj.applicationPool
    if ($poolName -and (Test-Path "IIS:\AppPools\$poolName")) {
        try { Stop-WebAppPool -Name $poolName -ErrorAction Stop } catch { }
    }
}

function Set-ConnectionString($AppSettingsPath, $ConnectionString) {
    if (-not $AppSettingsPath -or -not (Test-Path $AppSettingsPath)) { return }
    $raw = Get-Content -Path $AppSettingsPath -Raw
    $updated = $raw -replace '("DefaultConnection"\s*:\s*)"[^"]*"', "`$1`"$ConnectionString`""
    if ($updated -ne $raw) {
        Set-Content -Path $AppSettingsPath -Value $updated -NoNewline
        Write-Host "   - updated DefaultConnection in $AppSettingsPath" -ForegroundColor DarkGray
    }
}

function Publish-Service($ProjectPath, $TargetDir, $AspNetCoreEnvironment) {
    Write-Host "== Publishing $ProjectPath -> $TargetDir ==" -ForegroundColor Cyan

    if (Test-Path $TargetDir) {
        Remove-Item -Path (Join-Path $TargetDir "*") -Recurse -Force -ErrorAction SilentlyContinue
    } else {
        New-Item -ItemType Directory -Path $TargetDir -Force | Out-Null
    }

    $projectDir = Split-Path $ProjectPath -Parent
    Push-Location $projectDir
    try {
        # Must pass $ProjectPath explicitly (not just cd into its folder and let dotnet
        # auto-detect) - UBIS_Web's folder has both UBIS_Web.csproj and UBIS_Web.slnx, and
        # `dotnet publish` with no project argument picks the .slnx over the .csproj, which
        # triggers NETSDK1194 ("--output isn't supported when building a solution") and risks
        # every project in that solution copying its output into the same $TargetDir if the
        # .slnx ever grows to reference more than one project.
        dotnet publish $ProjectPath -c Release -o $TargetDir
        if ($LASTEXITCODE -ne 0) { throw "Publish failed for $ProjectPath (exit $LASTEXITCODE)." }
    } finally {
        Pop-Location
    }

    $webConfigPath = Join-Path $TargetDir "web.config"
    if (-not (Test-Path $webConfigPath)) {
        throw "Publish for $ProjectPath did not produce web.config in $TargetDir - something is still wrong."
    }

    # `dotnet publish` always bakes ASPNETCORE_ENVIRONMENT="Production" into the generated
    # web.config (the .NET SDK's own default, not something this repo configures) - and web.config
    # is read directly by the IIS ASP.NET Core Module, so it wins over the site-level
    # environmentVariables set on IIS:\Sites\<name> below, even though that one is what persists
    # across republishes. Found 2026-08-25 (client testing feedback: login hanging ~10-20s then
    # 503 through the Gateway, root-caused to AIM's Production Redis (192.168.100.2) being
    # unreachable from this dev box) - switching only the site-level setting didn't help because
    # this file kept overriding it back to Production on every republish. Patched here so the
    # environment actually chosen by the caller (see $AspNetCoreEnvironment below) sticks.
    if ($AspNetCoreEnvironment -and $AspNetCoreEnvironment -ne "Production") {
        $rawWebConfig = Get-Content -Path $webConfigPath -Raw
        $updatedWebConfig = $rawWebConfig -replace '(ASPNETCORE_ENVIRONMENT"\s+value=")Production(")', "`${1}$AspNetCoreEnvironment`${2}"
        if ($updatedWebConfig -ne $rawWebConfig) {
            Set-Content -Path $webConfigPath -Value $updatedWebConfig -NoNewline
            Write-Host "   - web.config ASPNETCORE_ENVIRONMENT -> $AspNetCoreEnvironment" -ForegroundColor DarkGray
        }
    }
}

function Grant-SqlAccess($PoolName, $SqlServerInstance, $DatabaseName) {
    Write-Host "== Granting SQL access to IIS APPPOOL\$PoolName ==" -ForegroundColor Cyan
    sqlcmd -S $SqlServerInstance -E -Q @"
IF NOT EXISTS (SELECT * FROM sys.server_principals WHERE name = 'IIS APPPOOL\$PoolName')
    CREATE LOGIN [IIS APPPOOL\$PoolName] FROM WINDOWS;
"@
    sqlcmd -S $SqlServerInstance -E -d $DatabaseName -Q @"
IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = 'IIS APPPOOL\$PoolName')
    CREATE USER [IIS APPPOOL\$PoolName] FOR LOGIN [IIS APPPOOL\$PoolName];
ALTER ROLE db_datareader ADD MEMBER [IIS APPPOOL\$PoolName];
ALTER ROLE db_datawriter ADD MEMBER [IIS APPPOOL\$PoolName];
"@
}

function Get-SmokeUrl($svc) {
    # Resolves the actual bound IP from the live site rather than assuming one - a site created
    # just now binds to whatever $IPAddress was, but a site that already existed before this run
    # may still be bound to some other specific address from an earlier deploy, so this always
    # asks IIS what it is actually listening on instead of guessing.
    $ip = "localhost"
    $site_obj = Get-Website -Name $svc.Site -ErrorAction SilentlyContinue
    if ($site_obj) {
        $bindingInfo = ($site_obj.bindings.Collection | Select-Object -First 1).bindingInformation
        $boundIp = ($bindingInfo -split ':')[0]
        if ($boundIp -and $boundIp -ne '*') { $ip = $boundIp }
    }
    return "http://${ip}:$($svc.Port)$($svc.SmokePath)"
}

function Invoke-Deploy {
    param(
        [string]$SqlServerInstance,
        [string]$DatabaseName,
        [string]$IPAddress,
        [string[]]$Only = @(),
        # "Production" (default) matches the real deploy target's appsettings.Production.json -
        # remote Redis/SQL, reachable from that box. Pass "Development" for a dev machine that
        # can't reach those (uses appsettings.Development.json, falling back to the base
        # appsettings.json's local Redis/SQL instead) - see Publish-Service's own comment for why
        # this has to patch web.config directly, not just the site-level IIS setting.
        [string]$AspNetCoreEnvironment = "Production"
    )

    $sqlServerJsonEscaped = $SqlServerInstance -replace '\\', '\\'
    $ConnectionString = "Server=$sqlServerJsonEscaped;Database=$DatabaseName;Trusted_Connection=True;TrustServerCertificate=True;Encrypt=True;"
    Write-Host "== SQL Server: $SqlServerInstance  Database: $DatabaseName  Bind: $IPAddress ==" -ForegroundColor Cyan

    $allServices = Get-UbisServiceInventory
    $services = $allServices
    if ($Only.Count -gt 0) {
        $services = $services | Where-Object { $Only -contains $_.Name }
        # Derived from the actual inventory, not hand-copied (found 2026-08-31: this list had
        # drifted stale, silently missing "ECL" and "Reporting" even though both are real,
        # publishable entries in Get-UbisServiceInventory - misleading anyone who mistyped a name
        # and got told ECL wasn't valid when it actually is).
        if (-not $services) { throw "Nothing matched `$Only = $($Only -join ','). Valid names: $($allServices.Name -join ', ')." }
    }

    # 0. Stop the standalone Gateway process if one is running (started by
    #    Hand-over-developer\Start-UbisEnvironment.ps1 at logon) - it and the IIS site can't both
    #    bind port 5150.
    if ($services | Where-Object { $_.Name -eq "Gateway" }) {
        $standaloneGateway = Get-CimInstance Win32_Process -Filter "Name='dotnet.exe'" -ErrorAction SilentlyContinue |
            Where-Object { $_.CommandLine -like "*5150*" -or $_.CommandLine -like "*UBIS.ApiGateway*" }
        if ($standaloneGateway) {
            Write-Host "== Stopping standalone Gateway process(es) on port 5150 before IIS takes over ==" -ForegroundColor Cyan
            $standaloneGateway | ForEach-Object {
                Write-Host ("  Stopping PID {0}" -f $_.ProcessId)
                Stop-Process -Id $_.ProcessId -Force -ErrorAction SilentlyContinue
            }
            Start-Sleep -Seconds 2
        }
    }

    # 1. Stop any already-running sites first, so publish never fights a file lock.
    foreach ($svc in $services) { Stop-SiteIfExists $svc.Site }

    # 2. Write the single connection string into every service's source appsettings.json before
    #    publishing, so the published copy picks it up in the same step.
    foreach ($svc in ($services | Where-Object { $_.NeedsSql })) { Set-ConnectionString $svc.AppSettings $ConnectionString }

    # 3. Publish every selected service directly to its IIS physical path.
    foreach ($svc in $services) { Publish-Service $svc.ProjectPath $svc.Path $AspNetCoreEnvironment }

    # 4. Create app pool + site for anything not already IIS-hosted. Skipped entirely for sites
    #    that already exist - never touches their existing config (including their existing IP
    #    binding).
    foreach ($svc in $services) {
        $poolName = Resolve-PoolName $svc

        if (-not (Test-Path "IIS:\AppPools\$poolName")) {
            Write-Host "== Creating app pool $poolName ==" -ForegroundColor Cyan
            New-WebAppPool -Name $poolName
            Set-ItemProperty "IIS:\AppPools\$poolName" -Name managedRuntimeVersion -Value ""
            Set-ItemProperty "IIS:\AppPools\$poolName" -Name managedPipelineMode -Value "Integrated"
        }

        # Re-applied on every run, not just at creation (bug found 2026-08-03: a pool created before
        # this idle-timeout/AlwaysRunning logic existed never picked it up on later re-deploys,
        # since it was previously gated behind the "if newly created" branch above - that silently
        # left AIM/Gateway cold-starting on the first request after any idle period, adding up to
        # ~50s to a login while the resilience handler retried through the slow first attempt).
        Set-ItemProperty "IIS:\AppPools\$poolName" -Name startMode -Value "AlwaysRunning"
        Set-ItemProperty "IIS:\AppPools\$poolName" -Name processModel.idleTimeout -Value ([TimeSpan]::Zero)

        if (-not (Get-Website -Name $svc.Site -ErrorAction SilentlyContinue)) {
            # UBIS_Web is the one site developer/tester machines actually browse to - it must bind
            # "*" (all interfaces), not the loopback-only $IPAddress used for the backend
            # microservices (which only ever talk to each other over 127.0.0.1 on this same box, per
            # the corporate policy noted in UBIS_Web/appsettings.Production.json). Binding it to
            # loopback would make it unreachable from any other machine on the VLAN - the exact bug
            # found 2026-08-10 (site created with IPAddress 127.0.0.1, port 80 unreachable remotely).
            $siteIp = if ($svc.Name -eq "UBIS_Web") { "*" } else { $IPAddress }
            Write-Host "== Creating site $($svc.Site) (${siteIp}:$($svc.Port)) ==" -ForegroundColor Cyan
            New-Website -Name $svc.Site -PhysicalPath $svc.Path `
                -ApplicationPool $poolName -Port $svc.Port -IPAddress $siteIp
        }

        # Belt-and-suspenders copy of ASPNETCORE_ENVIRONMENT at the site level (applicationHost.config),
        # persisted across republishes - Publish-Service above patches the actual authoritative copy
        # (web.config, which the IIS ASP.NET Core Module reads directly and which wins over this one).
        # Now always synced to $AspNetCoreEnvironment, not just set-once-if-missing (that guard is
        # exactly why an earlier manual attempt to flip this to Development silently no-op'd on a
        # site that already had Production set from its very first deploy).
        $envVarsPath = "IIS:\Sites\$($svc.Site)"
        $existing = Get-WebConfigurationProperty -PSPath $envVarsPath -Filter "system.webServer/aspNetCore/environmentVariables" -Name "." -ErrorAction SilentlyContinue
        $alreadySet = $existing.Collection | Where-Object { $_.name -eq "ASPNETCORE_ENVIRONMENT" }
        if (-not $alreadySet) {
            Write-Host "== Setting ASPNETCORE_ENVIRONMENT=$AspNetCoreEnvironment for $($svc.Site) ==" -ForegroundColor Cyan
            Add-WebConfigurationProperty -PSPath $envVarsPath -Filter "system.webServer/aspNetCore/environmentVariables" -Name "." `
                -Value @{ name = "ASPNETCORE_ENVIRONMENT"; value = $AspNetCoreEnvironment }
        } elseif ($alreadySet.value -ne $AspNetCoreEnvironment) {
            Write-Host "== Updating ASPNETCORE_ENVIRONMENT $($alreadySet.value) -> $AspNetCoreEnvironment for $($svc.Site) ==" -ForegroundColor Cyan
            Set-WebConfigurationProperty -PSPath $envVarsPath -Filter "system.webServer/aspNetCore/environmentVariables/add[@name='ASPNETCORE_ENVIRONMENT']" -Name "value" -Value $AspNetCoreEnvironment
        }
    }

    # 5. Grant SQL access to whichever app pool identities actually talk to the DB directly. Uses
    #    the real, resolved pool name (not an assumed one) - idempotent, safe to re-run.
    foreach ($svc in ($services | Where-Object { $_.NeedsSql })) { Grant-SqlAccess (Resolve-PoolName $svc) $SqlServerInstance $DatabaseName }

    # 6. Register the "ubis2" Event Log source (Windows Logs > Application) that LogWriter's
    #    Serilog EventLog sink writes to. Idempotent.
    if (-not [System.Diagnostics.EventLog]::SourceExists("ubis2")) {
        Write-Host "== Registering Event Log source 'ubis2' ==" -ForegroundColor Cyan
        New-EventLog -LogName Application -Source "ubis2"
    }

    # 7. Start every site's app pool and the site itself.
    foreach ($svc in $services) {
        $site_obj = Get-Website -Name $svc.Site -ErrorAction SilentlyContinue
        if (-not $site_obj) { continue }

        $poolName = $site_obj.applicationPool
        if ($poolName -and (Test-Path "IIS:\AppPools\$poolName")) {
            try { Start-WebAppPool -Name $poolName -ErrorAction Stop } catch { }
        }
        try { Start-Website -Name $svc.Site -ErrorAction Stop } catch { }
    }

    # 8. Smoke test all selected services.
    Write-Host ""
    Write-Host "== Smoke test ==" -ForegroundColor Cyan
    Start-Sleep -Seconds 5
    foreach ($svc in $services) {
        $url = Get-SmokeUrl $svc
        try {
            $r = Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 15
            Write-Host ("  {0,-15} {1} -> {2}" -f $svc.Name, $url, $r.StatusCode) -ForegroundColor Green
        } catch {
            Write-Host ("  {0,-15} {1} -> FAILED: {2}" -f $svc.Name, $url, $_.Exception.Message) -ForegroundColor Red
        }
    }

    Write-Host ""
    Write-Host "Done." -ForegroundColor Cyan
}
