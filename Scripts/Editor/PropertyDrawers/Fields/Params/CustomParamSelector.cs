using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Params.Interfaces;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Type-centric selector for custom data types.
    /// Lists all Types registered with CustomDataTypeAttribute.
    /// </summary>
    [UseSubSelector("custom")]
    public class CustomParamSelector : IFieldParamSelector
    {
        private Dictionary<string, string> _options;

        public Dictionary<string, string> GetOptions()
        {
            if (_options != null)
                return _options;

            _options = new Dictionary<string, string>();

            var customTypes = AppDomain.CurrentDomain
                .GetAssemblies()
                .SelectMany(assembly => assembly.GetTypes());

            foreach (var type in customTypes)
            {
                var attr = type.GetCustomAttribute<CustomDataTypeAttribute>();
                if (attr == null) continue;

                // 중복 방지 (TypeID 기준)
                if (_options.ContainsKey(attr.TypeID)) continue;

                // 화면에 보일 이름 (예: UnityEngine.Color -> Color)
                string displayName = attr.DataType?.Name ?? attr.TypeID;
                _options.Add(attr.TypeID, displayName);
            }

            return _options;
        }
    }
}