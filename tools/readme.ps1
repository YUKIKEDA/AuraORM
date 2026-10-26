# Compiles and runs the C# examples of README.md against the packed package, as a consumer would (row R1).
# Usage: ./tools/readme.ps1 -Version <package version> [-Feed artifacts/packages]
# Exits non-zero when README.ja.md has other code, when the examples do not compile, or when they do not run.
#
# The ```csharp blocks are taken in order. A block that declares a type goes into a file of its own; the other blocks
# are the program, in order. The project is made outside the repository, so the repository's analyzers do not apply to
# it, with nullable reference types on and their warnings as errors, as README asks of a consumer.

param(
    [Parameter(Mandatory = $true)] [string] $Version,
    [string] $Feed = "artifacts/packages"
)

$ErrorActionPreference = "Stop"
$root = Split-Path $PSScriptRoot -Parent
Set-Location $root

function Get-CSharpBlocks([string] $path) {
    $blocks = [System.Collections.Generic.List[string]]::new()
    $current = $null
    foreach ($line in Get-Content $path -Encoding utf8) {
        if ($null -eq $current) {
            if ($line -eq '```csharp') {
                $current = [System.Text.StringBuilder]::new()
            }
        }
        elseif ($line -eq '```') {
            $blocks.Add($current.ToString())
            $current = $null
        }
        else {
            [void] $current.AppendLine($line)
        }
    }

    return , $blocks
}

$blocks = Get-CSharpBlocks "README.md"
$japanese = Get-CSharpBlocks "README.ja.md"
if ($blocks.Count -eq 0) {
    Write-Host "FAIL README.md has no csharp block."
    exit 1
}

if (($blocks -join "`n----`n") -ne ($japanese -join "`n----`n")) {
    Write-Host "FAIL The csharp blocks of README.ja.md differ from README.md."
    exit 1
}

[xml] $packages = Get-Content "Directory.Packages.props" -Raw
$sqlite = ($packages.Project.ItemGroup.PackageVersion | Where-Object { $_.Include -eq "Microsoft.Data.Sqlite" }).Version
$feedPath = (Resolve-Path $Feed).Path

$work = Join-Path ([System.IO.Path]::GetTempPath()) ("aura-readme-" + [guid]::NewGuid().ToString("N"))
New-Item -ItemType Directory $work | Out-Null
try {
    @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
    <WarningsAsErrors>nullable</WarningsAsErrors>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <RestoreAdditionalProjectSources>$feedPath</RestoreAdditionalProjectSources>
    <RestorePackagesPath>$(Join-Path $work 'packages')</RestorePackagesPath>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="AuraORM" Version="$Version" />
    <PackageReference Include="Microsoft.Data.Sqlite" Version="$sqlite" />
  </ItemGroup>
</Project>
"@ | Set-Content (Join-Path $work "Readme.csproj") -Encoding utf8

    $program = [System.Text.StringBuilder]::new()
    $types = 0
    foreach ($block in $blocks) {
        if ($block -match '(?m)^\s*(public |internal )?(sealed |static |abstract )*(partial )?(record|class|struct|enum|interface) ') {
            $types++
            Set-Content (Join-Path $work "Type$types.cs") $block -Encoding utf8
        }
        else {
            [void] $program.AppendLine($block)
        }
    }

    Set-Content (Join-Path $work "Program.cs") $program.ToString() -Encoding utf8
    Write-Host "README: $($blocks.Count) blocks, $types with types, the rest as the program."

    $output = dotnet run --project (Join-Path $work "Readme.csproj")
    $code = $LASTEXITCODE
    $output | ForEach-Object { Write-Host $_ }
    if ($code -ne 0) {
        Write-Host "FAIL The README examples did not build or run (exit code $code)."
        exit 1
    }

    # A comment that shows SQL is what the example before it prints.
    $shown = $blocks | ForEach-Object { $_ -split "`r?`n" } | Where-Object { $_ -match '^// (SELECT|INSERT|UPDATE|DELETE|WITH) ' }
    foreach ($line in $shown) {
        if ($output -notcontains $line.Substring(3)) {
            Write-Host "FAIL README shows SQL that the example does not print: $($line.Substring(3))"
            exit 1
        }
    }

    Write-Host "The README examples built and ran, and printed the SQL that README shows ($(@($shown).Count) statements)."
}
finally {
    Remove-Item -Recurse -Force $work -ErrorAction SilentlyContinue
}
