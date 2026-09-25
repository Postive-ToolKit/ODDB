# ODDB CLI agent instructions

Use the ODDB CLI for database reads and edits in this Unity project. Invoke the
sample launcher for your platform (`oddb.bat`, `oddb.ps1`, or `oddb.sh`). Always
pass the exact Unity project root with `--project` and use `--json` when reading
results. The path identifies the target project; no MCP server or TCP port is
needed. Run `help` for all commands or `help <group> <action>` for one command;
these help forms do not need `--project` when the Sample is inside the project.

Before a mutation, read the relevant view/table and row with `views list`,
`views schema --view-id ID`, and `rows show --table-id ID --row-id ID`. Prefer
IDs over display names. Check the CLI exit code; on failure, read the JSON
error from stderr. Do not edit `ODDB.bytes` directly.

## Queries

- `database info`; `views list`; `views pure`; `views show --view-id ID`
- `views schema --view-id ID`; `rows list --table-id ID`
- `rows show --table-id ID --row-id ID`; `tables inherited --view-id ID`
- `types data`; `types bind`; `history list`

## Mutations

- `views add --name NAME`; `views remove --view-id ID`
- `tables add --name NAME [--parent-view-id ID] [--bind-type TYPE]`
- `tables remove --table-id ID`
- `views set-name --view-id ID --name NAME`; `views set-id --view-id ID --new-view-id ID`
- `views set-parent --view-id ID --parent-view-id ID`; use `null` to clear
- `views set-bind-type --view-id ID --type-name TYPE`; use `null` to clear
- `fields add --view-id ID --field-name NAME --field-type TYPE [--param PARAM]`
- `fields remove --view-id ID --index N`; `fields move --view-id ID --old-index N --new-index N`
- `fields set-type --view-id ID --field-index N --field-type TYPE [--param PARAM]`
- `rows add --table-id ID`; `rows remove --table-id ID --row-id ID`
- `rows set-id --table-id ID --row-id ID --new-row-id ID`
- `cells set --table-id ID --row-id ID --field-index N --value JSON`
- `code generate [--view-ids JSON_ARRAY]`; `database save`

Each mutation is saved automatically when run outside Unity. When an Editor is
open, the CLI sends a project-local request and uses the Editor's shared ODDB
session so the UI and Undo history stay current. Run `database save` after live
mutations when the changes should persist on disk. If that session is unavailable,
the CLI fails rather than overwriting potentially unsaved data. Use
`call oddb_OPERATION --args JSON_OBJECT` or `read oddb://RESOURCE` for the
legacy operation contract during migration.

With Unity closed, `history list` is a CLI audit log. It cannot undo earlier
processes. With Unity open, it reads the Editor's current undo/redo stacks.
