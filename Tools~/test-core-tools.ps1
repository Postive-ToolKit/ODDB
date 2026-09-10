#!/usr/bin/env pwsh
$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/core-artifact.ps1"
$root = Split-Path -Parent $PSScriptRoot
$temp = Join-Path ([IO.Path]::GetTempPath()) ('oddb-core-tools-' + [Guid]::NewGuid().ToString('N'))

function Expect-Failure([scriptblock]$Action, [string]$Message) {
    try { & $Action } catch {
        if ($_.Exception.Message.Contains($Message)) { return }
        throw
    }
    throw "Expected rejection containing: $Message"
}

try {
    New-Item "$temp/Source~/ODDB.Core" -ItemType Directory -Force | Out-Null
    New-Item "$temp/Plugins" -ItemType Directory | Out-Null
    Copy-Item "$root/Tools~" "$temp/Tools~" -Recurse
    Copy-Item "$root/Source~/core-source.json" "$temp/Source~/core-source.json"
    $pin = Get-Content "$temp/Source~/core-source.json" -Raw | ConvertFrom-Json
    foreach ($entry in $pin.files.PSObject.Properties) {
        $target = Join-Path "$temp/Source~/ODDB.Core" $entry.Name
        New-Item (Split-Path $target -Parent) -ItemType Directory -Force | Out-Null
        Copy-Item (Join-Path "$root/Source~/ODDB.Core" $entry.Name) $target
    }
    foreach ($name in @('ODDB.Core.dll', 'ODDB.Core.dll.sha256', 'core-artifact.json')) {
        Copy-Item "$root/Plugins/$name" "$temp/Plugins/$name"
    }
    $verify = { & "$temp/Tools~/verify-core.ps1" }
    & $verify

    $source = "$temp/Source~/ODDB.Core/ODDatabase.cs"
    $originalSource = [IO.File]::ReadAllText($source)
    Write-CoreText $source ($originalSource.Replace("`r`n", "`n").Replace("`n", "`r`n"))
    & $verify
    Write-CoreText $source ($originalSource + "`n// changed input`n")
    Expect-Failure $verify 'Core source differs'
    Write-CoreText $source $originalSource

    $extra = "$temp/Source~/ODDB.Core/Unpinned.cs"
    Write-CoreText $extra '// unpinned input'
    Expect-Failure $verify 'Unpinned Core source file'
    Remove-Item $extra
    Move-Item $source "$temp/missing-source"
    Expect-Failure $verify 'Core source differs'
    Move-Item "$temp/missing-source" $source

    $dll = "$temp/Plugins/ODDB.Core.dll"
    $bytes = [IO.File]::ReadAllBytes($dll)
    $bytes[0] = $bytes[0] -bxor 1
    [IO.File]::WriteAllBytes($dll, $bytes)
    Expect-Failure $verify 'do not match'
    Copy-Item "$root/Plugins/ODDB.Core.dll" $dll -Force

    Write-CoreText "$temp/Plugins/ODDB.Core.dll.sha256" 'stale hash'
    Expect-Failure $verify 'do not match'
    Copy-Item "$root/Plugins/ODDB.Core.dll.sha256" "$temp/Plugins/ODDB.Core.dll.sha256" -Force
    $artifact = Get-Content "$temp/Plugins/core-artifact.json" -Raw | ConvertFrom-Json
    $artifact.sourceCommit = '0000000000000000000000000000000000000000'
    Write-CoreText "$temp/Plugins/core-artifact.json" ($artifact | ConvertTo-Json)
    Expect-Failure $verify 'do not match'
    Write-Host 'ODDB_CORE_TOOL_TESTS failures=0 checks=8'
} finally {
    if (Test-Path $temp) { Remove-Item $temp -Recurse -Force }
}
