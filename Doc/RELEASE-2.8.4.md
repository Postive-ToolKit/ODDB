# ODDB 2.8.4

## Shared Core Update

- Update the Unity Core DLL and source snapshot to standalone ODDB.Core commit
  `1e9702b6c896895cd09a6fffb53edf76884e4ef1`, also used by ODDB.Godot.
- Include safe byte-payload loading and incremental entity materialization, with
  cancellation and retry support. Existing synchronous `PortData()` remains available.
- Preserve unresolved binding names and unknown v2 field types and serialized values
  across save/reload. This preserves unavailable extension data; it does not supply
  a serializer for a missing custom type.
- Pin the source commit/tree and source inventory; record DLL SHA-256 and build SDK.
- Add a clean-checkout sync tool and reject source/provenance mismatches during builds.
- Retain the matching original source under Unity-ignored `Source~`.

## Compatibility

No database migration is required by this package update. No Godot editor code is
included. The Unity UI is unchanged. Unity package version `2.8.4` is independent
of the Core assembly's existing version `2.7.2`; the Core commit is the revision pin.

Unity package consumers do not need the standalone Core repository or build tools.
For contributors, the shell build entry point now requires PowerShell 7 so Windows,
macOS and Linux use the same provenance checks. Windows PowerShell 5.1 is supported
by the `.ps1` entry points.

## Regression Coverage

`ODDBSharedCoreTests` covers byte/file loading, malformed and empty-payload rejection,
unresolved binding and opaque field round trips, incremental loading/cancellation,
and the existing synchronous loading API. `Tools~/test-core-tools.ps1` covers source
and binary provenance failures using disposable copies.

Verified on Windows with Unity `6000.3.15f1` and .NET SDK `8.0.425`:

- Unity EditMode: 132 passed, 0 failed, 0 skipped, 0 inconclusive in a separate
  test project seeded with the synthetic export-consumer database.
- Standalone Core checks: `ODDB_CORE failures=0`.
- Source/DLL tooling checks: 8 checks passed, including stale/missing/extra source,
  altered DLL/checksum/provenance, and LF/CRLF normalization.
- Settings tests now preserve and restore pre-existing settings assets and GUIDs
  instead of remaining inconclusive when editor startup creates settings first.

This is automated Windows compatibility coverage, not a new native UI or
cross-platform manual validation claim. No production database was changed.
