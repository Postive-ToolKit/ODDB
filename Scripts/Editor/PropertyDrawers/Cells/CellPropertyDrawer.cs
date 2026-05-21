using TeamODD.ODDB.Runtime;
using UnityEditor;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.PropertyDrawers
{
    [CustomPropertyDrawer(typeof(Cell))]
    public class CellPropertyDrawer : PropertyDrawer
    {
        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var fieldTypeProp = property.FindPropertyRelative(Cell.DATA_TYPE_FIELD);
            var typeKeyProp = fieldTypeProp.FindPropertyRelative(FieldType.TYPE_KEY_FIELD);
            var paramProp = fieldTypeProp.FindPropertyRelative(FieldType.PARAM_FIELD);

            string typeKey = typeKeyProp?.stringValue ?? string.Empty;
            string param = paramProp?.stringValue ?? string.Empty;

            var drawer = CellDrawerRegistry.Get(typeKey);
            if (drawer == null)
            {
                // No drawer registered for this key — fall back to a text label.
                return new Label($"<no drawer for '{typeKey}'>");
            }
            return drawer.CreatePropertyGUI(property, typeKey, param);
        }
    }
}
