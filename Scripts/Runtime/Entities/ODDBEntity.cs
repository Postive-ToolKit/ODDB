using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Settings;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Entities
{
    public abstract class ODDBEntity
    {
        public static readonly BindingFlags FieldFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
        private static Dictionary<Type, List<FieldInfo>> _fieldFieldCache = new Dictionary<Type, List<FieldInfo>>();
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
                    .Where(f => f.IsDefined(typeof(CompilerGeneratedAttribute), false) == false);
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

                var value = cell.GetData();
                if (value == null)
                {
                    if (ODDBRuntimeSettings.Setting.UseDebugLog)
                    {
                        ODDB.Logger.Warn($"[Import Warning][{entityType.Name}] Field '{field.Name}' got 'null' from meta '{meta.Name}'");
                    }
                    continue;
                }

                // FieldInfo.SetValue is relatively slow, but we've already cached the FieldInfo objects.
                // For even more performance, one could use Expression Trees to compile setters, 
                // but let's stick to cached FieldInfo for maximum AOT compatibility first.
                try 
                {
                    field.SetValue(this, value);
                }
                catch (Exception)
                {
                    if (ODDBRuntimeSettings.Setting.UseDebugLog)
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
                if (ODDBRuntimeSettings.Setting.UseDebugLog)
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