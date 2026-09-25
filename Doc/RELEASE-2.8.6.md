# ODDB 2.8.6

## Table Tag and Color dialog

- Right-click a table in the ODDB Editor's left list and choose **Edit Tag and Color...** to open a Unity modal dialog.
- Edit the tag and color together, then Save. Cancel discards edits; Reset clears the tag and restores the default color when saved.
- The chosen color and tag remain visible in the left list, and the tag remains searchable. The settings stay in `Assets/Settings/ODDBEditorSettings.asset`.
- The Tag and Color row was removed from the selected table header.

## Compatibility

This update changes only the Unity Editor assembly and package version. The runtime assembly, standalone ODDB.Core DLL, and database format are unchanged.

## Verification

Unity `6000.3.15f1` batch mode compiled the project successfully on Windows. The Editor UI was not checked interactively.
