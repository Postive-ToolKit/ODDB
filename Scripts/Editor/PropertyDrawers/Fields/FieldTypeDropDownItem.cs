using System;
using TeamODD.ODDB.Runtime.Enums;
using UnityEditor.IMGUI.Controls;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    public class FieldTypeDropDownItem : AdvancedDropdownItem
    {
        public string TypeKey { get; }
        public string Param { get; }

        public FieldTypeDropDownItem(string typeKey, string param, string displayName)
            : base(displayName)
        {
            TypeKey = typeKey ?? string.Empty;
            Param = param ?? string.Empty;
        }

        // Back-compat — used by legacy enum-based call sites until T13.
        [Obsolete("Use (string typeKey, string param, string displayName) overload.")]
        public FieldTypeDropDownItem(string displayName, ODDBDataType type, string param = null)
            : base(displayName)
        {
            TypeKey = type.ToWireKey();
            Param = param ?? string.Empty;
        }

        [Obsolete("Use TypeKey. Will be removed in T13.")]
        public ODDBDataType Type
        {
            get
            {
                if (Enum.TryParse<ODDBDataType>(TypeKey, true, out var parsed))
                    return parsed;
                return ODDBDataType.Custom;
            }
        }
    }
}
