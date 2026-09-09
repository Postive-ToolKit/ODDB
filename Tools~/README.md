# ODDB tools

## `build.sh` / `build.ps1` — rebuild ODDB.Core dll

Run after any change under `Source~/ODDB.Core/` to refresh `Plugins/ODDB.Core.dll`:

```sh
./Tools~/build.sh           # macOS / Linux, Release (default)
./Tools~/build.sh Debug     # Debug build

pwsh Tools~/build.ps1                    # Windows (PowerShell)
pwsh Tools~/build.ps1 -Configuration Debug
```

Requires `dotnet` on PATH. Unity picks up the new dll on next focus.

During the Core extraction, the build scripts can build an external checkout
instead of the embedded `Source~/ODDB.Core` directory:

```sh
ODDB_CORE_PROJECT_PATH=/path/to/ODDB.Core ./Tools~/build.sh

pwsh Tools~/build.ps1 -CoreProjectPath C:\path\to\ODDB.Core
```

The generated DLL is still copied to this package's `Plugins/` directory. The
default embedded path remains available until the Unity package switches to a
versioned Core artifact.

The v1 → v2 migration script now lives at `Samples~/Migration/` so it can be imported via Unity Package Manager. See that folder's README.
