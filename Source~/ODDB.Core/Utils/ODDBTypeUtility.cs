using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeamODD.ODDB.Runtime.Entities;

namespace TeamODD.ODDB.Runtime.Utils.Converters
{
    public static class ODDBTypeUtility
    {
        private static readonly Dictionary<string, Type> _typeCache = new();
        private static readonly HashSet<string> _warnedUnresolvedBindTypes = new();
        private static bool _isFullIndexed = false;

        internal static void ResetCache()
        {
            _typeCache.Clear();
            _warnedUnresolvedBindTypes.Clear();
            _isFullIndexed = false;
        }

        public static bool TryConvertBindType(string bindType, out Type type)
        {
            type = null;
            if (string.IsNullOrEmpty(bindType))
                return true;

            if (_typeCache.TryGetValue(bindType, out type))
                return type != null;

            // Quick check for common types or fully qualified names
            type = Type.GetType(bindType);
            if (type != null)
            {
                if (!type.IsSubclassOf(typeof(ODDBEntity)))
                {
                    WarnUnresolvedBindType(bindType,
                        $"[ODDBImporter] '{bindType}' is not a subclass of ODDBEntity; binding is preserved as unresolved.");
                    type = null;
                    return false;
                }
                _typeCache[bindType] = type;
                return true;
            }

            // If still not found and we haven't done a full scan yet, do it now.
            if (!_isFullIndexed)
            {
                if (ODDB.DebugLog)
                    ODDB.Logger.Info("[ODDBImporter] Performing one-time full assembly scan to index ODDBEntities...");

                PerformFullIndex();
                _isFullIndexed = true;

                if (_typeCache.TryGetValue(bindType, out type))
                    return type != null;
            }

            WarnUnresolvedBindType(bindType,
                $"[ODDBImporter] Cannot find or convert bind type: '{bindType}'; binding is preserved as unresolved.");
            return false;
        }

        private static void WarnUnresolvedBindType(string bindType, string message)
        {
            if (_warnedUnresolvedBindTypes.Add(bindType ?? string.Empty))
                ODDB.Logger.Warn(message);
        }

        private static void PerformFullIndex()
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                foreach (var t in types)
                {
                    if (!t.IsAbstract && t.IsSubclassOf(typeof(ODDBEntity)))
                    {
                        if (!_typeCache.ContainsKey(t.FullName))
                            _typeCache[t.FullName] = t;

                        if (!_typeCache.ContainsKey(t.Name))
                            _typeCache[t.Name] = t;
                    }
                }
            }
        }
    }
}
