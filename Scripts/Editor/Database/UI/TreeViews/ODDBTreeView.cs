using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Editors.CodeGen.UI;
using TeamODD.ODDB.Editors.UI.Interfaces;
using TeamODD.ODDB.Editors.UI.Menus;
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
        private readonly List<IView> _flatViews = new();
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
            
            makeItem = MakeItem;
            bindItem = CreateVisualElement;
            selectionChanged += OnSelectionChanged;
            reorderable = true;
            itemIndexChanged += OnItemIndexChanged;

            showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            showBorder = true;
            
            RegisterCallback<ContextClickEvent>(CreateBackgroundContextMenu);
            UpdateItemSource();
        }

        private VisualElement MakeItem()
        {
            var container = new VisualElement
            {
                style = {
                    flexDirection = FlexDirection.Row,
                    alignItems = Align.Center,
                    flexGrow = 1,
                }
            };

            var icon = new VisualElement
            {
                style = {
                    width = 8,
                    height = 8,
                    marginRight = 4,
                    marginLeft = 2,
                    borderTopLeftRadius = 2,
                    borderTopRightRadius = 2,
                    borderBottomLeftRadius = 2,
                    borderBottomRightRadius = 2,
                }
            };

            var label = new Label
            {
                style = {
                    unityTextAlign = TextAnchor.MiddleLeft,
                    flexGrow = 1,
                },
            };

            container.Add(icon);
            container.Add(label);

            container.RegisterCallback<ContextClickEvent>(evt =>
            {
                if (label.userData is not IView view)
                    return;

                _view = view;
                SetSelectionWithoutNotify(new[] { _indexMapping[view.ID] });
                OnViewSelected?.Invoke(view.ID);
                CreateItemContextMenu();
                evt.StopPropagation();
            });

            return container;
        }
        
        public void SetTypes(params Type[] viewTypes)
        {
            _viewTypes.Clear();
            _viewTypes.AddRange(viewTypes);
            ScheduleRebuild();
        }

        private void CreateVisualElement(VisualElement element, int index)
        {
            var data = GetItemDataForIndex<IView>(index);
            var container = element;
            var icon = container[0];
            var label = (Label)container[1];
            label.userData = data;
            
            bool isTable = data is Table;
            label.text = data.Name;
            
            if (isTable)
            {
                icon.style.backgroundColor = new Color(0.357f, 0.608f, 0.835f);
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
            }
            else
            {
                icon.style.backgroundColor = new Color(0.439f, 0.678f, 0.278f);
                label.style.unityFontStyleAndWeight = FontStyle.Normal;
            }
            
            label.tooltip = $"{(isTable ? "Table" : "View")}: {data.Name}\nID: {data.ID}";
            
            _itemActions[data.ID] = () =>
            {
                bool table = data is Table;
                label.text = data.Name;
                if (table)
                {
                    icon.style.backgroundColor = new Color(0.357f, 0.608f, 0.835f);
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                }
                else
                {
                    icon.style.backgroundColor = new Color(0.439f, 0.678f, 0.278f);
                    label.style.unityFontStyleAndWeight = FontStyle.Normal;
                }
                label.tooltip = $"{(table ? "Table" : "View")}: {data.Name}\nID: {data.ID}";
            };
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
            _flatViews.Clear();
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
            var itemId = _itemIds;
            _indexMapping[container.View.ID] = itemId;
            _flatViews.Add(container.View);
            _itemIds++;

            var children = new List<TreeViewItemData<IView>>();
            foreach (var child in container.Children)
            {
                var childItem = TraverseContainer(child);
                children.Add(childItem);
            }
            return new TreeViewItemData<IView>(itemId, container.View, children);
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
        
        private void CreateBackgroundContextMenu(ContextClickEvent evt)
        {
            if (evt.target != this)
                return;

            if (_database == null)
                return;

            _view = null;
            SetSelectionWithoutNotify(System.Array.Empty<int>());
            OnViewSelected?.Invoke(null);

            var menu = new GenericMenu();

            if (_viewTypes.Contains(typeof(Table)))
            {
                menu.AddItem(new GUIContent("Add/Table"), false, () => _editorUseCase.AddTable());
            }

            if (_viewTypes.Contains(typeof(View)))
            {
                menu.AddItem(new GUIContent("Add/View"), false, () => _editorUseCase.AddView());
            }

            menu.ShowAsContext();
            evt.StopPropagation();
        }

        private void CreateItemContextMenu()
        {
            var menu = new GenericMenu();

            if (_viewTypes.Contains(typeof(Table)))
            {
                menu.AddItem(new GUIContent("Add/Table"), false, () => _editorUseCase.AddTable());
            }

            if (_viewTypes.Contains(typeof(View)))
            {
                menu.AddItem(new GUIContent("Add/View"), false, () => _editorUseCase.AddView());
            }

            menu.AddSeparator(string.Empty);
            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                if (_view == null)
                    return;
                if (_view is Table table)
                    _editorUseCase.RemoveTable(table.ID);
                else if (_view is View view)
                    _editorUseCase.RemoveView(view.ID);
                _view = null;
                OnViewSelected?.Invoke(null);
            });

            if (_view is Table contextTable)
            {
                var capturedTableId = contextTable.ID;
                menu.AddSeparator(string.Empty);
                ODDBImportExportMenu.AppendTableContextMenu(menu, _editorUseCase, capturedTableId);
            }

            if (_view != null)
            {
                var capturedViewId = _view.ID.ToString();
                if (!(_view is Table))
                    menu.AddSeparator(string.Empty);
                menu.AddItem(new GUIContent("Generate Code"), false, () =>
                    ODDBCodeGenMenu.RunGenerateSelection(new[] { capturedViewId }));
            }

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

        private void OnItemIndexChanged(int oldIndex, int newIndex)
        {
            if (oldIndex < 0 || oldIndex >= _flatViews.Count || newIndex < 0 || newIndex >= _flatViews.Count)
            {
                ScheduleRebuild();
                return;
            }

            var moved = _flatViews[oldIndex];
            var destination = _flatViews[newIndex];
            if (moved == null || destination == null)
            {
                ScheduleRebuild();
                return;
            }

            if (moved.GetType() != destination.GetType())
            {
                ScheduleRebuild();
                return;
            }

            if (!HasSameParent(moved, destination))
            {
                ScheduleRebuild();
                return;
            }

            var oldSiblingIndex = CountPreviousSiblings(oldIndex, moved);
            var newSiblingIndex = CountPreviousSiblings(newIndex, moved);
            _editorUseCase.MoveViewItem(moved.ID, oldSiblingIndex, newSiblingIndex);
            ScheduleRebuild();
        }

        private static bool HasSameParent(IView first, IView second)
        {
            var firstParentId = first.ParentView?.ID.ToString();
            var secondParentId = second.ParentView?.ID.ToString();
            return string.Equals(firstParentId, secondParentId);
        }

        private int CountPreviousSiblings(int beforeFlatIndex, IView moved)
        {
            var count = 0;
            for (var i = 0; i < beforeFlatIndex && i < _flatViews.Count; i++)
            {
                var candidate = _flatViews[i];
                if (candidate == null)
                    continue;
                if (candidate.GetType() == moved.GetType() && HasSameParent(candidate, moved))
                    count++;
            }
            return count;
        }
        
        private void UpdateView(string viewId)
        {
            if (_database == null)
                return;
            var view = _database.GetView(new ODDBID(viewId));
            if (view != null && _indexMapping.ContainsKey(view.ID) && _itemActions.ContainsKey(viewId))
                _itemActions[viewId].Invoke();
            ScheduleRebuild();
        }

        private void ScheduleRebuild()
        {
            IsDirty = true;
            Update();
        }
    }
}
