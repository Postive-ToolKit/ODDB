using TeamODD.ODDB.Editors.PropertyDrawers;
using TeamODD.ODDB.Editors.Utils.Elements;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public class ViewEditor : MultiColumnEditor
    {
        private IView _view;
        private const float DELETE_COLUMN_WIDTH = 30f;
        private readonly IODDBEditorUseCase _editorUseCase;

        public ViewEditor()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            selectionType = SelectionType.Single;
            showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            reorderable = true;
            itemIndexChanged += (oldIndex, newIndex) =>
            {
                if (_view == null) return;

                // ListView already reordered the underlying list (_view.ScopedFields).
                // Revert it temporarily so the Command can perform the move and handle Undo properly.
                var list = _view.ScopedFields;
                var item = list[newIndex];
                list.RemoveAt(newIndex);
                list.Insert(oldIndex, item);

                int myStartIndex = 0;
                if (_view.ParentView != null)
                    myStartIndex = _view.ParentView.TotalFields.Count;

                _editorUseCase.MoveField(_view.ID, oldIndex + myStartIndex, newIndex + myStartIndex);
            };
            horizontalScrollingEnabled = true;
            showBorder = true;
            style.flexGrow = 1;

            columns.Add(CreateHandleColumn());
            columns.Add(CreateNameColumn());
            columns.Add(CreateTypeColumn());
            columns.Add(CreateToolColumn());
        }

        public override void SetView(string viewKey)
        {
            if (_view != null)
            {
                _view.OnFieldsChanged -= RefreshRows;
                _editorUseCase.OnViewChanged -= OnExternalViewChanged;
            }
            _view = _editorUseCase.GetViewByKey(viewKey);
            if (_view == null) return;

            itemsSource = _view.ScopedFields;
            RefreshRows();

            _view.OnFieldsChanged += RefreshRows;
            _editorUseCase.OnViewChanged += OnExternalViewChanged;
        }

        private void OnExternalViewChanged(string viewId)
        {
            if (_view == null || viewId != _view.ID) return;
            RefreshRows();
        }

        private void RefreshRows()
        {
            if (_view == null) return;
            itemsSource = _view.ScopedFields;
            RefreshItems();
        }

        private Column CreateHandleColumn()
        {
            var handleColumn = new Column()
            {
                title = "",
                maxWidth = 25f,
                width = 25f,
                minWidth = 25f,
                stretchable = false,
                resizable = false
            };

            handleColumn.makeCell = () =>
            {
                var container = new VisualElement();
                container.style.justifyContent = Justify.Center;
                container.style.alignItems = Align.Center;
                container.style.flexGrow = 1;

                var icon = new VisualElement();
                icon.style.width = 15;
                icon.style.height = 15;
                icon.style.backgroundImage = (StyleBackground)EditorGUIUtility.IconContent("d_VerticalLayoutGroup Icon").image;
                icon.style.opacity = 0.5f;

                container.Add(icon);
                return container;
            };

            handleColumn.bindCell = (element, index) => { };
            handleColumn.unbindCell = (element, index) => { };

            return handleColumn;
        }

        private Column CreateNameColumn()
        {
            var column = new Column()
            {
                title = "Name",
                stretchable = true,
            };
            column.makeCell = () => new TextField()
            {
                style = { flexGrow = 1 }
            };
            column.bindCell = (element, index) =>
            {
                if (_view == null || index < 0 || index >= _view.ScopedFields.Count) return;
                var textField = (TextField)element;
                var field = _view.ScopedFields[index];
                textField.SetValueWithoutNotify(field.Name);

                textField.UnregisterCallback<ChangeEvent<string>>(OnNameChanged);
                textField.userData = field;
                textField.RegisterCallback<ChangeEvent<string>>(OnNameChanged);
            };
            return column;
        }

        private void OnNameChanged(ChangeEvent<string> evt)
        {
            if (_view == null) return;
            if (evt.target is not TextField tf) return;
            if (tf.userData is not Field field) return;
            field.Name = evt.newValue;
            _editorUseCase.NotifyViewDataChanged(_view.ID);
        }

        private Column CreateTypeColumn()
        {
            var column = new Column()
            {
                title = "Type",
                stretchable = true,
            };
            column.makeCell = () => new Button()
            {
                style = { flexGrow = 1 }
            };
            column.bindCell = (element, index) =>
            {
                if (_view == null || index < 0 || index >= _view.ScopedFields.Count) return;
                var button = (Button)element;
                var field = _view.ScopedFields[index];
                var fieldType = field.Type ?? (field.Type = new FieldType());

                button.text = BuildTypeTitle(fieldType.TypeKey, fieldType.Param);
                button.clickable = new Clickable(() => { });
                button.clicked += () =>
                {
                    var dropdown = new FieldTypeDropDown(new AdvancedDropdownState());
                    dropdown.Show(button.worldBound);
                    dropdown.OnSelectionChanged += (newTypeKey, newParam, _) =>
                    {
                        fieldType.TypeKey = newTypeKey;
                        fieldType.Param = newParam ?? string.Empty;
                        button.text = BuildTypeTitle(newTypeKey, fieldType.Param);
                        _editorUseCase.NotifyViewDataChanged(_view.ID);
                    };
                };
            };
            return column;
        }

        private static string BuildTypeTitle(string typeKey, string param)
        {
            if (string.IsNullOrEmpty(typeKey))
                return "<unset>";
            return string.IsNullOrEmpty(param) ? typeKey : $"{typeKey} - {param}";
        }

        private Column CreateToolColumn()
        {
            var toolColumn = new Column()
            {
                title = "",
                name = "",
                maxWidth = DELETE_COLUMN_WIDTH,
                width = DELETE_COLUMN_WIDTH,
                minWidth = DELETE_COLUMN_WIDTH,
                stretchable = false,
                resizable = false
            };

            toolColumn.makeCell = () => new ODDBButton() { text = "-", };

            toolColumn.bindCell = (element, index) =>
            {
                var button = element as ODDBButton;
                button!.ClearCallbacks();
                button.AddOnClickCallback(evt =>
                {
                    if (_view == null) return;
                    var normalizedIndex = _view.TotalFields.Count - _view.ScopedFields.Count + index;
                    if (index < _view.TotalFields.Count)
                        _editorUseCase.RemoveField(_view.ID, normalizedIndex);
                });
            };

            return toolColumn;
        }
    }
}
