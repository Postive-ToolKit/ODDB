using System;
using System.Linq;
using TeamODD.ODDB.Runtime.Entities;

namespace TeamODD.ODDB.Editors.MCP.Resources
{
    public class BindTypesResource : IMcpResource
    {
        public string UriOrTemplate => "oddb://bind-types";
        public string Description => "Available ODDBEntity subtypes for view binding.";
        public string MimeType => "application/json";

        public bool TryMatch(string uri) => uri == UriOrTemplate;

        public object Read(string uri)
        {
            var entityType = typeof(ODDBEntity);
            return AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(SafeGetTypes)
                .Where(t => t != null && !t.IsAbstract && entityType.IsAssignableFrom(t))
                .Select(t => new { fullName = t.FullName, name = t.Name, assembly = t.Assembly.GetName().Name })
                .OrderBy(x => x.fullName)
                .ToArray();
        }

        private static Type[] SafeGetTypes(System.Reflection.Assembly a)
        {
            try { return a.GetTypes(); }
            catch { return new Type[0]; }
        }
    }
}
