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

            // Fallback: no per-Param drawer registered. Show a clear "register one"
            // hint above a raw string editor so the user knows what to do, but can
            // still inspect / hand-edit the underlying value if needed.
            var container = new VisualElement();
            container.style.flexGrow = 1;

            var hint = new Label(string.IsNullOrEmpty(param)
                ? "No CellDrawer registered for this custom field. Add a class with [CellDrawer(\"<your-key>\")] in your Editor code, then put that key in this field's Param."
                : $"No CellDrawer for '{param}'. Register one in your Editor code: [CellDrawer(\"{param}\")] public class … : IODDBCellDrawer {{ … }}");
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Italic;
            hint.style.opacity = 0.7f;
            hint.style.paddingBottom = 2;
            container.Add(hint);

            var value = _serializer.Deserialize(cell.SerializedData, param) as string;
            var textField = new TextField()
            {
                value = value ?? string.Empty
            };
            textField.RegisterValueChangedCallback(evt =>
            {
                commit(_serializer.Serialize(evt.newValue, param));
            });
            container.Add(textField);
            return container;
        }
    }
}
