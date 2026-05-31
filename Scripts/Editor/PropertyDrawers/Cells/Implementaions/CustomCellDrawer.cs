using System;
using TeamODD.ODDB.Editors.Attributes;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Serializers;
using TeamODD.ODDB.Runtime.Types;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    /// <summary>
    /// Fallback drawer for the "custom" type key (v1 ODDBDataType.Custom = 9999 maps
    /// to this in the migration). When <c>param</c> matches another registered cell
    /// drawer key, dispatch to it so users can register their own per-type drawer
    /// via <c>[CellDrawer("MyTypeKey")]</c> without having to replace the "custom"
    /// slot entirely. When no delegate is found, fall back to a raw string editor.
    /// </summary>
    [CellDrawer("custom")]
    public class CustomCellDrawer : IODDBCellDrawer
    {
        private static readonly IDataSerializer _serializer =
            TypeRegistry.Get("custom") ?? new CustomSerializer();

        public VisualElement CreatePropertyGUI(Cell cell, string typeKey, string param, Action<string> commit)
        {
            // Delegate to a Param-keyed drawer when one is registered. Guard against
            // recursing into ourselves (param=="custom") in case anyone configures
            // a field that way.
            if (!string.IsNullOrEmpty(param) && param != "custom")
            {
                var delegateDrawer = CellDrawerRegistry.Get(param);
                if (delegateDrawer != null && delegateDrawer.GetType() != typeof(CustomCellDrawer))
                    return delegateDrawer.CreatePropertyGUI(cell, typeKey, param, commit);
            }

            // Fallback: raw string TextField. The column header already shows the
            // Param (full type name) so we don't duplicate it in the cell body.
            var value = _serializer.Deserialize(cell.SerializedData, param) as string;
            var textField = new TextField()
            {
                value = value ?? string.Empty
            };
            textField.RegisterValueChangedCallback(evt =>
            {
                commit(_serializer.Serialize(evt.newValue, param));
            });
            return textField;
        }
    }
}
