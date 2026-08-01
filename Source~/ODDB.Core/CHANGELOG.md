# ODDB.Core Changelog

This source is versioned with the Unity package under `Source~/ODDB.Core`. The trailing `~` keeps Unity from importing the .NET project as Unity scripts.

## v2.6.0 — 2026-08-02

### feat(core): add type-declared async loading

- Added `ODDBLoadType.Default` and `ODDBLoadType.Async` to `ODDBTypeAttribute` while preserving the existing constructor.
- Added the `IAsyncLoader` integration point and `ODDB.RegisterAsyncLoader`, `GetAsync`, and `Release` APIs.
- Async entity fields retain their serialized key, so existing database files require no migration.
- Unity CodeGen emits typed `Get{Field}Async` and `Release{Field}` wrappers for async types.
- Added the default `AddressablesAsyncLoader` adapter and removed synchronous Addressables materialization.

## v2.2.6 — 2026-07-12

### fix(core): exclude entity infrastructure fields during porting

- `ODDBEntity.GetFieldFields` now excludes fields declared by `ODDBEntity` itself.
- Bound entity fields therefore map only to fields declared by the concrete entity type and its application-level base types.

## v2.0.11 — 2026-05-31

Build host: macOS, `dotnet 10.0.107`, `netstandard2.1` target.

### feat(core): add ODDBLoadReport and ODDBLoadFailureStage constants

- New `ODDBLoadReport` value object in `Runtime/Utils/Converters` namespace.
- Carries `IsSafeToSave`, `FailureStage`, `FailureReason`, file size + DTO/restored counts, `UnmappedFieldTypeCount`, `SourceFormatVersion` ("v1" | "v2" | "ambiguous").
- `Success(...)` / `Failure(...)` factories — instances are immutable.
- New `ODDBLoadFailureStage` static constants class — `None` | `FileMissing` | `Read` | `Gzip` | `Json` | `EmptyDtoOnExistingFile` | `EmptyRestoredOnNonEmptyDto` | `UnmappedFieldType`.
- New `ODDBLoadException : Exception` carrying `FailureStage` / `FailureReason` for the `Load(throw)` path.

### feat(core): split ODDatabase.Load into Load+TryLoad+CreateEmpty

- **`ODDatabase.Load(path)`** — was: swallow every failure into `new ODDatabase()`. Now: throws `ArgumentException` (null/empty path), `FileNotFoundException` (no file), or `ODDBLoadException` (gzip/json/empty DTO/empty restoration). Callers wanting a fallback use `TryLoad` or `CreateEmpty`.
- **`ODDatabase.TryLoad(path, out database, out report)`** — new. Returns false with a populated `ODDBLoadReport` on any failure. On `EmptyRestoredOnNonEmptyDto` / `UnmappedFieldType` the `database` is populated for read-only inspection but `IsSafeToSave` is false; the caller MUST NOT save it.
- **`ODDatabase.CreateEmpty()`** — new. Explicit new-DB intent. No load happens.
- Pre-mortem #2 resolution: `EmptyDtoOnExistingFile` is **always fatal** even on `SourceFormatVersion=="ambiguous"`. Legitimate-empty recovery is via the migration script's `--allow-empty` override (exit 10), not via in-editor save.
- Private helpers `ProbeSourceFormat(DatabaseDTO)` and `CountUnmappedFieldTypes(ODDatabase)`.

### feat(core): surface deserialize failure from ImportDTO via TryImportDTO

- New `bool ODDBConverter.TryImportDTO(byte[], out DatabaseDTO, out string failureStage, out string failureReason)`.
- New overload `DatabaseDTO TryImportDTO(byte[], out string failureStage, out string failureReason)` (returns null on failure).
- Legacy `ImportDTO(byte[])` preserved for backwards compat — still returns `new DatabaseDTO()` on failure.
- `Import(byte[])` rewired: invokes `OnDatabaseCreated` subscribers **only when deserialization succeeded**; on failure the static list is still cleared (matches fire-and-forget contract) but subscribers are NOT invoked.

### Build artifact

`Plugins/ODDB.Core.dll` — `e7ac62fe68c3d7a168398a26740902203152046b5597511ceee35dc1d3bccffc` (sha256), 49152 bytes.
