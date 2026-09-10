#!/usr/bin/env pwsh
[CmdletBinding()]
param(
    [ValidateSet('Release', 'Debug')][string]$Configuration = 'Release',
    [string]$CoreProjectPath = ''
)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/core-artifact.ps1"
$packageRoot = Split-Path -Parent $PSScriptRoot
$pin = Get-Content "$packageRoot/Source~/core-source.json" -Raw | ConvertFrom-Json
if ([string]::IsNullOrWhiteSpace($CoreProjectPath)) {
    $CoreProjectPath = "$packageRoot/Source~/ODDB.Core"
}
$coreRoot = (Resolve-Path $CoreProjectPath).Path
Assert-CoreSource $coreRoot $pin

dotnet build "$coreRoot/ODDB.Core.csproj" -c $Configuration --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw "Core build failed: $LASTEXITCODE" }
Assert-CoreSource $coreRoot $pin
$dll = "$coreRoot/bin/$Configuration/netstandard2.1/ODDB.Core.dll"
$hash = (Get-FileHash $dll -Algorithm SHA256).Hash.ToLowerInvariant()
$sdk = & dotnet --version
if ($LASTEXITCODE -ne 0) { throw 'Cannot determine build SDK.' }
[xml]$project = Get-Content "$coreRoot/ODDB.Core.csproj" -Raw
$artifact = [ordered]@{
    assembly = 'ODDB.Core'
    version = [string]$project.Project.PropertyGroup.Version
    targetFramework = 'netstandard2.1'
    sourceRepository = $pin.repository
    sourceCommit = $pin.commit
    sourceTree = $pin.tree
    sha256 = $hash
    buildSdk = $sdk.Trim()
    configuration = $Configuration
    dependency = 'Newtonsoft.Json 13.0.3'
    license = 'MIT'
}
Copy-Item $dll "$packageRoot/Plugins/ODDB.Core.dll" -Force
Write-CoreText "$packageRoot/Plugins/ODDB.Core.dll.sha256" "$hash  ODDB.Core.dll`n"
Write-CoreText "$packageRoot/Plugins/core-artifact.json" (($artifact | ConvertTo-Json) + "`n")
& "$PSScriptRoot/verify-core.ps1"
