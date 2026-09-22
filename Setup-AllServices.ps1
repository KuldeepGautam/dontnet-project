# Run this in an ELEVATED PowerShell (Run as Administrator).
#
# One-shot "get this repo running on my own machine's local IIS" script, meant for a brand-new
# developer machine right after cloning. In a single run it builds, hosts (creates IIS site +
# app-pool + binding), and grants SQL access for all 10 UBIS2 microservices - and additionally
# writes this machine's SQL Server / Redis / RabbitMQ locations into every service's own
# appsettings.json before publishing. Wraps Publish-Common.ps1's shared
# Invoke-Deploy logic (build+host+grant-SQL) rather than duplicating it - that file's own header
# explains why: one place to fix a bug or add a service, instead of scripts silently drifting apart.
# This script only adds the Redis/RabbitMQ piece, which Invoke-Deploy itself does not touch (it only
# ever writes the SQL connection string).
#
# HOW TO RUN (from an elevated PowerShell, at the repo root):
#   powershell -ExecutionPolicy Bypass -File .\Setup-AllServices.ps1
# (The Set-ExecutionPolicy line below only takes effect once the script is already running, so it
# can't rescue its own launch on a machine where script execution is disabled outright - use the
# command above for the very first run. Or run this once, permanently, and every .ps1 in this repo
# runs normally from then on:
#   Set-ExecutionPolicy -Scope CurrentUser -ExecutionPolicy RemoteSigned
# )
#
# HOW TO EDIT FOR YOUR MACHINE: change the variables right below this comment block, or just accept
# the defaults - $SqlServerInstance auto-detects this machine's own name via $env:COMPUTERNAME, and
# Redis/RabbitMQ both default to a fresh local install on this same box, so most developers on a
# freshly-provisioned machine won't need to touch anything.
#
# Port scheme: AIM=5000  MenuGenerator=5001  LogWriter=5002  Reporting=5003  Email=5004
#              PreBudget=5010  ECL=5011  Gateway=5150  UserProfile=5104  UBIS_Web=80

Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass -Force

# ====================================================================================================
# EDIT THESE FOR YOUR MACHINE
# ====================================================================================================

# SQL Server instance this machine's SQL Express (or full SQL Server) is running as. Auto-detects
# this machine's own name - override $SqlInstanceName if your local instance isn't "SQLEXPRESS", or
# set $SqlServerInstance directly (e.g. to point at a shared/remote SQL box instead) to bypass the
# auto-detected default entirely.
$SqlServerName = $env:COMPUTERNAME
$SqlInstanceName = "SQLEXPRESS"
$SqlServerInstance = "$SqlServerName\$SqlInstanceName"
$DatabaseName = "UBIS-Dev"

# StackExchange.Redis-format connection string, written into every service that uses Redis (AIM,
# MenuGenerator, UBIS_Web). Each stores this in its own independently-duplicated JSON shape, not
# shared config - see Set-RedisConnectionInFile below for how each shape is handled.
$RedisConnectionString = "localhost:6379,abortConnect=false"

# Host this machine's RabbitMQ broker is reachable on - written into every service's
# RabbitMq:HostName field. Port/UserName/Password are left at their existing "5672"/"guest"/"guest"
# defaults (a fresh local RabbitMQ install with no auth hardening) - edit those by hand in a
# specific service's appsettings.json if your setup differs.
$RabbitMqHostName = "localhost"

# IP address IIS sites bind to. Corporate security policy blocks IIS from binding to "*"/a specific
# LAN IP for the backend microservices - only loopback (127.0.0.1) works. UBIS_Web itself still
# binds "*" regardless (handled inside Invoke-Deploy) so it stays reachable from other machines on
# the network. Do NOT hardcode a machine-specific address here.
$IPAddress = "127.0.0.1"


# ====================================================================================================
# Nothing below this line needs editing.
# ====================================================================================================

. "$PSScriptRoot\Publish-Common.ps1"

# Both Redis and RabbitMq live at different nesting depths in different services' appsettings.json
# (independently duplicated config, not shared - e.g. AIM nests Redis under "CacheConfiguration",
# MenuGenerator nests it under "ConnectionStrings" as a plain string, UBIS_Web keeps it top-level as
# an object with a "Configuration" field) - confirmed by walking every service's actual file rather
# than assuming. These two functions recurse the whole object graph looking for a property with the
# given name, wherever it is, and mutate it in place by shape:
#   - a plain string value -> replaced wholesale with the connection string.
#   - an object with a "Configuration" field -> that field replaced (UBIS_Web's Redis shape).
#   - an object with a "Host"/"HostName" field -> Host/Port/Password parsed out of the connection
#     string and written into the matching fields (AIM's Redis shape; every RabbitMq shape).
# Returns $true if anything changed, so the caller only rewrites the file when something moved.
function Set-ConnectionValueRecursive($obj, $PropertyName, $ConnectionString, $HostFieldName) {
    if ($obj -isnot [System.Management.Automation.PSCustomObject]) { return $false }
    $changed = $false
    foreach ($prop in @($obj.PSObject.Properties)) {
        if ($prop.Name -eq $PropertyName) {
            if ($prop.Value -is [string]) {
                $obj.$PropertyName = $ConnectionString
                $changed = $true
            } elseif ($prop.Value -is [System.Management.Automation.PSCustomObject]) {
                $target = $prop.Value
                if ($target.PSObject.Properties['Configuration']) {
                    $target.Configuration = $ConnectionString
                    $changed = $true
                } elseif ($target.PSObject.Properties[$HostFieldName]) {
                    $hostPortPart = ($ConnectionString -split ',')[0]
                    $hostPortSplit = $hostPortPart -split ':'
                    $target.$HostFieldName = $hostPortSplit[0]
                    if ($hostPortSplit.Count -gt 1 -and $target.PSObject.Properties['Port']) { $target.Port = [int]$hostPortSplit[1] }
                    if ($target.PSObject.Properties['Password']) {
                        $passwordPart = ($ConnectionString -split ',') | Where-Object { $_ -match '^password=' } | Select-Object -First 1
                        if ($passwordPart) { $target.Password = ($passwordPart -replace '^password=', '') }
                    }
                    if ($target.PSObject.Properties['UserName'] -and $ConnectionString -match '^([^:@,]+):') {
                        $target.UserName = $Matches[1]
                    }
                    $changed = $true
                }
            }
        } else {
            if (Set-ConnectionValueRecursive $prop.Value $PropertyName $ConnectionString $HostFieldName) { $changed = $true }
        }
    }
    return $changed
}

function Set-RabbitMqHostNameInFile($AppSettingsPath, $HostName) {
    if (-not $AppSettingsPath -or -not (Test-Path $AppSettingsPath)) { return }
    # -Encoding utf8 is required on read AND write: these files are UTF-8 without a BOM, and Windows
    # PowerShell 5.1's default Get-Content encoding detection misreads that as the system ANSI
    # codepage, silently corrupting every non-ASCII character already in the file (₹, em-dashes,
    # etc.) - confirmed by a dry run against a scratch copy before this script was ever pointed at
    # the real files.
    $json = Get-Content -Path $AppSettingsPath -Raw -Encoding utf8 | ConvertFrom-Json
    if (Set-ConnectionValueRecursive $json "RabbitMq" $HostName "HostName") {
        ($json | ConvertTo-Json -Depth 20) | Set-Content -Path $AppSettingsPath -Encoding utf8 -NoNewline
        Write-Host "   - updated RabbitMq host in $AppSettingsPath" -ForegroundColor DarkGray
    }
}

function Set-RedisConnectionInFile($AppSettingsPath, $ConnectionString) {
    if (-not $AppSettingsPath -or -not (Test-Path $AppSettingsPath)) { return }
    $json = Get-Content -Path $AppSettingsPath -Raw -Encoding utf8 | ConvertFrom-Json
    if (Set-ConnectionValueRecursive $json "Redis" $ConnectionString "Host") {
        ($json | ConvertTo-Json -Depth 20) | Set-Content -Path $AppSettingsPath -Encoding utf8 -NoNewline
        Write-Host "   - updated Redis config in $AppSettingsPath" -ForegroundColor DarkGray
    }
}

Write-Host "== Writing Redis/RabbitMQ locations into every service's appsettings.json ==" -ForegroundColor Cyan
foreach ($svc in (Get-UbisServiceInventory)) {
    # Derived from ProjectPath rather than the inventory's own AppSettings field - that field is
    # deliberately $null for Gateway/UBIS_Web (they don't need the SQL connection string Invoke-Deploy
    # writes), but both still have their own appsettings.json with RabbitMq/Redis sections that do
    # need updating here.
    $appSettingsPath = Join-Path (Split-Path $svc.ProjectPath -Parent) "appsettings.json"
    Set-RabbitMqHostNameInFile $appSettingsPath $RabbitMqHostName
    Set-RedisConnectionInFile $appSettingsPath $RedisConnectionString
}

Invoke-Deploy -SqlServerInstance $SqlServerInstance -DatabaseName $DatabaseName -IPAddress $IPAddress -Only @()
