using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime.Serializers;

namespace TeamODD.ODDB.Runtime.Types
{
    public static class TypeRegistry
    {
        private static Dictionary<string, RegisteredType> _byKey;
        private static List<RegisteredType> _all;

        public static IReadOnlyList<RegisteredType> All
        {
            get { EnsureLoaded(); return _all; }
        }

        public static IDataSerializer Get(string key)
        {
            EnsureLoaded();
            return _byKey.TryGetValue(key ?? "", out var rt) ? rt.Serializer : null;
        }

        public static RegisteredType GetDescriptor(string key)
        {
            EnsureLoaded();
            return _byKey.TryGetValue(key ?? "", out var rt) ? rt : null;
        }

        public static IEnumerable<RegisteredType> ByFolder(string folder)
        {
            EnsureLoaded();
            return _all.Where(t => t.Folder == folder);
        }

        public static void ResetCache()
        {
            _byKey = null;
            _all = null;
        }

        private static void EnsureLoaded()
        {
            if (_byKey != null) return;
            _byKey = new Dictionary<string, RegisteredType>();
            _all = new List<RegisteredType>();
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); } catch { continue; }
                foreach (var t in types)
                {
                    if (t == null || t.IsAbstract || t.IsInterface) continue;
                    if (!typeof(IDataSerializer).IsAssignableFrom(t)) continue;
                    var attr = (ODDBTypeAttribute)Attribute.GetCustomAttribute(t, typeof(ODDBTypeAttribute));
                    if (attr == null) continue;
                    IDataSerializer inst;
                    try { inst = (IDataSerializer)Activator.CreateInstance(t); }
                    catch { continue; }
                    var reg = new RegisteredType(attr.Key, attr.TargetType, attr.Folder, attr.RequiresParam, inst);
                    if (_byKey.ContainsKey(attr.Key))
                        TeamODD.ODDB.Runtime.ODDB.Logger.Warn($"duplicate ODDBType key '{attr.Key}', last registration wins ({t.FullName})");
                    _byKey[attr.Key] = reg;
                    _all.Add(reg);
                }
            }
        }
    }
}
