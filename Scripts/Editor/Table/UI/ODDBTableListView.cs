using System;
using System.Collections.Generic;
using TeamODD.ODDB.Editors.UI.Interfaces;
using UnityEngine.UIElements;
using TeamODD.ODDB.Runtime.Settings.Data;
using TeamODD.ODDB.Scripts.Runtime.Data;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.UI
{
#if UNITY_2022_2_OR_NEWER
    [UxmlElement]
    public partial class ODDBTableListView : ListView, IODDBUpdateUI
#else
    public class ODDBTableListView : ListView
#endif
    {
#if !UNITY_2022_2_OR_NEWER
        public new class UxmlFactory : UxmlFactory<ODDBTableListView, ListView.UxmlTraits> { }
#endif
        public bool IsDirty { get; set; }
        private ODDatabase _database;
        private ODDBTable _table;
        public event Action<ODDBTable> OnTableSelected;
        public ODDBTableListView()
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
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Add"), false, () =>
                {
                    var newTable = _database.CreateTable();
                    newTable.Name = "New Table";
                    IsDirty = true;
                });
                menu.AddItem(new GUIContent("Remove"), false, () =>
                {
                    if (_table == null)
                        return;
                    if (!_database.Tables.Contains(_table))
                        return;
                    _database.Tables.Remove(_table);
                    _table = null;
                    OnTableSelected?.Invoke(null);
                    IsDirty = true;
                });
                menu.ShowAsContext();
            });
            
            schedule.Execute(Update).Every(100);
        }

        private void CreateVisualElement(VisualElement element, int index)
        {
            var label = (Label)element;
            if (_database != null && index < _database.Tables.Count)
            {
                label.text = _database.Tables[index].Name;
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
            var items = new List<ODDBTable>();
            if (_database == null)
                return;
            
            foreach (var table in _database.Tables)
                items.Add(table);
            
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
                if (item is ODDBTable table)
                {
                    _table = table;
                    OnTableSelected?.Invoke(table);
                    return;
                }
                    
            }
        }

        public void UpdateTable(ODDBTable table)
        {
            // find table index and update it
            if (_database == null)
                return;
            var index = _database.Tables.IndexOf(table);
            this.RefreshItem(index);
        }
    }
}
