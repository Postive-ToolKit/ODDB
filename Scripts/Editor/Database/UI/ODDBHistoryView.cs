using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Editors.Commands;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public class ODDBHistoryView : VisualElement
    {
        private readonly IODDBEditorUseCase _editorUseCase;
        private readonly ListView _listView;
        private List<ICommand> _allCommands = new List<ICommand>();

        public ODDBHistoryView()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            _editorUseCase.OnHistoryChanged += Refresh;

            style.flexGrow = 1;
            style.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            style.borderTopWidth = 1;
            style.borderTopColor = Color.black;

            var title = new Label("History")
            {
                style = {
                    unityFontStyleAndWeight = FontStyle.Bold,
                    paddingLeft = 5,
                    paddingTop = 2,
                    paddingBottom = 2,
                    backgroundColor = new Color(0.25f, 0.25f, 0.25f, 1f)
                }
            };
            Add(title);

            _listView = new ListView
            {
                itemHeight = 20,
                makeItem = () => new Label { style = { unityTextAlign = TextAnchor.MiddleLeft, paddingLeft = 5 } },
                bindItem = BindItem,
                selectionType = SelectionType.Single,
                style = { flexGrow = 1 }
            };

            // Double click to jump? Or single click?
            // Single click is better for quick navigation.
            // selectionChanged is triggered on click.
            _listView.selectionChanged += OnSelectionChanged;

            Add(_listView);
            Refresh();
        }

        private void BindItem(VisualElement element, int index)
        {
            var label = (Label)element;
            var command = _allCommands[index];
            
            var undoStack = _editorUseCase.GetUndoHistory().ToList();
            bool isExecuted = undoStack.Contains(command);

            var timeStr = command.ExecutionTime.ToString("HH:mm:ss");
            label.text = $"[{timeStr}] {command.Name}";
            
            if (isExecuted)
            {
                label.style.color = new Color(0.8f, 0.8f, 0.8f);
                if (undoStack.FirstOrDefault() == command)
                {
                    label.text = $"▶ [{timeStr}] {command.Name}";
                    label.style.unityFontStyleAndWeight = FontStyle.Bold;
                }
                else
                {
                    label.style.unityFontStyleAndWeight = FontStyle.Normal;
                }
            }
            else
            {
                label.style.color = new Color(0.5f, 0.5f, 0.5f);
                label.style.unityFontStyleAndWeight = FontStyle.Normal;
            }
        }

        private void OnSelectionChanged(IEnumerable<object> selectedItems)
        {
            var selected = selectedItems.FirstOrDefault() as ICommand;
            if (selected != null)
            {
                _editorUseCase.JumpToHistory(selected);
            }
        }

        private void Refresh()
        {
            _allCommands.Clear();
            
            var undoList = _editorUseCase.GetUndoHistory().ToList(); // [Newest ... Oldest]
            var redoList = _editorUseCase.GetRedoHistory().ToList(); // [Nearest Future ... Farthest Future]
            
            redoList.Reverse(); // [Farthest Future ... Nearest Future]
            
            _allCommands.AddRange(redoList);
            _allCommands.AddRange(undoList);
            
            _listView.itemsSource = _allCommands;
            _listView.Rebuild();
            
            var lastExecuted = _editorUseCase.GetUndoHistory().FirstOrDefault();
            if (lastExecuted != null)
            {
                var index = _allCommands.IndexOf(lastExecuted);
                _listView.ScrollToItem(index);
            }
        }
        
        // Destructor or Dispose needed to unsubscribe?
        // VisualElement doesn't have OnDestroy.
        // Need to handle detaching from panel.
        // But for now, UseCase lives with Window, and View lives with Window.
    }
}