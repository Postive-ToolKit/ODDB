using System;
using System.Collections.Generic;

namespace TeamODD.ODDB.Editors.Utils
{
    public static class ODDBEditorDI
    {
        private static readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        public static T Resolve<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service)) {
                return (T)service;
            }
            return null;
        }

        public static void Register<T>(T service) where T : class
        {
            var type = typeof(T);
            _services[type] = service;
        }

        public static void RegisterSelfAndInterfaces<T>(T service) where T : class
        {
            var type = typeof(T);
            _services[type] = service;
            foreach (var interfaceType in type.GetInterfaces()) {
                _services[interfaceType] = service;
            }
        }

        public static void DisposeAll()
        {
            _services.Clear();
        }
    }
}