using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.Enum;
using TeamODD.ODDB.Runtime.Utils;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Entities
{
    public abstract class ODDBEntity
    {
        public void Import(List<ODDBField> tableMetas, ODDBRow row)
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

                if (meta.Type == ODDBDataType.View)
                {
                    RegisterAsLazyLoad(field, rawValue);
                    continue;
                }
                
                var convertedValue = converter.Convert(rawValue, meta.Type);
                
                //Debug.Log("[ODDBImporter] Converted value: " + convertedValue + " for field: " + field.Name);

                if (targetType.IsInstanceOfType(convertedValue))
                {
                    field.SetValue(this, convertedValue);
                }
                else
                {
                    // change above to exception to string builder
                    var stringBuilder = new StringBuilder();
                    stringBuilder.Append($"[Import Error] Field '{field.Name}' expects type '{targetType}', ");
                    stringBuilder.Append($"but got '{convertedValue?.GetType()}' from meta '{meta.Name}'");
                    stringBuilder.AppendLine();
                    Debug.LogError(stringBuilder.ToString());
                }
                fieldIndex++;
            }

        }

        private void RegisterAsLazyLoad(FieldInfo field, string rawValue)
        {
            ODDBPort.RegisterOnDataPortedCallback(() =>
            {
                var path = rawValue;
                var targetId = path.Split('/').LastOrDefault();
                field.SetValue(this, ODDBPort.GetEntity<ODDBEntity>(targetId));
            });
        }
    }
}