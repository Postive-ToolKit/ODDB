using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Settings;
using TeamODD.ODDB.Runtime.Types;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Editors.CodeGen
{
    /// <summary>
    /// Resolves ODDB Field types to concrete C# type expressions used in generated code.
    /// Holds batch-scoped context (which Views are being generated, with which class names)
    /// so that View references can be resolved to the correct generated class name.
    /// </summary>
    internal sealed class TypeMapper
    {
        public readonly struct Resolved
        {
            public readonly string TypeName;     // C# expression e.g. "int", "Monster", "PlayerClass"
            public readonly string Namespace;    // namespace to add to using; null/empty if BCL
            public readonly bool Ok;
            public readonly string FailureReason;

            private Resolved(string typeName, string ns, bool ok, string reason)
            {
                TypeName = typeName; Namespace = ns; Ok = ok; FailureReason = reason;
            }

            public static Resolved Success(string typeName, string ns)
                => new(typeName, ns, true, null);
            public static Resolved Failure(string reason)
                => new(null, null, false, reason);
        }

        // ViewID → generated class name (for views in the current generation batch).
        private readonly Dictionary<string, string> _batchClassNames;

        // TypeID → Type (lazily built scan of [CustomDataType] attributed types).
        private readonly Lazy<Dictionary<string, Type>> _customTypes;

        public TypeMapper(IEnumerable<KeyValuePair<string, string>> batchClassNames)
        {
            _batchClassNames = batchClassNames.ToDictionary(kv => kv.Key, kv => kv.Value);
            _customTypes = new Lazy<Dictionary<string, Type>>(BuildCustomTypeIndex);
        }

        public Resolved Resolve(FieldType fieldType, IODatabaseView referencedViewLookup)
        {
            var key = fieldType?.TypeKey ?? string.Empty;

            // Special-cased keys need extra context the registry can't supply standalone.
            switch (key)
            {
                case "enum":
                    return ResolveEnum(fieldType.Param);
                case "resource":
                    return ResolveType(fieldType.Param, "Resource");
                case "view":
                    return ResolveViewReference(fieldType.Param, referencedViewLookup);
                case "custom":
                    return ResolveCustom(fieldType.Param);
#if ADDRESSABLE_EXIST
                case "addressable":
                    return ResolveAddressable(fieldType.Param);
#endif
            }

            // Generic path — look up in TypeRegistry.
            var descriptor = TypeRegistry.GetDescriptor(key);
            if (descriptor != null && descriptor.TargetType != null && !descriptor.RequiresParam)
            {
                var t = descriptor.TargetType;
                var keyword = CSharpKeywordFor(t);
                if (keyword != null)
                    return Resolved.Success(keyword, null);
                return Resolved.Success(t.Name, t.Namespace);
            }

            return Resolved.Failure($"unsupported data type '{key}'");
        }

        private static string CSharpKeywordFor(Type t)
        {
            if (t == typeof(int))    return "int";
            if (t == typeof(float))  return "float";
            if (t == typeof(bool))   return "bool";
            if (t == typeof(string)) return "string";
            if (t == typeof(long))   return "long";
            if (t == typeof(double)) return "double";
            if (t == typeof(byte))   return "byte";
            if (t == typeof(short))  return "short";
            return null;
        }

        private static Resolved ResolveEnum(string param)
        {
            if (string.IsNullOrEmpty(param))
                return Resolved.Failure("enum field has empty Param (no enum type selected)");

            var type = ODDBEnumUtility.GetEnumType(param);
            if (type == null)
                return Resolved.Failure($"enum '{param}' not found (missing [ODDBEnum]?)");
            return Resolved.Success(type.Name, type.Namespace);
        }

        private static Resolved ResolveType(string typeName, string label)
        {
            if (string.IsNullOrEmpty(typeName))
                return Resolved.Failure($"{label} field has empty Param");

            var type = AppDomain.CurrentDomain.GetAssemblies()
                .Select(a => a.GetType(typeName, throwOnError: false))
                .FirstOrDefault(t => t != null);

            if (type == null)
            {
                type = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(SafeGetTypes)
                    .FirstOrDefault(t => t.FullName == typeName || t.Name == typeName);
            }
            if (type == null)
                return Resolved.Failure($"{label} type '{typeName}' not found in loaded assemblies");
            return Resolved.Success(type.Name, type.Namespace);
        }

#if ADDRESSABLE_EXIST
        private static Resolved ResolveAddressable(string param)
        {
            if (ODDBSettings.Setting.UseAddressableAutoLoad)
                return ResolveType(param, "Addressable");
            return Resolved.Success("string", null);
        }
#endif

        private Resolved ResolveCustom(string typeId)
        {
            if (string.IsNullOrEmpty(typeId))
                return Resolved.Failure("custom field has empty Param (no TypeID selected)");

            if (_customTypes.Value.TryGetValue(typeId, out var type))
                return Resolved.Success(type.Name, type.Namespace);
            return Resolved.Failure($"custom TypeID '{typeId}' not found ([CustomDataType] missing or unloaded)");
        }

        private Resolved ResolveViewReference(string viewId, IODatabaseView lookup)
        {
            if (string.IsNullOrEmpty(viewId))
                return Resolved.Failure("view reference has empty Param (no target view)");

            if (_batchClassNames.TryGetValue(viewId, out var className))
                return Resolved.Success(className, "ODDB.Generated");

            var view = lookup?.Find(viewId);
            if (view == null)
                return Resolved.Failure($"view reference target '{viewId}' not found in database");
            if (view.BindType != null)
                return Resolved.Success(view.BindType.Name, view.BindType.Namespace);

            return Resolved.Failure(
                $"view reference target '{view.Name}' has no BindType — include it in the batch or generate it first");
        }

        private static Dictionary<string, Type> BuildCustomTypeIndex()
        {
            var index = new Dictionary<string, Type>();
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            foreach (var type in SafeGetTypes(assembly))
            {
                var attr = type.GetCustomAttribute<CustomDataTypeAttribute>();
                if (attr == null) continue;
                if (attr.DataType != null && !index.ContainsKey(attr.TypeID))
                    index[attr.TypeID] = attr.DataType;
            }
            return index;
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly assembly)
        {
            try { return assembly.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
        }
    }

    /// <summary>
    /// Minimal lookup contract used by TypeMapper to resolve cross-batch view references.
    /// Implemented by ODDBCodeGenerator via ODDBDataService-loaded ODDatabase.
    /// </summary>
    internal interface IODatabaseView
    {
        IView Find(string viewId);
    }
}
