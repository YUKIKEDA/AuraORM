# Starts and stops the databases of the integration tests in WSL containers (wslc.exe), and runs the tests against them.
# The images and versions come from tools/databases.json, which CI reads too (.cursor/rules/containers.mdc).
# Docker is not used. CI runs the same databases as GitHub Actions service containers; CI is the merge gate, and a local
# run is a development aid.
#
# Usage:
#   ./tools/db.ps1 up   [-Version latest|min] [-Only postgresql,mysql,mariadb,sqlserver]
#   ./tools/db.ps1 test [-Only ...]      runs tests/AuraORM.Integration.Tests with AURA_TEST_* set for the running databases
#   ./tools/db.ps1 env  [-Only ...]      prints the AURA_TEST_* assignments for PowerShell
#   ./tools/db.ps1 down [-Only ...]
param(
    [Parameter(Mandatory = $true, Position = 0)]
    [ValidateSet("up", "down", "test", "env")]
    [string] $Command,

    [ValidateSet("latest", "min")]
    [string] $Version = "latest",

    [string[]] $Only = @()
)

$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)

# wslc.exe ships with WSL 2.9.3 or later; it is not always on PATH.
$wslc = Join-Path $env:ProgramFiles "WSL\wslc.exe"
if (-not (Test-Path $wslc)) {
    Write-Error "wslc.exe was not found at $wslc. Update WSL (wsl --update) to 2.9.3 or later."
    exit 1
}

$config = Get-Content tools/databases.json -Raw | ConvertFrom-Json
$selected = @($config.databases | Where-Object { $Only.Count -eq 0 -or $Only -contains $_.name })

function ContainerName($database) { "aura-test-$($database.name)" }

function ConnectionString($database) { $database.connectionString.Replace("{port}", [string]$database.localPort) }

function IsRunning($database) {
    # wslc prints one JSON object per running container.
    $names = & $wslc container list --format json 2>$null |
        Where-Object { $_.Trim().StartsWith("{") } |
        ForEach-Object { ($_ | ConvertFrom-Json).Names }
    return @($names) -contains (ContainerName $database)
}

function Start-Database($database) {
    $tag = if ($Version -eq "min") { $database.versions[0] } else { $database.versions[-1] }
    $name = ContainerName $database
    if (IsRunning $database) {
        Write-Host "==> $name is already running"
        return
    }

    [string[]] $arguments = @("run", "-d", "--rm", "--name", $name, "-p", "$($database.localPort):$($database.port)")
    foreach ($property in $database.environment.PSObject.Properties) {
        $arguments += @("-e", "$($property.Name)=$($property.Value)")
    }

    $arguments += "$($database.image):$tag"
    Write-Host "==> Starting $name ($($database.image):$tag) on port $($database.localPort)"
    & $wslc @arguments | Out-Null
    if ($LASTEXITCODE -ne 0) {
        Write-Error "wslc run failed for $name with exit code $LASTEXITCODE."
        exit $LASTEXITCODE
    }

    # Wait for the database's own health check, at most three minutes.
    [string[]] $health = @("exec", $name) + @($database.health)
    $deadline = (Get-Date).AddMinutes(3)
    while ($true) {
        & $wslc @health *> $null
        if ($LASTEXITCODE -eq 0) {
            break
        }

        if ((Get-Date) -gt $deadline) {
            Write-Error "$name did not become healthy in three minutes. See: $wslc container logs $name"
            exit 1
        }

        Start-Sleep -Seconds 2
    }

    Write-Host "==> $name is ready"
}

function Stop-Database($database) {
    $name = ContainerName $database
    if (IsRunning $database) {
        Write-Host "==> Stopping $name"
        & $wslc container stop $name | Out-Null
    }
}

switch ($Command) {
    "up" {
        foreach ($database in $selected) { Start-Database $database }
    }
    "down" {
        foreach ($database in $selected) { Stop-Database $database }
    }
    "env" {
        foreach ($database in $selected | Where-Object { IsRunning $_ }) {
            Write-Output "`$env:AURA_TEST_$($database.name.ToUpperInvariant()) = '$(ConnectionString $database)'"
        }
    }
    "test" {
        $running = @($selected | Where-Object { IsRunning $_ })
        if ($running.Count -eq 0) {
            Write-Error "No database is running. Start them with: ./tools/db.ps1 up"
            exit 1
        }

        foreach ($database in $running) {
            [Environment]::SetEnvironmentVariable("AURA_TEST_$($database.name.ToUpperInvariant())", (ConnectionString $database))
        }

        $env:AURA_TEST_REQUIRE = ($running | ForEach-Object { $_.name }) -join ","
        dotnet test --project tests/AuraORM.Integration.Tests/AuraORM.Integration.Tests.csproj
        exit $LASTEXITCODE
    }
}
