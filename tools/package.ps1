# Packs AuraORM and checks the package that a consumer receives (row R1, .cursor/rules/dependencies.mdc).
# Usage: ./tools/package.ps1 [-Output artifacts/packages] [-Suffix <prerelease suffix>]
# Writes the .nupkg and .snupkg to -Output and prints the version. Exits non-zero when a check fails.
#
# The checks: the library and its XML docs under lib/net8.0, the generator under analyzers/dotnet/cs, the README, no
# package dependency in the nuspec, and a symbol package next to the package.

param(
    [string] $Output = "artifacts/packages",
    [string] $Suffix = ""
)

$ErrorActionPreference = "Stop"
Set-Location (Split-Path $PSScriptRoot -Parent)

$arguments = @("pack", "src/AuraORM/AuraORM.csproj", "-c", "Release", "-o", $Output)
if ($Suffix -ne "") {
    $arguments += @("--version-suffix", $Suffix)
}

& dotnet @arguments
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$package = Get-ChildItem $Output -Filter "AuraORM.*.nupkg" | Where-Object { $_.Name -notlike "*.snupkg" } | Sort-Object LastWriteTime | Select-Object -Last 1
if ($null -eq $package) {
    Write-Host "No AuraORM package in $Output."
    exit 1
}

$version = $package.Name.Substring("AuraORM.".Length, $package.Name.Length - "AuraORM.".Length - ".nupkg".Length)
$failures = [System.Collections.Generic.List[string]]::new()

Add-Type -AssemblyName System.IO.Compression.FileSystem
$zip = [System.IO.Compression.ZipFile]::OpenRead($package.FullName)
try {
    $entries = $zip.Entries | ForEach-Object { $_.FullName }
    foreach ($required in "lib/net8.0/AuraORM.dll", "lib/net8.0/AuraORM.xml", "analyzers/dotnet/cs/AuraORM.Generators.dll", "README.md") {
        if ($entries -notcontains $required) {
            $failures.Add("The package has no $required.")
        }
    }

    $nuspecEntry = $zip.Entries | Where-Object { $_.FullName -eq "AuraORM.nuspec" }
    $reader = [System.IO.StreamReader]::new($nuspecEntry.Open())
    try {
        [xml] $nuspec = $reader.ReadToEnd()
    }
    finally {
        $reader.Dispose()
    }

    # The runtime library has no package dependency; an empty group per target framework is allowed.
    $dependencies = $nuspec.GetElementsByTagName("dependency")
    foreach ($dependency in $dependencies) {
        $failures.Add("The package depends on $($dependency.GetAttribute('id')) $($dependency.GetAttribute('version')).")
    }
}
finally {
    $zip.Dispose()
}

if (-not (Test-Path (Join-Path $Output "AuraORM.$version.snupkg"))) {
    $failures.Add("There is no symbol package AuraORM.$version.snupkg.")
}

if ($failures.Count -gt 0) {
    $failures | ForEach-Object { Write-Host "FAIL $_" }
    exit 1
}

Write-Host "AuraORM $version passed the package checks."
if ($env:GITHUB_OUTPUT) {
    "version=$version" | Out-File -Append -Encoding utf8 $env:GITHUB_OUTPUT
}
