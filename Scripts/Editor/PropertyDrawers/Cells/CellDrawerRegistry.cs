using System;
using System.Collections.Generic;
using TeamODD.ODDB.Editors.Attributes;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Reflection-based registry for IODDBCellDrawer implementations
    /// indexed by string type key.
    /// </summary>
    public static class CellDrawerRegistry
    {
        private static Dictionary<string, IODDBCellDrawer> _byKey;

        public static IODDBCellDrawer Get(string typeKey)
        {
            EnsureLoaded();
            if (string.IsNullOrEmpty(typeKey)) return null;
            return _byKey.TryGetValue(typeKey, out var d) ? d : null;
        }

        public static void ResetCache() { _byKey = null; }

        private static void EnsureLoaded()
        {
            if (_byKey != null) return;
            _byKey = new Dictionary<string, IODDBCellDrawer>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); } catch { continue; }
                foreach (var t in types)
                {
                    if (t == null || t.IsAbstract || t.IsInterface) continue;
                    if (!typeof(IODDBCellDrawer).IsAssignableFrom(t)) continue;
                    var attr = (CellDrawerAttribute)Attribute.GetCustomAttribute(t, typeof(CellDrawerAttribute));
                    if (attr == null) continue;
                    _byKey[attr.TypeKey] = (IODDBCellDrawer)Activator.CreateInstance(t);
                }
            }
        }
    }
}
