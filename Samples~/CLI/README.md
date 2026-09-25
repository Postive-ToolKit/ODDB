# ODDB CLI sample

These scripts locate the ODDB package in the selected Unity project, then run
its packaged standalone CLI. Install the .NET 10 runtime and pass the **Unity project
root** with `--project` for data commands. From an imported Sample, `help`,
`--help`, and `--version` work without `--project`. The scripts contain no ODDB
data logic.

Windows PowerShell:

```powershell
./oddb.ps1 help
./oddb.ps1 help rows add
./oddb.ps1 --project C:/path/to/UnityProject views list --json
./oddb.ps1 --project C:/path/to/UnityProject tables add --name Items --json
```

Windows Command Prompt: `oddb.bat --project C:\path\to\UnityProject views list --json`

macOS/Linux: `./oddb.sh help` or `./oddb.sh --project /path/to/UnityProject views list --json`

Use `read oddb://views` or `call oddb_add_table --args '{"name":"Items"}'`
for direct migration from the old MCP tool and resource names. Mutations save
automatically. Unity-specific operations use the project's open Editor or start
Unity in batch mode if the project is closed. Pass `--unity <path>` or set
`ODDB_UNITY_PATH` when Unity Hub is installed in a custom location.

See [AGENT.md](AGENT.md) for AI agent usage rules and the full command map.

If an AI client was registered to the old ODDB MCP server, remove its old
`oddb` server entry from that client's configuration. This CLI does not open a
network port or register itself as an MCP server.

`history list` shows the live Editor undo/redo stacks while Unity is open. With
Unity closed it shows the project's CLI audit entries from `Library/ODDB/cli`;
those entries are not an offline undo stack.
