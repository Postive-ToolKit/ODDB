using TeamODD.ODDB.Editors.Settings;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI.Dialogs
{
    public sealed class ODDBTableAppearanceWindow : EditorWindow
    {
        private IODDBEditorUseCase _useCase;
        private string _tableId;
        private TextField _tagField;
        private ColorField _colorField;

        public static void ShowForTable(IODDBEditorUseCase useCase, string tableId)
        {
            if (useCase == null || string.IsNullOrEmpty(tableId) || useCase.GetViewByKey(tableId) is not Table)
                return;

            var window = CreateInstance<ODDBTableAppearanceWindow>();
            window._useCase = useCase;
            window._tableId = tableId;
            window.titleContent = new GUIContent("Table Tag and Color");
            window.minSize = new Vector2(380, 150);
            window.maxSize = new Vector2(380, 150);
            window.ShowModalUtility();
        }

        private void CreateGUI()
        {
            var settings = ODDBEditorSettings.Setting;
            var root = rootVisualElement;
            root.style.paddingLeft = 10;
            root.style.paddingRight = 10;
            root.style.paddingTop = 10;
            root.style.paddingBottom = 10;

            _tagField = new TextField("Tag") { value = settings.GetTableTag(_tableId) };
            _tagField.tooltip = "Search for this tag in the left table list.";
            root.Add(_tagField);

            _colorField = new ColorField("Color")
            {
                value = settings.GetTableColor(_tableId),
                showAlpha = false
            };
            _colorField.tooltip = "Color of this table in the left list.";
            root.Add(_colorField);

            var buttons = new VisualElement
            {
                style =
                {
                    flexDirection = FlexDirection.Row,
                    justifyContent = Justify.FlexEnd,
                    marginTop = 12
                }
            };
            buttons.Add(new Button(() =>
            {
                _tagField.value = string.Empty;
                _colorField.value = ODDBEditorSettings.DefaultTableColor;
            }) { text = "Reset" });
            buttons.Add(new Button(Close) { text = "Cancel" });
            buttons.Add(new Button(Save) { text = "Save" });
            root.Add(buttons);

            _tagField.Focus();
        }

        private void Save()
        {
            if (_useCase.GetViewByKey(_tableId) is Table)
                ODDBEditorSettings.Setting.SetTableAppearance(_tableId, _tagField.value, _colorField.value);
            Close();
        }
    }
}
