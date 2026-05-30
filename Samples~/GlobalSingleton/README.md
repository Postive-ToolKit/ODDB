# ODDB Global Singleton (Legacy v1.x Style)

This sample restores the `ODDBPort` static facade behavior from ODDB v1.x.
Import via Package Manager → ODDB → Samples → Global Singleton → Import.

Usage after import:
```csharp
var item = ODDBPort.GetEntity<WeaponData>("weapon_001");
```

After import, `ODDBPort.cs` is copied into your project's Assets folder.
You can modify it freely (e.g., change auto-init behavior, add callbacks).

In v2.0+, the recommended pattern is to keep your own `ODDatabase` reference.
**Note:** `ODDatabase.Load(path)` now throws on any failure (missing file, gzip /
JSON parse failure, broken DTO). Use `TryLoad` for safe-load with a diagnostic
report, or `CreateEmpty()` when bootstrapping a brand-new project.

```csharp
public class GameBootstrap : MonoBehaviour
{
    public static ODDatabase Db;

    void Awake()
    {
        if (ODDatabase.TryLoad(path, out Db, out var report))
        {
            Db.PortData();
        }
        else
        {
            // Either bootstrap a fresh DB (first run) or surface the failure
            // and refuse to start the game with a half-loaded one.
            Debug.LogError($"ODDB load failed: stage={report.FailureStage} reason={report.FailureReason}");
            Db = ODDatabase.CreateEmpty();
        }
    }
}
```

The instance-based API exposes the same query surface as the legacy
`ODDBPort` facade:

```csharp
Db.GetEntity<WeaponData>("weapon_001");
Db.GetEntities<WeaponData>();
Db.RegisterOnDataPorted(() => { /* ... */ });
```
