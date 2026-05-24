# ODDB v1 → v2 migration

`ODDB-Migrate-v1-to-v2.csx` rewrites a v1.x ODDB `.bytes` database to the v2.0 schema in place. It converts the old integer `Type` field on every `FieldType` into the new string `_typeKey`, and leaves the original file backed up as `<file>.v1.bak`.

## Usage

```sh
# one-time install
dotnet tool install -g dotnet-script

# run on each database file before opening with ODDB v2.0
dotnet script ODDB-Migrate-v1-to-v2.csx -- path/to/oddb.bytes
```

The script is idempotent — running it on an already-migrated file reports `nothing to convert` and leaves the file untouched.

## What it changes

The legacy schema embeds an integer that maps to the deprecated `ODDBDataType` enum:

```json
"Type": { "Type": 1, "Param": "" }
```

v2.0 stores the type as a string key instead:

```json
"Type": { "_typeKey": "int", "Param": "" }
```

## Enum → string key mapping

| v1 enum int | v2 key |
| --- | --- |
| 1 | `int` |
| 2 | `float` |
| 100 | `enum` |
| 200 | `bool` |
| 300 | `string` |
| 1003 | `resource` |
| 1004 | `addressable` |
| 2000 | `view` |
| 3000 | `string` (deprecated ID type) |
| 9999 | `custom` |

Unmapped integers print a warning and are left untouched so you can investigate.

## Safety

The script writes the new file only after successfully creating the backup. If the run is interrupted, the database is either fully migrated or fully untouched, never half-converted.
