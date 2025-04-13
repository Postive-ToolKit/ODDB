using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeamODD.ODDB.Scripts.Runtime.Data;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Entities
{
    public abstract class ODDBEntity
    {
        public void Import(List<ODDBTableMeta> tableMetas, ODDBRow row)
        {
            var converter = new ODDBDataConverter();
            var entityType = this.GetType(); // 현재 인스턴스의 실제 타입
            var fields = entityType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .OrderBy(f => f.MetadataToken) // 대체적으로 선언 순서에 가까움
                .ToList();
            
            var fieldIndex = 0;
            
            for (int i = 0; i < tableMetas.Count && fieldIndex < fields.Count; i++)
            {
                var meta = tableMetas[i];
                var field = fields[fieldIndex];
                var targetType = field.FieldType;
                var rawValue = row.GetData(i);
                var convertedValue = converter.Convert(rawValue, meta.DataType);

                if (targetType.IsInstanceOfType(convertedValue))
                {
                    field.SetValue(this, convertedValue);
                    fieldIndex++;
                }
                else
                {
                    Debug.LogError($"[Import Error] Field '{field.Name}' expects type '{targetType}', " +
                                   $"but got '{convertedValue?.GetType()}' from meta '{meta.Name}'");
                }
            }

        }
    }
}