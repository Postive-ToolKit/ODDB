using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Types;

namespace TeamODD.ODDB.Runtime.Entities
{
    public abstract class ODDBEntity
    {
        public static readonly BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private static Dictionary<Type, List<FieldInfo>> _fieldFieldCache = new Dictionary<Type, List<FieldInfo>>();

        internal static void ResetFieldCache()
        {
            _fieldFieldCache.Clear();
        }

        private static List<FieldInfo> GetFieldFields(Type type)
        {
            if (_fieldFieldCache.TryGetValue(type, out var cachedFields))
                return cachedFields;
            var results = new List<FieldInfo>();
            var currentType = type;
            while (currentType != null && currentType != typeof(object))
            {
                var fields = currentType
                    .GetFields(FieldFlags)
                    .Where(f =>
                        f.IsDefined(typeof(CompilerGeneratedAttribute), false) == false &&
                        f.DeclaringType != typeof(ODDBEntity));
                results.InsertRange(0, fields);
                currentType = currentType.BaseType;
            }

            _fieldFieldCache[type] = results;
            return results;
        }

        public string ID { get; private set; }

        private ODDatabase _owner;

        /// <summary>
        /// Imports this entity from a row, recording the owning database so that lazy
        /// view-typed field resolution can look up referenced entities on the same instance.
        /// </summary>
        public void Import(ODDatabase owner, List<Field> tableMetas, Row row)
        {
            _owner = owner;
            Import(tableMetas, row);
        }

        public void Import(List<Field> tableMetas, Row row)
        {
            var entityType = GetType();
            var fields = GetFieldFields(entityType);
            ID = row.ID;

            int metaCount = tableMetas.Count;
            int fieldCount = fields.Count;

            for (int i = 0; i < metaCount && i < fieldCount; i++)
            {
                var meta = tableMetas[i];
                var field = fields[i];
                var cell = row.GetData(i);
                if (cell == null) continue;

                if (meta.Type != null && meta.Type.TypeKey == "view")
                {
                    RegisterAsLazyLoad(field, cell.SerializedData);
                    continue;
                }

                var descriptor = TypeRegistry.GetDescriptor(meta.Type?.TypeKey);
                if (descriptor?.LoadType == ODDBLoadType.Async)
                {
                    field.SetValue(this, cell.SerializedData);
                    continue;
                }

                var value = cell.GetData();
                if (value == null)
                {
                    if (ODDB.DebugLog)
                    {
                        ODDB.Logger.Warn($"[Import Warning][{entityType.Name}] Field '{field.Name}' got 'null' from meta '{meta.Name}'");
                    }
                    continue;
                }

                try
                {
                    field.SetValue(this, value);
                }
                catch (Exception)
                {
                    if (ODDB.DebugLog)
                    {
                        ODDB.Logger.Error($"[Import Error][{entityType.Name}] Failed to set '{field.Name}' (Expected {field.FieldType}, Got {value.GetType()})");
                    }
                }
            }
        }

        private void RegisterAsLazyLoad(FieldInfo field, string rawValue)
        {
            if (_owner == null)
            {
                if (ODDB.DebugLog)
                {
                    ODDB.Logger.Warn(
                        $"[Import Warning][{GetType().Name}] View-typed field '{field.Name}' cannot be resolved: no owning ODDatabase. " +
                        $"Use Import(ODDatabase, fields, row) instead of Import(fields, row).");
                }
                return;
            }

            var owner = _owner;
            owner.RegisterOnDataPorted(() =>
            {
                var targetId = rawValue;
                var targetEntity = owner.GetEntity<ODDBEntity>(targetId);
                if (targetEntity == null)
                    return;

                if (field.FieldType.IsAssignableFrom(targetEntity.GetType()))
                    field.SetValue(this, targetEntity);
            });
        }
    }
}
