#!/usr/bin/env pwsh
[CmdletBinding()]
param([Parameter(Mandatory = $true)][string]$CoreRepositoryPath)

$ErrorActionPreference = 'Stop'
. "$PSScriptRoot/core-artifact.ps1"
$root = Split-Path -Parent $PSScriptRoot
$core = (Resolve-Path $CoreRepositoryPath).Path
$commit = & git -C $core rev-parse --verify HEAD
if ($LASTEXITCODE -ne 0) { throw 'CoreRepositoryPath must be a Git checkout.' }
$tree = & git -C $core rev-parse 'HEAD^{tree}'
if ($LASTEXITCODE -ne 0) { throw 'Cannot resolve Core tree.' }
$dirty = & git -C $core status --porcelain
if ($LASTEXITCODE -ne 0 -or $dirty) { throw 'Commit or stash Core changes before syncing.' }
$dirty = & git -C $root status --porcelain -- 'Source~/ODDB.Core' 'Source~/core-source.json'
if ($LASTEXITCODE -ne 0 -or $dirty) { throw 'Commit or stash Unity Core snapshot changes before syncing.' }

$temp = Join-Path ([IO.Path]::GetTempPath()) ('oddb-core-sync-' + [Guid]::NewGuid().ToString('N'))
New-Item $temp -ItemType Directory | Out-Null
try {
    $archive = Join-Path $temp 'core.tar'
    $snapshot = Join-Path $temp 'source'
    New-Item $snapshot -ItemType Directory | Out-Null
    & git -C $core archive --format=tar --output=$archive $commit
    if ($LASTEXITCODE -ne 0) { throw 'Core archive failed.' }
    & tar -xf $archive -C $snapshot
    if ($LASTEXITCODE -ne 0) { throw 'Core archive extraction failed.' }
    $files = [ordered]@{}
    foreach ($file in Get-ChildItem $snapshot -Recurse -File -Force | Sort-Object FullName) {
        $relative = $file.FullName.Substring($snapshot.Length + 1).Replace('\', '/')
        $files[$relative] = Get-CoreSourceHash $file.FullName
    }
    $pin = [ordered]@{
        repository = 'https://github.com/Postive-ToolKit/ODDB.Core'
        commit = $commit.Trim()
        tree = $tree.Trim()
        files = $files
    }
    $tracked = & git -C $root ls-files -- 'Source~/ODDB.Core'
    if ($LASTEXITCODE -ne 0) { throw 'Cannot list existing snapshot files.' }
    # Remove only previously tracked source files, never unrelated untracked files or build output.
    foreach ($path in $tracked) { Remove-Item (Join-Path $root $path) -Force }
    Copy-Item "$snapshot/*" "$root/Source~/ODDB.Core" -Recurse -Force
    Write-CoreText "$root/Source~/core-source.json" (($pin | ConvertTo-Json -Depth 5) + "`n")
    & "$PSScriptRoot/build.ps1"
} finally { Remove-Item $temp -Recurse -Force }
