# ODDB.Core

ODDB.Core is the engine-agnostic C# database layer shared by ODDB integrations.
It owns the database model, DTO conversion, serialization, type registration,
load safety, and core mutations. Engine-specific editor UI and runtime adapters
belong in separate repositories.

## Current Consumers

- Unity: the ODDB UPM package and Unity editor integration.
- Godot: the Godot editor plugin and C# bridge under development.

## Development

```sh
dotnet build ODDB.Core.csproj
```

The project currently targets `netstandard2.1`. Keep the core free of
UnityEngine, UnityEditor, Godot, and other engine APIs.

## Release Boundary

ODDB.Core versions are independent of engine adapter versions. Consumers must
pin an explicit Core version or release artifact; development branches must not
be used as a package dependency.

## Extraction Note

This repository was extracted from the Unity package while preserving the
Core-only commit history. The Unity package remains the source of truth for
Unity integration until the adapter is switched to a versioned Core artifact.
