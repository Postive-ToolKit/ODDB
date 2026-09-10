# ODDB.Core Source Snapshot

Core development belongs in [Postive-ToolKit/ODDB.Core](https://github.com/Postive-ToolKit/ODDB.Core).
This directory retains a source snapshot for the DLL shipped with the Unity
package. Unity ignores directories ending in `~`, so it does not compile these
files alongside `Plugins/ODDB.Core.dll`.

`core-source.json` pins the upstream commit, Git tree and normalized source hashes.
`Plugins/core-artifact.json` identifies the binary built from that source, including
its SDK and SHA-256. Unity package version and Core assembly version are independent;
use the source commit to identify this Core revision.

Do not edit `ODDB.Core/` independently or copy a DLL without updating its provenance.
Use `Tools~/sync-core.ps1` after committing changes upstream, or `Tools~/build.ps1`
to rebuild the pinned snapshot. See `Tools~/README.md` for commands and requirements.

The standalone checks are also retained and can be run with:

```sh
dotnet run --project Source~/ODDB.Core/tests/ODDB.Core.Checks.csproj --configuration Release
```

Do not commit `bin/` or `obj/`. Core publication does not automatically update the
Unity package; its snapshot and binary update need a separate Unity release.
