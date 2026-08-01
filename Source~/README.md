# ODDB.Core Source

`ODDB.Core` is the engine-agnostic .NET Standard 2.1 library used by the Unity package.
It lives under `Source~` deliberately: Unity ignores folders whose names end with `~`, while Git still versions the source.

Build the release DLL from the package root:

```sh
dotnet build Source~/ODDB.Core/ODDB.Core.csproj --configuration Release
cp Source~/ODDB.Core/bin/Release/netstandard2.1/ODDB.Core.dll Plugins/ODDB.Core.dll
shasum -a 256 Plugins/ODDB.Core.dll
```

Update `Plugins/ODDB.Core.dll.sha256` with the printed checksum whenever the DLL changes. Do not commit `bin/` or `obj/`.
