using System.IO;
using TeamODD.ODDB.Editors;
using TeamODD.ODDB.Editors.CodeGen.UI;
using TeamODD.ODDB.Editors.UI;
using TeamODD.ODDB.Editors.UI.Menus;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Settings;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;


namespace TeamODD.ODDB.Editors.Window
{
    public class ODDBEditorWindow : EditorWindow
    {
        private IODDBEditorUseCase _editorUseCase;
        #region Layout
        private TwoPaneSplitView _splitView;
        private ODDBTreeView _tableTreeView;
        private ODDBHistoryView _historyView;
        private ODDBEditorView _editorView;
        #endregion

        [MenuItem(ODDBEditorConst.MENU_ROOT + "ODDB Editor")]
        public static void OpenWindow()
        {
            ODDBEditorWindow wnd = GetWindow<ODDBEditorWindow>();
            wnd.titleContent = new GUIContent("ODDB Editor");
            wnd.minSize = new Vector2(800, 600);
        }

        public void CreateGUI()
        {
            // Use case + DI are now owned by ODDBEditorRuntime so the MCP server
            // and the window share the same instance. Accessing the property here
            // triggers lazy creation and DI registration if this is the first use.
            _editorUseCase = ODDBEditorRuntime.UseCase;
            if (_editorUseCase == null)
            {
                rootVisualElement.Add(new UnityEngine.UIElements.Label(
                    "ODDB failed to initialize. Check the Console for details."));
                return;
            }
            CreateLayout();
            
            _tableTreeView.OnViewSelected += _editorView.SetView;
            _tableTreeView.OnViewSelected += PushSelectionContext;
            
            // bind save key to window not view
            rootVisualElement.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.ctrlKey)
                {
                    if (evt.keyCode == KeyCode.S)
                    {
                        var fullPath = ODDBRuntimeSettings.ResolveDatabasePath();
                        _editorUseCase.SaveDatabase(fullPath);
                    }
                    else if (evt.keyCode == KeyCode.Z)
                    {
                        _editorUseCase.Undo();
                    }
                    else if (evt.keyCode == KeyCode.Y)
                    {
                        _editorUseCase.Redo();
                    }
                }
            });
        }

        private void CreateLayout()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            
            _tableTreeView = new ODDBTreeView(typeof(View), typeof(Table));
            
            var topToolbar = new Toolbar();
            var toolBarMenu = new ToolbarMenu();
            toolBarMenu.text = "All";
            toolBarMenu.tooltip = "Filter the tree to show all items or only Views";
            toolBarMenu.menu.AppendAction("All", action =>
            {
                _tableTreeView.SetTypes(typeof(View), typeof(Table));
                toolBarMenu.text = "All";
            });
            toolBarMenu.menu.AppendAction("Views", action =>
            {
                _tableTreeView.SetTypes(typeof(View));
                toolBarMenu.text = "Views";
            });
            topToolbar.Add(toolBarMenu);

            var selectionLabel = new Label();
            selectionLabel.text = "Selected: None";
            selectionLabel.tooltip = "No View or Table is currently selected";
            selectionLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
            selectionLabel.style.paddingLeft = 6;
            selectionLabel.style.paddingRight = 6;
            selectionLabel.style.flexGrow = 1;
            
            _tableTreeView.OnViewSelected += view =>
            {
                if (string.IsNullOrEmpty(view))
                {
                    selectionLabel.text = "Selected: None";
                    selectionLabel.tooltip = "No View or Table is currently selected";
                }
                else
                {
                    var viewType = _editorUseCase.GetViewTypeByKey(view);
                    var viewName = _editorUseCase.GetViewName(view);
                    selectionLabel.text = $"Selected {viewType}: {viewName}";
                    selectionLabel.tooltip = $"{viewType}: {viewName}\nID: {view}";
                }
            };
            
            topToolbar.Add(selectionLabel);

            var exportMenu = new ToolbarMenu { text = "Export" };
            ODDBImportExportMenu.BuildToolbarExportMenu(exportMenu, _editorUseCase);
            topToolbar.Add(exportMenu);

            var importMenu = new ToolbarMenu { text = "Import" };
            ODDBImportExportMenu.BuildToolbarImportMenu(importMenu, _editorUseCase);
            topToolbar.Add(importMenu);

            var generateCodeMenu = new ToolbarMenu { text = "Generate Code" };
            generateCodeMenu.menu.AppendAction("Generate All",
                _ => ODDBCodeGenMenu.RunGenerateAll());
            generateCodeMenu.menu.AppendAction("Open Generated Folder",
                _ => ODDBCodeGenMenu.OpenGeneratedFolder());
            topToolbar.Add(generateCodeMenu);

            var historyToggle = new ToolbarToggle { text = "History" };
            historyToggle.RegisterValueChangedCallback(evt =>
            {
                _historyView.style.display = evt.newValue ? DisplayStyle.Flex : DisplayStyle.None;
            });
            topToolbar.Add(historyToggle);
            
            rootVisualElement.Add(topToolbar);
            
            _splitView = new TwoPaneSplitView
            {
                style = {
                    flexGrow = 1
                },
                fixedPaneIndex = 0,
                fixedPaneInitialDimension = 250
            };
            var treeViewContainer = new VisualElement() { style = { flexGrow = 1 } };
            
            treeViewContainer.Add(_tableTreeView);
            
            _historyView = new ODDBHistoryView { style = { display = DisplayStyle.None, height = 150 } };
            treeViewContainer.Add(_historyView);

            _splitView.Add(treeViewContainer);
            
            _editorView = new ODDBEditorView();
            _splitView.Add(_editorView);
            
            rootVisualElement.Add(_splitView);
        }

        private void PushSelectionContext(string viewId)
        {
            if (_editorUseCase == null) return;
            var tableId = !string.IsNullOrEmpty(viewId)
                          && _editorUseCase.GetViewTypeByKey(viewId) == ODDBViewType.Table
                ? viewId
                : null;
            _editorUseCase.SetSelectionContext(tableId);
        }

        private void OnDestroy()
        {
            if (_editorUseCase == null || !_editorUseCase.IsDirty)
                return;

            var choice = EditorUtility.DisplayDialogComplex(
                "Save Changes",
                "You have unsaved ODDB changes. Save before closing?",
                "Save",      // 0
                "Discard",   // 1
                "Cancel");   // 2

            if (choice == 2)
            {
                // Reopen the window since OnDestroy can't be canceled.
                EditorApplication.delayCall += () => GetWindow<ODDBEditorWindow>();
                return;
            }
            if (choice == 0)
            {
                var fullPath = ODDBRuntimeSettings.ResolveDatabasePath();
                _editorUseCase.SaveDatabase(fullPath);
            }
            // Discard (choice == 1): do nothing.
            // The use case and DI registrations live in ODDBEditorRuntime now;
            // do NOT dispose them here.
        }
    }
}
