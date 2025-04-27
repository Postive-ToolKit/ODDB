using System.Collections.Generic;
using TeamODD.ODDB.Editors.UI.Fields;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public class ODDBViewEditor : ODDBMultiColumnEditor
    {
        private IODDBView _view;
        private List<string> _columnNames = new List<string>();
        private const float DELETE_COLUMN_WIDTH = 30f;
        private readonly IODDBEditorUseCase _editorUseCase;
        public ODDBViewEditor()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            selectionType = SelectionType.Single;
            showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            showBorder = true;
            horizontalScrollingEnabled = false;
            style.flexShrink = 1;

            schedule.Execute(Update).Every(100);
        }

        private void Update()
        {
            if (IsDirty)
            {
                IsDirty = false;
                RefreshColumns();
                RefreshItems();
                Rebuild();
            }
        }
        public override void SetView(string viewKey)
        {
            _view = _editorUseCase.GetViewByKey(viewKey);
            if (_view == null) {
                return;
            }
            RefreshColumns();
            RefreshItems();
        }
        
        private void RefreshColumns()
        {
            if (_view == null) return;

            columns.Clear();
            _columnNames.Clear();
            
            columns.Add(CreateKeyColumn());
            
            var nameColumn = new Column()
            {
                title = "Name",
                name = "Name",
                maxWidth = 300,
                width = 60,
                minWidth = 60,
                stretchable = true,
                resizable = true,
            };
            nameColumn.makeCell = CreateNameView;
            nameColumn.bindCell = BindNameCell;
            columns.Add(nameColumn);
            
            
            var typeColumn = new Column()
            {
                title = "Type",
                name = "Type",
                maxWidth = 300,
                width = 60,
                minWidth = 60,
                stretchable = true,
                resizable = true,
            };
            typeColumn.makeCell = CreateTypeSelectView;
            typeColumn.bindCell = BindTypeCell;
            columns.Add(typeColumn);
            
            
            
            columns.Add(CreateToolColumn());
        }
        
        private Column CreateKeyColumn()
        {
            var keyColumn = new Column()
            {
                title = "Key",
                name = "Key",
                maxWidth = 100,
                minWidth = 100,
                stretchable = true,
                resizable = true
            };
            keyColumn.makeCell =
                () => new TextField()
                {
                    style =
                    {
                        flexShrink = 1,
                        unityTextAlign = TextAnchor.MiddleLeft
                    },
                    isReadOnly = true,
                };
            keyColumn.bindCell = (element, index) =>
            {
                if (_view != null && index < _view.TotalFields.Count)
                    (element as TextField)!.value = _view.TotalFields[index].ID;
            };
            return keyColumn;
        }

        private void BindNameCell(VisualElement element, int index)
        {
            var container = element as VisualElement;
            var field = container.userData as ODDBStringField;
            if (field == null)
                return;
            if (index >= _view.TotalFields.Count)
                return;
            
            var value = _view.TotalFields[index];
            field.SetValue(value.Name);
            field.RegisterValueChangedCallback((changedName) =>
            {
                if (_view == null)
                    return;
                _view.TotalFields[index].Name = changedName.ToString();
            });
        }

        private VisualElement CreateNameView()
        {
            var container = new ODDBFieldBase();
            // string field
            var field = new ODDBStringField();
            container.Add(field.Root);
            container.userData = field;
            return container;
        }

        private VisualElement CreateTypeSelectView()
        {
            var container = new ODDBFieldBase();
            var field = new ODDBMetaSelectView();
            container.Add(field);
            container.userData = field;
            return container;
        }

        private void BindTypeCell(VisualElement element, int index)
        {
            var container = element as VisualElement;
            var field = container.userData as ODDBMetaSelectView;
            if (field == null)
                return;
            if (index >= _view.TotalFields.Count)
                return;
            var value = _view.TotalFields[index];
            field.SetType(value.Type);
            field.OnTypeChanged += type =>
            {
                if (_view == null)
                    return;
                _view.TotalFields[index].Type = type;
                IsDirty = true;
            };
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
            
            // row will work like delete row button
            toolColumn.makeCell = () =>
            {
                var button = new Button()
                {
                    text = "-",
                    style =
                    {
                        flexGrow = 0,
                        flexShrink = 0,
                        unityTextAlign = TextAnchor.MiddleCenter
                    }
                };
                return button;
            };

            toolColumn.bindCell = (element, index) =>
            {
                var button = element as Button;
                if (button != null)
                {
                    button.clicked += () =>
                    {
                        if (_view != null && index < _view.TotalFields.Count)
                        {
                            _view.RemoveField(index);
                            IsDirty = true;
                        }
                    };
                }
            };

            return toolColumn;
        }
        
        private new void RefreshItems()
        {
            if (_view == null) return;
            
            itemsSource = new List<int>();
            for (int i = 0; i < _view.TotalFields.Count; i++)
            {
                (itemsSource as List<int>).Add(i);
            }
            Rebuild();
        }
    }
}