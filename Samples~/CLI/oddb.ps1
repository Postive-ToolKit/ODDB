param([Parameter(ValueFromRemainingArguments = $true)][string[]]$CliArguments)
if (-not $CliArguments) { $CliArguments = @() }
$projectIndex = [Array]::IndexOf($CliArguments, '--project')
if ($projectIndex -ge 0) {
    if ($projectIndex + 1 -ge $CliArguments.Count) {
        throw '--project needs a Unity project root.'
    }
    $projectRoot = (Resolve-Path $CliArguments[$projectIndex + 1]).Path
} else {
    $cursor = (Resolve-Path (Split-Path -Parent $MyInvocation.MyCommand.Path)).Path
    while ($cursor) {
        if ((Test-Path (Join-Path $cursor 'Assets') -PathType Container) -and
            (Test-Path (Join-Path $cursor 'Packages') -PathType Container)) {
            $projectRoot = $cursor
            break
        }
        $parent = Split-Path -Parent $cursor
        if (-not $parent -or $parent -eq $cursor) { break }
        $cursor = $parent
    }
    if (-not $projectRoot) {
        throw 'Cannot locate the Unity project; pass --project with its root.'
    }
}
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
