# The checks before a release, and its notes (row R1, .github/workflows/release.yml).
# Usage: ./tools/release.ps1 [-Notes <file>]
# Prints the version (VersionPrefix in Directory.Build.props) and exits non-zero when it cannot be released: CHANGELOG.md
# has no section for it, nuget.org has it, or the tag v<version> exists. Writes the CHANGELOG section to -Notes.

param(
    [string] $Notes = ""
)

$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)

[xml] $props = Get-Content "Directory.Build.props" -Raw
$version = ($props.Project.PropertyGroup | Where-Object { $_.VersionPrefix } | Select-Object -First 1).VersionPrefix
if ([string]::IsNullOrWhiteSpace($version)) {
    Write-Host "FAIL Directory.Build.props has no VersionPrefix."
    exit 1
}

$failures = [System.Collections.Generic.List[string]]::new()

# The section of the version: from its heading to the next version's heading.
$lines = Get-Content "CHANGELOG.md" -Encoding utf8
$start = -1
for ($i = 0; $i -lt $lines.Count; $i++) {
    if ($lines[$i] -match "^## \[$([regex]::Escape($version))\]") {
        $start = $i
        break
    }
}

if ($start -lt 0) {
    $failures.Add("CHANGELOG.md has no section '## [$version]'.")
}
else {
    $end = $lines.Count
    for ($i = $start + 1; $i -lt $lines.Count; $i++) {
        if ($lines[$i] -match '^## \[') {
            $end = $i
            break
        }
    }

    $section = ($lines[($start + 1)..($end - 1)] -join "`n").Trim()
    if ($section -eq "") {
        $failures.Add("The section '## [$version]' of CHANGELOG.md is empty.")
    }
    elseif ($Notes -ne "") {
        $folder = Split-Path $Notes -Parent
        if ($folder -ne "") {
            New-Item -ItemType Directory -Force $folder | Out-Null
        }

        Set-Content $Notes $section -Encoding utf8
    }
}

# A published version cannot be replaced on nuget.org, only unlisted.
$index = "https://api.nuget.org/v3-flatcontainer/auraorm/index.json"
try {
    $published = (Invoke-RestMethod $index).versions
}
catch {
    if ($_.Exception.Response.StatusCode.value__ -eq 404) {
        $published = @()
    }
    else {
        throw
    }
}

if ($published -contains $version.ToLowerInvariant()) {
    $failures.Add("nuget.org already has AuraORM $version.")
}

git rev-parse --quiet --verify "refs/tags/v$version" | Out-Null
if ($LASTEXITCODE -eq 0) {
    $failures.Add("The tag v$version exists.")
}

$global:LASTEXITCODE = 0
if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL $_" }
    exit 1
}

Write-Host "AuraORM $version can be released."
if ($env:GITHUB_OUTPUT) {
    "version=$version" | Out-File -Append -Encoding utf8 $env:GITHUB_OUTPUT
}
