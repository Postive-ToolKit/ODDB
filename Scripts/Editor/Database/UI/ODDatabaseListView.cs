using System;
using System.Collections.Generic;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.UI
{
    public class ODDatabaseListView : ListView, IODDBUpdateUI
    {
        public bool IsDirty { get; set; }
        private readonly ODDatabase _database;
        private readonly IODDBEditorUseCase _editorUseCase;
        private IODDBView _view;
        public event Action<string> OnViewSelected;
        public ODDatabaseListView()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            _database = ODDBEditorDI.Resolve<ODDatabase>();
            _editorUseCase.OnViewChanged += UpdateView;
            
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

            UpdateItemSource();
            Rebuild();
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
        
        private void CreateDatabaseContextMenu(ContextClickEvent evt)
        {
            var menu = new GenericMenu();
            
            menu.AddItem(new GUIContent("Add/Table"), false, () =>
            {
                _database.CreateTable();
                IsDirty = true;
            });
            menu.AddItem(new GUIContent("Add/View"), false, () =>
            {
                _database.CreateView();
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

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            foreach (var item in selectedItems)
            {
                if (item is IODDBView view)
                {
                    _view = view;
                    OnViewSelected?.Invoke(view.Key);
                    return;
                }
                    
            }
        }
        
        private void UpdateView(string viewId)
        {
            var view = _database.GetViewByKey(viewId);
            var index = itemsSource.IndexOf(view);
            RefreshItem(index);
        }
    }
}
