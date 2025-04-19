using System.Collections.Generic;
using TeamODD.ODDB.Editors.UI.Fields;
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

        public ODDBViewEditor()
        {
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
        public override void SetView(IODDBView view)
        {
            _view = view;
            RefreshColumns();
            RefreshItems();
        }
        
        private void RefreshColumns()
        {
            if (_view == null) return;

            columns.Clear();
            _columnNames.Clear();
            
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

        private void BindNameCell(VisualElement element, int index)
        {
            var container = element as VisualElement;
            var field = container.userData as ODDBStringField;
            if (field == null)
                return;
            if (index >= _view.TableMetas.Count)
                return;
            
            var value = _view.TableMetas[index];
            field.SetValue(value.Name);
            field.RegisterValueChangedCallback((changedName) =>
            {
                if (_view == null)
                    return;
                _view.TableMetas[index] = new ODDBTableMeta(value.DataType, changedName.ToString());
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
            if (index >= _view.TableMetas.Count)
                return;
            var value = _view.TableMetas[index];
            field.SetType(value.DataType);
            field.OnTypeChanged += type =>
            {
                if (_view == null)
                    return;
                _view.TableMetas[index] = new ODDBTableMeta(type, value.Name);
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
                        if (_view != null && index < _view.TableMetas.Count)
                        {
                            _view.RemoveTableMeta(index);
                            IsDirty = true;
                        }
                    };
                }
            };

            return toolColumn;
        }
        
        private void RefreshItems()
        {
            if (_view == null) return;
            
            itemsSource = new List<int>();
            for (int i = 0; i < _view.TableMetas.Count; i++)
            {
                (itemsSource as List<int>).Add(i);
            }
            Rebuild();
        }
    }
}