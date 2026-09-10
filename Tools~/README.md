# ODDB Core Tools

The authoritative source is [ODDB.Core](https://github.com/Postive-ToolKit/ODDB.Core).
The Unity package vendors a pinned source snapshot in `Source~/ODDB.Core` and a
prebuilt `Plugins/ODDB.Core.dll`. Consumers do not need a Core checkout, .NET SDK,
or PowerShell to use the Unity package.

## Update Core

Commit changes in the standalone Core repository first, then run from the Unity
package root (Git, tar, .NET SDK and PowerShell are required):

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools~/sync-core.ps1 -CoreRepositoryPath C:/path/to/ODDB.Core
```

On macOS/Linux use PowerShell 7 (`pwsh`) instead of `powershell`. The sync command
archives the clean Core checkout's HEAD, replaces the tracked Unity source snapshot,
records the source commit/tree and file checksums, then builds the DLL. It refuses
uncommitted Core changes or changes to the Unity snapshot. Commit the snapshot,
`Source~/core-source.json`, DLL, checksum and `Plugins/core-artifact.json` together
after running Unity tests. A failed build is not a publishable update.

## Rebuild And Verify

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File Tools~/build.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Tools~/verify-core.ps1
powershell -NoProfile -ExecutionPolicy Bypass -File Tools~/test-core-tools.ps1
```

`build.ps1` accepts `-Configuration Release|Debug` and `-CoreProjectPath` pointing
to an external checkout. External and embedded builds must match the same pinned
source inventory. Source hashes normalize LF/CRLF. The DLL SHA-256 is a byte hash
and is recorded after each build; different SDKs or source paths can change it.
For release builds use `Release` and retain the recorded SDK version.

```sh
./Tools~/build.sh
ODDB_CORE_PROJECT_PATH=/path/to/ODDB.Core ./Tools~/build.sh
```

The shell entry point now requires PowerShell 7 and delegates to the same verified
build implementation. `verify-core.ps1` rejects modified, missing or extra source
files and mismatched DLL/checksum/provenance. `test-core-tools.ps1` tests these
failure paths on temporary copies without changing the package.

The v1-to-v2 migration script remains in `Samples~/Migration`.
