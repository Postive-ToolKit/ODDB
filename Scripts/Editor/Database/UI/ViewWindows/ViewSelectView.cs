using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI.ViewWindows
{
    public class ViewSelectView : ListView
    {
        public event Action<IView> OnViewSelected; 
        public ViewSelectView()
        {
            style.flexGrow = 1;
            style.flexShrink = 1;
            style.flexBasis = new StyleLength(StyleKeyword.Auto);
            style.width = new StyleLength(StyleKeyword.Auto);
            style.height = new StyleLength(StyleKeyword.Auto);
            style.marginLeft = 0;
            style.marginRight = 0;
            style.marginTop = 0;
            style.marginBottom = 0;

            this.makeItem = () => new Label()
            {
                style =
                {
                    flexGrow = 1,
                    flexShrink = 0,
                    flexBasis = new StyleLength(StyleKeyword.Auto),
                    unityTextAlign = TextAnchor.MiddleLeft,
                    marginLeft = 0,
                    marginRight = 0,
                    marginTop = 0,
                    marginBottom = 0
                }
            };
            this.bindItem = (element, index) =>
            {
                var button = element as Label;
                if (button == null || itemsSource == null || index < 0 || index >= itemsSource.Count)
                    return;

                var item = itemsSource[index] as IView;
                // Not Select selection
                if (item == null)
                {
                    button.text = "None";
                    return;
                }
                button.text = item.Name + " - " + item.ID;
            };
            
            this.selectionType = SelectionType.Single;
            
            this.showBorder = true;
            this.showAlternatingRowBackgrounds = AlternatingRowBackground.All;
            this.fixedItemHeight = 20;
            
            selectionChanged += OnSelectionChanged;
        }

        private void OnSelectionChanged(IEnumerable<object> selections)
        {
            foreach (var item in selectedItems)
            {
                if (item is IView view)
                {
                    OnViewSelected?.Invoke(view);
                    return;
                }
            }
            OnViewSelected?.Invoke(null);
        }

        public void SetDataSource<TView>(IEnumerable<TView> source) where TView : class ,IView
        {
            var newList = new List<TView>();
            newList.Add(null);
            newList.AddRange(source);
            
            this.itemsSource = newList.ToArray();
            this.Rebuild();
        }


        public void SetSelectedView(IView currentView)
        {
            if (currentView == null || itemsSource == null)
            {
                selectedIndex = -1;
                return;
            }
            var items = itemsSource as IView[];
            var itemList = items?.ToList();
            var index = itemList?.FindIndex(view => view != null && view.ID.Equals(currentView.ID)) ?? -1;
            selectedIndex = index;
        }
    }
}