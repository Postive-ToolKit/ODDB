#!/usr/bin/env pwsh
# Rebuild ODDB.Core and drop the fresh dll into the Unity package.
#
# Run from anywhere — paths are resolved relative to this script's location.
# Requires .NET SDK (https://dotnet.microsoft.com).

[CmdletBinding()]
param(
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'

$ScriptDir    = Split-Path -Parent $MyInvocation.MyCommand.Path
$PackageRoot  = Resolve-Path (Join-Path $ScriptDir '..')
$CoreProject  = Resolve-Path (Join-Path $PackageRoot '../../../src/ODDB.Core')
$PluginsDir   = Join-Path $PackageRoot 'Plugins'
$Dll          = Join-Path $CoreProject "bin/$Configuration/netstandard2.1/ODDB.Core.dll"

Write-Host "-> building ODDB.Core ($Configuration)"
dotnet build (Join-Path $CoreProject 'ODDB.Core.csproj') -c $Configuration --nologo -v quiet
if ($LASTEXITCODE -ne 0) { throw "dotnet build failed (exit $LASTEXITCODE)" }

if (-not (Test-Path $Dll)) {
    throw "expected dll not found at $Dll"
}

if (-not (Test-Path $PluginsDir)) {
    New-Item -ItemType Directory -Path $PluginsDir | Out-Null
}

Copy-Item -Path $Dll -Destination (Join-Path $PluginsDir 'ODDB.Core.dll') -Force
$Size = (Get-Item (Join-Path $PluginsDir 'ODDB.Core.dll')).Length
Write-Host "-> copied $([System.IO.Path]::GetFileName($Dll)) -> $PluginsDir\"
Write-Host "   $Size bytes"
Write-Host 'done. Unity will recompile on next focus.'
