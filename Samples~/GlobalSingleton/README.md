# ODDB Global Singleton (Legacy v1.x Style)

This sample restores the `ODDBPort` static facade behavior from ODDB v1.x.
Import via Package Manager → ODDB → Samples → Global Singleton → Import.

Usage after import:
```csharp
var item = ODDBPort.GetEntity<WeaponData>("weapon_001");
```

After import, `ODDBPort.cs` is copied into your project's Assets folder.
You can modify it freely (e.g., change auto-init behavior, add callbacks).

In v2.0+, the recommended pattern is to keep your own `ODDatabase` reference:

```csharp
public class GameBootstrap : MonoBehaviour
{
    public static ODDatabase Db;

    void Awake()
    {
        Db = ODDatabase.Load(path);
        Db.PortData();
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
