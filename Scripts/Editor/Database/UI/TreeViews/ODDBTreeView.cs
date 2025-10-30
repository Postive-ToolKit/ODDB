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
    public class ODDBTreeView : TreeView, IUpdateUI
    {
        public event Action<string> OnViewSelected;
        public bool IsDirty { get; set; }
        private readonly ODDatabase _database;
        private readonly IODDBEditorUseCase _editorUseCase;
        private readonly Dictionary<string, int> _indexMapping = new Dictionary<string, int>();
        private readonly Dictionary<string, Action> _itemActions = new Dictionary<string, Action>();
        private readonly List<Type> _viewTypes = new();
        private IView _view;

        private int _itemIds = 0;
        
        public ODDBTreeView(params Type[] viewTypes)
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            _database = ODDBEditorDI.Resolve<ODDatabase>();
            _editorUseCase.OnViewChanged += UpdateView;
            SetTypes(viewTypes);
            autoExpand = true;
            
            selectionType = SelectionType.Single;
            // 스타일 설정
            style.flexGrow = 1;
            
            makeItem = () => 
                new Label() 
                {
                    style = {
                        unityTextAlign = TextAnchor.MiddleLeft,
                        flexGrow = 1,
                    },
                };
            bindItem = CreateVisualElement;
            selectionChanged += OnSelectionChanged;

            showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            showBorder = true;
            
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
            //Rebuild();
            schedule.Execute(Update).Every(100);
        }
        
        public void SetTypes(params Type[] viewTypes)
        {
            _viewTypes.Clear();
            _viewTypes.AddRange(viewTypes);
            IsDirty = true;
        }

        private void CreateVisualElement(VisualElement element, int index)
        {
            var data = GetItemDataForIndex<IView>(index);
            var label = (Label)element;
            label.text = data.Name;
            
            // 
            
            element.style.backgroundColor = data is Table ?
                new Color(1f, .5f, .5f, 0.3f) :
                new Color(.5f, .5f, 1f, 0.3f);
            _itemActions[data.ID] = () => label.text = data.Name;
        }
        private void UpdateItemSource()
        {
            if (_database == null)
                return;
            
            var views = _editorUseCase
                .GetViews()
                .Where(v => _viewTypes.Contains(v.GetType()))
                .ToList();
            var targets = new List<ViewContainer>();
            var dict = new Dictionary<string, ViewContainer>();
            foreach (var v in views)
            {
                var item = new ViewContainer();
                item.View = v;
                dict.Add(v.ID, item);
                targets.Add(item);
            }
            
            foreach (var v in views)
            {
                if (v.ParentView == null)
                    continue;
                if (!dict.ContainsKey(v.ParentView.ID))
                    continue;
                var self = dict[v.ID];
                var parent = dict[v.ParentView.ID];
                targets.Remove(self);
                parent.Children.Add(self);
            }
            
            _itemActions.Clear();
            _indexMapping.Clear();
            _itemIds = 0;
            var results = new List<TreeViewItemData<IView>>();
            foreach (var view in targets)
            {
                var item = TraverseContainer(view);
                results.Add(item);
            }
            
            SetRootItems(results);
        }
        
        private TreeViewItemData<IView> TraverseContainer(ViewContainer container)
        {
            var children = new List<TreeViewItemData<IView>>();
            foreach (var child in container.Children)
            {
                var childItem = TraverseContainer(child);
                children.Add(childItem);
            }
            var item = new TreeViewItemData<IView>(_itemIds,container.View, children);
            _indexMapping[container.View.ID] = _itemIds;
            _itemIds++;
            return item;
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

            if (_viewTypes.Contains(typeof(Table)))
            {
                menu.AddItem(new GUIContent("Add/Table"), false, () =>
                {
                    _database.Tables.Create();
                    IsDirty = true;
                });
            }

            if (_viewTypes.Contains(typeof(View)))
            {
                menu.AddItem(new GUIContent("Add/View"), false, () =>
                {
                    _database.Views.Create();
                    IsDirty = true;
                });
            }

            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                if (_view == null)
                    return;
                if (_view is Table table)
                    _database.Tables.Delete(table.ID);
                else if (_view is View view)
                    _database.Views.Delete(view.ID);
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
                if (item is IView view)
                {
                    _view = view;
                    OnViewSelected?.Invoke(_view.ID);
                    return;
                }
            }
            _view = null;
        }
        
        private void UpdateView(string viewId)
        {
            if (_database == null)
                return;
            var view = _database.GetView(new ODDBID(viewId));
            if (view == null)
                return;
            if (!_indexMapping.ContainsKey(view.ID))
                return;
            if (_itemActions.ContainsKey(viewId)) 
                _itemActions[viewId].Invoke();
            IsDirty = true;
        }
    }
}
