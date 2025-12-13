using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Settings;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Entities
{
    public abstract class ODDBEntity
    {
        public static readonly BindingFlags FieldFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        
        public void Import(List<Field> tableMetas, Row row)
        {
            var entityType = this.GetType(); // 현재 인스턴스의 실제 타입
            var fields = entityType.GetFields(FieldFlags)
                .OrderBy(f => f.MetadataToken) // 대체적으로 선언 순서에 가까움
                .ToList();
            
            var fieldIndex = 0;
            
            for (int i = 0; i < tableMetas.Count && fieldIndex < fields.Count; i++)
            {
                var meta = tableMetas[i];
                var field = fields[fieldIndex];
                var targetType = field.FieldType;
                var rawValue = row.GetData(i).SerializedData;

                if (meta.Type == ODDBDataType.View)
                {
                    RegisterAsLazyLoad(field, rawValue);
                    continue;
                }

                var value = row.GetData(i).GetData();

                if (targetType.IsInstanceOfType(value))
                {
                    field.SetValue(this, value);
                }
                else if (value == null)
                {
                    if (ODDBSettings.Setting.UseDebugLog)
                    {
                        var stringBuilder = new StringBuilder();
                        stringBuilder.Append($"[Import Warning][{GetType()}] Field '{field.Name}' expects type '{targetType}', ");
                        stringBuilder.Append($"but got 'null' from meta '{meta.Name}'");
                        stringBuilder.AppendLine();
                        Debug.LogWarning(stringBuilder.ToString());
                    }
                }
                else
                {
                    if (ODDBSettings.Setting.UseDebugLog)
                    {
                        var stringBuilder = new StringBuilder();
                        stringBuilder.Append($"[Import Error][{GetType()}] Field '{field.Name}' expects type '{targetType}', ");
                        stringBuilder.Append($"but got '{value?.GetType()}' from meta '{meta.Name}'");
                        stringBuilder.AppendLine();
                        Debug.LogError(stringBuilder.ToString());
                    }

                }
                fieldIndex++;
            }

        }

        private void RegisterAsLazyLoad(FieldInfo field, string rawValue)
        {
            ODDBPort.RegisterOnDataPortedCallback(() =>
            {
                var targetId = rawValue;
                var targetEntity = ODDBPort.GetEntity<ODDBEntity>(targetId);
                if (targetEntity == null)
                    return;
                
                // try parse to field type
                var parsedEntity = Convert.ChangeType(targetEntity, field.FieldType);
                if (parsedEntity == null)
                    return;
                
                field.SetValue(this, parsedEntity);
            });
        }
    }
}