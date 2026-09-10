#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/core-artifact.ps1"
$root = Split-Path -Parent $PSScriptRoot
$pin = Get-Content "$root/Source~/core-source.json" -Raw | ConvertFrom-Json
$artifact = Get-Content "$root/Plugins/core-artifact.json" -Raw | ConvertFrom-Json
Assert-CoreSource (Resolve-Path "$root/Source~/ODDB.Core").Path $pin
$hash = (Get-FileHash "$root/Plugins/ODDB.Core.dll" -Algorithm SHA256).Hash.ToLowerInvariant()
$checksum = (Get-Content "$root/Plugins/ODDB.Core.dll.sha256" -Raw).Trim()
if ($artifact.sourceCommit -ne $pin.commit -or $artifact.sourceTree -ne $pin.tree -or
    $artifact.sourceRepository -ne $pin.repository -or $artifact.sha256 -ne $hash -or
    $checksum -ne "$hash  ODDB.Core.dll") {
    throw 'Core DLL, checksum, artifact provenance and source pin do not match.'
}
Write-Host "ODDB_CORE_ARTIFACT commit=$($pin.commit) sha256=$hash"
