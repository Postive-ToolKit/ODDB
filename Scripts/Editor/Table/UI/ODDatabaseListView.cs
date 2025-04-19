using System;
using System.Collections.Generic;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using UnityEngine.UIElements;
using TeamODD.ODDB.Runtime.Settings.Data;
using TeamODD.ODDB.Scripts.Runtime.Data;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.UI
{
    public class ODDatabaseListView : ListView, IODDBUpdateUI, IODDBHasView
    {
        public bool IsDirty { get; set; }

        private ODDatabase _database;
        private IODDBView _view;
        public event Action<IODDBView> OnViewSelected;
        public ODDatabaseListView()
        {
            selectionType = SelectionType.Single;
            makeItem = () => new Label() {
                style = {
                    unityTextAlign = TextAnchor.MiddleLeft,
                    flexGrow = 1
                },
            };
            bindItem = CreateVisualElement;

            selectionChanged += OnSelectionChanged;

            // 스타일 설정
            style.flexGrow = 1;
            showBorder = true;
            showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            // add context menu when right click
            RegisterCallback<ContextClickEvent>(evt =>
            {
                if (evt.button != 1)
                    return;
                if (_database == null)
                    return;
                CreateDatabaseContextMenu(evt);
            });
            
            schedule.Execute(Update).Every(100);
        }

        private void CreateVisualElement(VisualElement element, int index)
        {
            var label = (Label)element;
            if (_database != null && index < _database.Count)
            {
                var view = itemsSource[index] as ODDBView;
                label.text = view!.Name;
            }
        }

        public void SetDatabase(ODDatabase database)
        {
            _database = database;
            UpdateItemSource();
            Rebuild();
        }

        private void UpdateItemSource()
        {
            var items = new List<ODDBView>();
            if (_database == null)
                return;
            
            foreach (var view in _database.GetViews())
                items.Add(view);
            
            itemsSource = items;
        }

        private void Update()
        {
            if(IsDirty)
            {
                IsDirty = false;
                UpdateItemSource();
                Rebuild();
            }
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            foreach (var item in selectedItems)
            {
                if (item is IODDBView view)
                {
                    _view = view;
                    OnViewSelected?.Invoke(view);
                    return;
                }
                    
            }
        }
        public void SetView(IODDBView view)
        {
            // find table index and update it
            if (_database == null)
                return;
            var index = itemsSource.IndexOf(view);
            this.RefreshItem(index);
        }
        
        private void CreateDatabaseContextMenu(ContextClickEvent evt)
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Add/Table"), false, () =>
            {
                var newTable = _database.CreateTable();
                newTable.Name = "New Table";
                IsDirty = true;
            });
            menu.AddItem(new GUIContent("Add/View"), false, () =>
            {
                var newView = _database.CreateView();
                newView.Name = "New View";
                IsDirty = true;
            });
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                if (_view == null)
                    return;
                if (_view is ODDBTable table)
                {
                    _database.RemoveTable(table);
                }
                else if (_view is ODDBView view)
                {
                    _database.RemoveView(view);
                }
                _view = null;
                OnViewSelected?.Invoke(null);
                IsDirty = true;
            });
            menu.ShowAsContext();
        }


    }
}
