# ODDB CLI implementation

`ODDB.Cli` is a portable .NET 10 command-line application. It references the
package's pinned `Plugins/ODDB.Core.dll` and links the same CLI operation and
query handlers that the Unity Editor bridge uses. The package distributes a
framework-dependent build in `dist/`; users need the .NET 10 runtime, not the
SDK. The importable launchers and agent guidance live in `Samples~/CLI`.

To rebuild the packaged executable from the package root:

```sh
dotnet publish Tools~/CLI/ODDB.Cli.csproj -c Release \
  -o Tools~/CLI/dist -p:UseAppHost=false
```

Remove the optional `.pdb` before packaging. Keep `dist/ODDB.Core.dll` identical
to `Plugins/ODDB.Core.dll`. Unity-specific commands use the project-local file
bridge when the Editor is open, or Unity batch mode for a closed project. Core
commands load and save directly when the project is closed.
