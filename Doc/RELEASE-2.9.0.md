# ODDB 2.9.0

## Project-scoped CLI

- Migrate all 18 ODDB MCP operations and 8 read resources to CLI commands. The `call oddb_* --args JSON` and `read oddb://...` forms preserve their names for migration.
- Add an importable **ODDB CLI** Unity Package Manager Sample with `oddb.bat`, `oddb.ps1`, `oddb.sh`, a README, and `AGENT.md` guidance.
- While Unity is open, CLI requests run in the Editor's shared ODDB session, keeping UI updates and Undo/Redo available. When the project is closed, Core operations work directly on the database, and Unity-dependent operations launch Unity in batch mode.
- Remove the ODDB MCP HTTP server, client registration, and port settings. CLI requests use files under the selected project's `Library/ODDB/cli`, so multiple Unity projects do not compete for a network port.

## Migration

Import the **ODDB CLI** sample and run its platform launcher with `--project <Unity project root>`. Install the .NET 10 runtime. Remove old ODDB MCP server entries from AI client configurations; the CLI does not start a server. The CLI saves standalone mutations automatically. With the Unity Editor open, run `database save` when edits should be written to disk.

## Compatibility

The Unity runtime assembly, pinned `ODDB.Core.dll`, and database format are unchanged. Editor integrations that referenced the removed MCP server classes must migrate to CLI or `ODDBEditorSession`.

## Verification

The standalone .NET CLI built successfully and Unity `6000.3.15f1` batch mode compiled the project on Windows. Functional CLI command tests were not run.
