using System;
using System.Collections.Generic;
using TeamODD.ODDB.Editors.UI.Interfaces;
using UnityEngine.UIElements;
using TeamODD.ODDB.Runtime.Settings.Data;
using TeamODD.ODDB.Scripts.Runtime.Data;
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
                if(item is ODDBTable table)
                    OnTableSelected?.Invoke(table);
            }
        }
    }
}
