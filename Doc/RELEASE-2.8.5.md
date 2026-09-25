# ODDB 2.8.5

## Table labels in the Unity Editor

- Assign one tag and a color to each table from the ODDB Editor header.
- Show the chosen color and tag in the left table list. Search by name, ID, or tag,
  and filter the list to tables only.
- Store the labels in the project editor settings asset at
  `Assets/Settings/ODDBEditorSettings.asset`, separate from `ODDB.bytes`.
- Keep a table's label when its ID changes, including ODDB command undo and redo.

## Compatibility

This update changes only the Unity Editor assembly and the package version. The
runtime assembly, standalone ODDB.Core DLL, and database format are unchanged.
Existing editor settings assets start with no table labels; no migration is needed.

## Verification

Unity `6000.3.15f1` batch mode compiled the project successfully on Windows.
The Editor UI was not checked interactively.
