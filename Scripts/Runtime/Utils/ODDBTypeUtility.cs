using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Settings;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Utils.Converters
{
    internal static class ODDBTypeUtility
    {
        private static readonly Dictionary<string, Type> _typeCache = new();
        public static bool TryConvertBindType(string bindType, out Type type)
        {
            type = null;
            if (string.IsNullOrEmpty(bindType))
                return true;
            
            if (_typeCache.TryGetValue(bindType, out type))
                return true;

            // Quick check for common types
            type = Type.GetType(bindType);
            if (type != null)
            {
                if (!type.IsSubclassOf(typeof(ODDBEntity)))
                {
                    Debug.LogError($"[ODDBImporter] '{bindType}' is not a subclass of ODDBEntity.");
                    type = null;
                    return false;
                }
                _typeCache[bindType] = type;
                return true;
            }

            if (ODDBSettings.Setting.UseDebugLog) 
                Debug.Log("[ODDBImporter] Cannot find bind type: " + bindType + " in current assembly, searching all assemblies...");
            
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
                    if (t.FullName == bindType && !t.IsAbstract && t.IsSubclassOf(typeof(ODDBEntity)))
                    {
                        type = t;
                        _typeCache[bindType] = type;
                        return true;
                    }
            }

            Debug.LogError($"[ODDBImporter] Cannot find or convert bind type: '{bindType}'");
            return false;
        }
    }
}