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
    public class CustomCellDrawer : IODDBReusableCellDrawer
    {
        private static readonly IDataSerializer _serializer =
            TypeRegistry.Get("custom") ?? new CustomSerializer();

        private sealed class CustomCellHost : VisualElement
        {
            public IODDBCellDrawer DelegateDrawer { get; set; }
            public IODDBReusableCellDrawer ReusableDrawer { get; set; }
            public VisualElement Editor { get; set; }
            public Action<string> Commit { get; set; }
        }

        public VisualElement CreatePropertyGUI(Cell cell, string typeKey, string param, Action<string> commit)
        {
            var delegateDrawer = ResolveDelegate(param);
            if (delegateDrawer != null)
                return delegateDrawer.CreatePropertyGUI(cell, typeKey, param, commit);

            var element = CreateReusablePropertyGUI(typeKey, param, commit);
            BindPropertyGUI(element, cell, typeKey, param);
            return element;
        }

        public VisualElement CreateReusablePropertyGUI(string typeKey, string param, Action<string> commit)
        {
            var host = new CustomCellHost { Commit = commit };
            host.style.flexGrow = 1;

            var delegateDrawer = ResolveDelegate(param);
            if (delegateDrawer != null)
            {
                host.DelegateDrawer = delegateDrawer;
                host.ReusableDrawer = delegateDrawer as IODDBReusableCellDrawer;
                if (host.ReusableDrawer != null)
                {
                    host.Editor = host.ReusableDrawer.CreateReusablePropertyGUI(typeKey, param, commit);
                    host.Add(host.Editor);
                }
                return host;
            }

            var hint = new Label(string.IsNullOrEmpty(param)
                ? "No CellDrawer registered for this custom field. Add a class with [CellDrawer(\"<your-key>\")] in your Editor code, then put that key in this field's Param."
                : $"No CellDrawer for '{param}'. Register one in your Editor code: [CellDrawer(\"{param}\")] public class … : IODDBCellDrawer {{ … }}");
            hint.style.whiteSpace = WhiteSpace.Normal;
            hint.style.unityFontStyleAndWeight = UnityEngine.FontStyle.Italic;
            hint.style.opacity = 0.7f;
            hint.style.paddingBottom = 2;
            host.Add(hint);

            var textField = new TextField();
            textField.RegisterValueChangedCallback(evt => commit(_serializer.Serialize(evt.newValue, param)));
            host.Editor = textField;
            host.Add(textField);
            return host;
        }

        public void BindPropertyGUI(VisualElement element, Cell cell, string typeKey, string param)
        {
            var host = (CustomCellHost)element;
            if (host.ReusableDrawer != null)
            {
                host.ReusableDrawer.BindPropertyGUI(host.Editor, cell, typeKey, param);
                return;
            }

            if (host.DelegateDrawer != null)
            {
                host.Clear();
                host.Editor = host.DelegateDrawer.CreatePropertyGUI(cell, typeKey, param, host.Commit);
                host.Add(host.Editor);
                return;
            }

            ((TextField)host.Editor).SetValueWithoutNotify(
                _serializer.Deserialize(cell.SerializedData, param) as string ?? string.Empty);
        }

        private static IODDBCellDrawer ResolveDelegate(string param)
        {
            if (string.IsNullOrEmpty(param) || param == "custom")
                return null;

            var drawer = CellDrawerRegistry.Get(param);
            return drawer != null && drawer.GetType() != typeof(CustomCellDrawer) ? drawer : null;
        }
    }
}
