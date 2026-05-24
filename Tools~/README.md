# ODDB tools

## `build.sh` / `build.ps1` — rebuild ODDB.Core dll

Run after any change under `src/ODDB.Core/` to refresh `Plugins/ODDB.Core.dll`:

```sh
./Tools~/build.sh           # macOS / Linux, Release (default)
./Tools~/build.sh Debug     # Debug build

pwsh Tools~/build.ps1                    # Windows (PowerShell)
pwsh Tools~/build.ps1 -Configuration Debug
```

Requires `dotnet` on PATH. Unity picks up the new dll on next focus.

The v1 → v2 migration script now lives at `Samples~/Migration/` so it can be imported via Unity Package Manager. See that folder's README.
