using UnityEngine.UIElements;
using TeamODD.ODDB.Scripts.Runtime.Data;
using System.Collections.Generic;
using Plugins.ODDB.Scripts.Runtime.Data.Enum;
using UnityEditor;
using UnityEngine;
using TeamODD.ODDB.Editors.UI.Fields;
using TeamODD.ODDB.Editors.UI.Interfaces;

namespace TeamODD.ODDB.Editors.UI
{
#if UNITY_2022_2_OR_NEWER
    [UxmlElement]
    public partial class ODDBMultiColumnListView : MultiColumnListView, IODDBUpdateUI
#else
    public class ODDBMultiColumnListView : MultiColumnListView
#endif
    {
#if !UNITY_2022_2_OR_NEWER
        public new class UxmlFactory : UxmlFactory<ODDBMultiColumnListView, MultiColumnListView.UxmlTraits> { }
#endif

        private ODDBTable _table;
        private List<string> _columnNames = new List<string>();
        public bool IsDirty { get; set; }
        private const float DELETE_COLUMN_WIDTH = 30f;

        public ODDBMultiColumnListView()
        {
            selectionType = SelectionType.Single;
            showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            showBorder = true;
            horizontalScrollingEnabled = false;
            //style.flexGrow = 1;
            style.flexShrink = 1;
            

            RegisterCallback<GeometryChangedEvent>(OnGeometryChanged);
            // Add Resize Window Listener
            // add call back on culumn
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

            // 데이터 컬럼 추가
            for (int i = 0; i < _table.TableMetas.Count; i++)
            {
                var meta = _table.TableMetas[i];
                var columnName = meta.Name;
                _columnNames.Add(columnName);

                var column = new Column()
                {
                    title = columnName,
                    name = columnName,
                    maxWidth = 300,  // 기본 너비를 더 크게 설정
                    width = 60,  // 기본 너비를 더 크게 설정
                    minWidth = 60,
                    stretchable = true,
                    resizable = true
                };

                var dataType = meta.DataType;
                var columnIndex = i;

                column.makeCell = () => CreateCell(dataType);
                column.bindCell = (element, index) => BindCell(element, index, columnIndex);

                columns.Add(column);
            }

            // 삭제 버튼 컬럼 추가
            var deleteColumn = new Column()
            {
                title = "",
                name = "DeleteColumn",
                maxWidth = DELETE_COLUMN_WIDTH,
                width = DELETE_COLUMN_WIDTH,
                minWidth = DELETE_COLUMN_WIDTH,
                stretchable = false,
                resizable = false
            };

            deleteColumn.makeCell = () =>
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

            deleteColumn.bindCell = (element, index) =>
            {
                var button = element as Button;
                if (button != null)
                {
                    button.clicked += () =>
                    {
                        if (_table != null && index < _table.ReadOnlyRows.Count)
                        {
                            _table.RemoveRow(index);
                            IsDirty = true;
                        }
                    };
                }
            };

            columns.Add(deleteColumn);
            UpdateMaxWidth();
        }

        private VisualElement CreateCell(ODDBDataType dataType)
        {
            var container = new VisualElement();
            container.style.flexGrow = 1;
            container.style.paddingBottom = 0;
            container.style.paddingTop = 0;
            container.style.paddingLeft = 0;
            container.style.paddingRight = 0;
            
            container.style.marginBottom = 0;
            container.style.marginTop = 0;
            container.style.marginLeft = 0;
            container.style.marginRight = 0;
            container.style.borderBottomLeftRadius = 0;
            container.style.borderBottomRightRadius = 0;
            container.style.borderTopLeftRadius = 0;
            container.style.borderTopRightRadius = 0;

            container.style.borderBottomColor = new StyleColor(Color.black);
            container.style.borderBottomWidth = 1;
            container.style.borderBottomLeftRadius = 0;
            container.style.borderBottomRightRadius = 0;
            container.style.borderTopColor = new StyleColor(Color.black);
            container.style.borderTopWidth = 1;
            container.style.borderTopLeftRadius = 0;
            container.style.borderTopRightRadius = 0;
            container.style.borderLeftColor = new StyleColor(Color.black);
            container.style.borderLeftWidth = 1;
            
            var field = ODDBFieldFactory.CreateField(dataType);
            container.Add(field.Root);
            container.userData = field;
            return container;
        }

        private void BindCell(VisualElement element, int index, int columnIndex)
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
                        row.SetData(columnIndex, newValue.ToString());
                    }
                });
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
                float totalWidth = evt.newRect.width - DELETE_COLUMN_WIDTH;
                Debug.Log($"Total Width: {totalWidth}");
                //float columnWidth = totalWidth / (columns.Count - 1); // 삭제 버튼 컬럼 제외
                
                // // 데이터 컬럼들의 너비 설정
                // for (int i = 0; i < columns.Count - 1; i++)
                // {
                //     columns[i].width = new Length(Mathf.Max(columnWidth, columns[i].minWidth.value));
                // }

                // 삭제 버튼 컬럼 너비 고정
                // columns[^1].width = DELETE_COLUMN_WIDTH;
            }
        }

        public void UpdateMaxWidth(float maxWidth = -1f)
        {
            if (maxWidth > 0)
            {
                style.maxWidth = maxWidth;
                return;
            }
            // 모든 자식의 너비를 합친 값을 계산하고 그 값보다 300 큰 값으로 maxWidth 설정
            Debug.Log("UpdateMaxWidth");
            float totalWidth = 0;
            foreach (var column in columns)
            {
                totalWidth += column.maxWidth.value;
            }

            style.maxWidth = totalWidth;
        }
    }
}
