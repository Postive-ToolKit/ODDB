# ODDB 2.9.1

## CLI help

- `help` and `--help` now list every CLI command. `help <group>` and `help <group> <action>` show focused usage and required options.
- The imported Sample launchers can show help and version without `--project`; data commands still require an explicit Unity project path.
- `--version` and `-v` work as standalone flags. The Unix launcher retains LF line endings.

## Standalone database access

- Resolve both an empty `_dbPath` and Unity's `/Resources` path correctly from runtime settings. The standalone CLI finds `Assets/Resources/ODDB.bytes` without an explicit `--db` override.

## Unity Editor compatibility

- Restore `ODDBEditorRuntime.Tools` as an in-process CLI operation registry. Existing Editor scripts that call `TryGet(...).Execute(...)` continue to work without bringing back the MCP server or a network port.

## Compatibility

The runtime assembly, pinned `ODDB.Core.dll`, and database format are unchanged. This patch updates the Editor assembly, CLI executable, Sample launchers, and package version.

## Verification

The standalone CLI built successfully and its help commands ran on Windows. Unity `6000.3.15f1` batch mode compiled the ODDB project. CLI reads completed through Unity batch mode and through offline copies of a nonempty database with both supported settings paths. An operation reached the Unity registry and returned the expected `NOT_FOUND` error for an absent ID. The source database bytes remained unchanged. The already-open graphical Editor bridge was not exercised in this remote session.
