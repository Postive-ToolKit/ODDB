using UnityEngine.UIElements;
using TeamODD.ODDB.Scripts.Runtime.Data;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using TeamODD.ODDB.Editors.UI.Fields;

namespace TeamODD.ODDB.Editors.UI
{
#if UNITY_2022_2_OR_NEWER
    [UxmlElement]
    public partial class ODDBMultiColumnListView : MultiColumnListView
#else
    public class ODDBMultiColumnListView : MultiColumnListView
#endif
    {
#if !UNITY_2022_2_OR_NEWER
        public new class UxmlFactory : UxmlFactory<ODDBMultiColumnListView, MultiColumnListView.UxmlTraits> { }
#endif

        private ODDBTable _table;
        private List<string> _columnNames = new List<string>();

        public ODDBMultiColumnListView()
        {
            selectionType = SelectionType.Multiple;
            showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            showBorder = true;
            horizontalScrollingEnabled = true;

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
        }

        public void SetTable(ODDBTable table)
        {
            _table = table;
            RefreshColumns();
            RefreshItems();
        }

        private void RefreshColumns()
        {
            if (_table == null) return;

            columns.Clear();
            _columnNames.Clear();

            for (int i = 0; i < _table.TableMetas.Count; i++)
            {
                var meta = _table.TableMetas[i];
                var columnName = meta.Name;
                _columnNames.Add(columnName);

                var column = new Column()
                {
                    title = columnName,
                    name = columnName,
                    width = 150,
                    minWidth = 50
                };

                var dataType = meta.DataType;
                var columnIndex = i;

                column.makeCell = () =>
                {
                    var container = new VisualElement();
                    container.style.flexGrow = 1;
                    var field = ODDBFieldFactory.CreateField(dataType);
                    container.Add(field.Root);
                    container.userData = field;
                    return container;
                };

                column.bindCell = (element, index) =>
                {
                    var container = element as VisualElement;
                    var field = container.userData as IODDBField;
                    
                    if (field != null)
                    {
                        var value = _table.GetValue(index, columnIndex);
                        field.SetValue(value);

                        field.RegisterValueChangedCallback(newValue =>
                        {
                            if (_table != null && index < _table.ReadOnlyRows.Count)
                            {
                                var row = _table.ReadOnlyRows[index];
                                // TODO: 값 변경 처리 - 테이블 데이터 업데이트 메서드 필요
                                Debug.Log($"Value changed at [{index}, {columnIndex}]: {newValue}");
                            }
                        });
                    }
                };

                columns.Add(column);
            }
        }

        private void RefreshItems()
        {
            if (_table == null) return;
            
            itemsSource = new List<int>();
            for (int i = 0; i < _table.ReadOnlyRows.Count; i++)
            {
                (itemsSource as List<int>).Add(i);
            }
            Rebuild();
        }

        private void OnGeometryChanged(GeometryChangedEvent evt)
        {
            if (columns.Count > 0)
            {
                float totalWidth = evt.newRect.width;
                float columnWidth = totalWidth / columns.Count;
                
                foreach (var column in columns)
                {
                    column.width = new Length(Mathf.Max(columnWidth, column.minWidth.value));
                }
            }
        }
    }
}
