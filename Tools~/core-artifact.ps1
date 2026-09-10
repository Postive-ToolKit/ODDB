function Write-CoreText([string]$Path, [string]$Text) {
    [IO.File]::WriteAllText($Path, $Text, [Text.UTF8Encoding]::new($false))
}

function Get-CoreSourceHash([string]$Path) {
    # The source snapshot is UTF-8 text; checkout line endings must not change its identity.
    $text = [IO.File]::ReadAllText($Path).Replace("`r`n", "`n")
    $sha = [Security.Cryptography.SHA256]::Create()
    try {
        return ([BitConverter]::ToString($sha.ComputeHash([Text.Encoding]::UTF8.GetBytes($text)))).Replace('-', '').ToLowerInvariant()
    } finally { $sha.Dispose() }
}

function Assert-CoreSource([string]$Root, $Pin) {
    if ($Pin.commit -notmatch '^[0-9a-f]{40}$' -or $Pin.tree -notmatch '^[0-9a-f]{40}$' -or !$Pin.files) {
        throw 'Invalid Core source pin.'
    }
    $expected = @{}
    foreach ($entry in $Pin.files.PSObject.Properties) {
        $relative = $entry.Name
        if ($relative -match '(^/|\\|(^|/)\.\.(/|$)|:)') { throw "Unsafe source path: $relative" }
        $expected[$relative] = $true
        $path = Join-Path $Root $relative
        if (!(Test-Path $path -PathType Leaf) -or (Get-CoreSourceHash $path) -ne $entry.Value) {
            throw "Core source differs from pinned commit $($Pin.commit): $relative. Run sync-core.ps1 from a committed Core checkout."
        }
    }
    foreach ($file in Get-ChildItem $Root -Recurse -File -Force) {
        $relative = $file.FullName.Substring($Root.TrimEnd('/', '\').Length + 1).Replace('\', '/')
        if ($relative -match '(^|/)(bin|obj|\.git)(/|$)') { continue }
        if (!$expected.ContainsKey($relative)) { throw "Unpinned Core source file: $relative" }
    }
}
