param([Parameter(ValueFromRemainingArguments = $true)][string[]]$CliArguments)
$projectIndex = [Array]::IndexOf($CliArguments, '--project')
if ($projectIndex -lt 0 -or $projectIndex + 1 -ge $CliArguments.Count) {
    throw 'Pass --project with the Unity project root.'
}
$projectRoot = (Resolve-Path $CliArguments[$projectIndex + 1]).Path
$candidates = @(
    (Join-Path $projectRoot 'Assets/Plugins/ODDB'),
    (Join-Path $projectRoot 'Packages/com.team-odd.oddb')
)
$cache = Join-Path $projectRoot 'Library/PackageCache'
if (Test-Path $cache) {
    $candidates += @(Get-ChildItem $cache -Directory -Filter 'com.team-odd.oddb@*' | ForEach-Object FullName)
}
$packageRoot = $candidates | Where-Object { Test-Path (Join-Path $_ 'Tools~/CLI/dist/ODDB.Cli.dll') } | Select-Object -First 1
if (-not $packageRoot) { throw 'ODDB CLI package source was not found in this Unity project.' }
$cliDll = Join-Path $packageRoot 'Tools~/CLI/dist/ODDB.Cli.dll'
& dotnet $cliDll @CliArguments
exit $LASTEXITCODE
