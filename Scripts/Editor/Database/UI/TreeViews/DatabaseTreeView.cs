using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEngine.UIElements;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.UI
{
    public class DatabaseTreeView : TreeView, IUpdateUI
    {
        public bool IsDirty { get; set; }
        private readonly ODDatabase _database;
        private readonly IODDBEditorUseCase _editorUseCase;
        private readonly Dictionary<string, int> _idMapping = new Dictionary<string, int>(); 
        private IView _view;
        public event Action<string> OnViewSelected;
        public DatabaseTreeView()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            _database = ODDBEditorDI.Resolve<ODDatabase>();
            _editorUseCase.OnViewChanged += UpdateView;
            autoExpand = true;
            
            selectionType = SelectionType.Single;
            // 스타일 설정
            style.flexGrow = 1;
            
            makeItem = () => new Label() {
                style = {
                    unityTextAlign = TextAnchor.MiddleLeft,
                    flexGrow = 1
                },
            };
            bindItem = CreateVisualElement;
            selectionChanged += OnSelectionChanged;


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
            UpdateItemSource();
            Rebuild();
            schedule.Execute(Update).Every(100);
        }

        private void CreateVisualElement(VisualElement element, int index)
        {
            var data = GetItemDataForIndex<ViewContainer>(index);
            var label = (Label)element;
            label.text = data.Name;
        }
        private void UpdateItemSource()
        {
            if (_database == null)
                return;
            // var views = new List<IODDBView>();
            // views.AddRange(_database.Tables.GetAll());
            // views.AddRange(_database.Views.GetAll());
            // foreach (var view in views.ToList())
            //     views.Add(view);
            //
            // itemsSource = views;
            _idMapping.Clear();
            var rootItems = new List<TreeViewItemData<ViewContainer>>();
            int cnt = 2;
            var tableList = new List<TreeViewItemData<ViewContainer>>();
            foreach (var table in _database.Tables.GetAll())
            {
                var item = new ViewContainer();
                item.Name = table.Name;
                item.Type = ViewContainerType.View;
                item.View = table;
                _idMapping.Add(table.ID, cnt);
                var treeItem = new TreeViewItemData<ViewContainer>(cnt++, item);
                tableList.Add(treeItem);
            }

            var tableItemData = new ViewContainer();
            tableItemData.Name = "Tables";
            tableItemData.Type = ViewContainerType.Repository;
            var tables = new TreeViewItemData<ViewContainer>(0, tableItemData, tableList);
            rootItems.Add(tables);
            
            var viewList = new List<TreeViewItemData<ViewContainer>>();
            foreach (var view in _database.Views.GetAll())
            {
                var item = new ViewContainer();
                item.Name = view.Name;
                item.Type = ViewContainerType.View;
                item.View = view;
                _idMapping.Add(view.ID, cnt);
                var treeItem = new TreeViewItemData<ViewContainer>(cnt++, item);
                viewList.Add(treeItem);
            }
            var viewItemData = new ViewContainer();
            viewItemData.Name = "Views";
            viewItemData.Type = ViewContainerType.Repository;
            var views = new TreeViewItemData<ViewContainer>(1, viewItemData, viewList);
            rootItems.Add(views);
            SetRootItems(rootItems);
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
                _database.Tables.Create();
                IsDirty = true;
            });
            menu.AddItem(new GUIContent("Add/View"), false, () =>
            {
                _database.Views.Create();
                IsDirty = true;
            });
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                if (_view == null)
                    return;
                if (_view is Table table)
                {
                    _database.Tables.Delete(table.ID);
                }
                else if (_view is View view)
                {
                    _database.Views.Delete(view.ID);
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
                if (item is ViewContainer container)
                {
                    if (container.Type == ViewContainerType.Repository)
                        return;
                    _view = container.View;
                    OnViewSelected?.Invoke(_view.ID);
                    return;
                }
            }
            _view = null;
            OnViewSelected?.Invoke(null);
        }
        
        private void UpdateView(string viewId)
        {
            if (_database == null)
                return;
            var view = _database.GetView(new ODDBID(viewId));
            if (view == null)
                return;
            if (!_idMapping.ContainsKey(view.ID))
                return;
            var index = _idMapping[view.ID];
            var data = GetItemDataForIndex<ViewContainer>(index);
            data.Name = view.Name;
            RefreshItem(index);
        }
    }
}
