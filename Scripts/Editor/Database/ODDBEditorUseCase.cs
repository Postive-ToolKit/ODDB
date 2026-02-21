using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Settings;
using TeamODD.ODDB.Runtime.Utils.Converters;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.Window
{
    /// <summary>
    /// The core use case class for the ODDB Editor. 
    /// Handles data access, manipulation, and command history (Undo/Redo).
    /// Acts as a bridge between the UI and the Data Logic.
    /// </summary>
    public class ODDBEditorUseCase : IODDBEditorUseCase
    {
        public IODDatabase DataBase => _database;
        public ODDBDataService Service => _dataService;
        
        /// <summary>
        /// Triggered when a specific view data is changed or removed.
        /// </summary>
        public event Action<string> OnViewChanged;
        
        /// <summary>
        /// Triggered when the undo/redo history changes.
        /// </summary>
        public event Action OnHistoryChanged;
        
        private ODDatabase _database;
        private readonly ODDBDataService _dataService;
        private readonly Commands.CommandProcessor _commandProcessor = new();

        public ODDBEditorUseCase() 
        {
            _commandProcessor.MaxHistoryCount = ODDBSettings.Setting.MaxHistoryCount;
            _commandProcessor.OnHistoryChanged += () => OnHistoryChanged?.Invoke();
            _dataService = new ODDBDataService();
            if(ODDBSettings.Setting.IsInitialized == false) 
            {
                var pathSelector = new ODDBPathUtility();
                var pickedPath = pathSelector.GetPath(ODDBSettings.BASE_PATH,ODDBSettings.BASE_PATH);
                if (!string.IsNullOrEmpty(pickedPath))
                    ODDBSettings.Setting.Path = pickedPath;
                else
                {
                    ODDBSettings.Setting.Path = ODDBSettings.BASE_PATH;
                    Debug.LogWarning("Path selection was canceled. Using default path: " + ODDBSettings.BASE_PATH);
                }
            }
            
            var fullPath = Path.Combine(ODDBSettings.Setting.Path, ODDBSettings.Setting.DBName);
            
            if (File.Exists(fullPath) == false)
            {
                Debug.Log($"Creating new database file: {fullPath}");
                _database = new ODDatabase();
                if (_dataService.SaveDatabase(_database, fullPath))
                    AssetDatabase.Refresh();
            }
            else
            {
                if (!_dataService.LoadDatabase(fullPath, out _database))
                {
                    Debug.LogError("Failed to load database");
                    return;
                }
            }
            
            _database.OnDataChanged += OnDataChanged;
            _database.OnDataRemoved += OnDataChanged;
        }
        
        private void OnDataChanged(ODDBID id)
        {
            OnViewChanged?.Invoke(id.ToString());
        }

        public IView GetViewByKey(string id)
        {
            if (_database == null)
                return null;
            var view = _database.GetView(new ODDBID(id));
            return view;
        }

        public IEnumerable<IView> GetViews(Predicate<IView> predicate = null)
        {
            if (_database == null)
                return null;
            if (predicate == null)
                return _database.GetAll();
            var query = _database.GetAll().Where(x => predicate(x));
            return query;
        }

        public ODDBViewType GetViewTypeByKey(string id)
        {
            var view = GetViewByKey(id);
            if (view is Table)
                return ODDBViewType.Table;
            if (view is View)
                return ODDBViewType.View;
            return ODDBViewType.None;
        }

        public string GetViewName(string id)
        {
            return GetViewByKey(id)?.Name ?? string.Empty;
        }

        /// <summary>
        /// Executes a command to change the name of a View or Table.
        /// </summary>
        public void SetViewName(string id, string name)
        {
            var view = GetViewByKey(id);
            if (view == null) return;
            
            var command = new TeamODD.ODDB.Editors.Commands.SetViewNameCommand(view, name, (viewId) => _database.NotifyDataChanged(new ODDBID(viewId)));
            _commandProcessor.Execute(command);
        }

        /// <summary>
        /// Executes a command to add a new row to a Table.
        /// </summary>
        public void AddRow(string tableId)
        {
            var view = GetViewByKey(tableId);
            if (view is not Table table) return;

            var command = new TeamODD.ODDB.Editors.Commands.AddRowCommand(table, (id) => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        /// <summary>
        /// Executes a command to remove a row from a Table.
        /// </summary>
        public void RemoveRow(string tableId, string rowId)
        {
            var view = GetViewByKey(tableId);
            if (view is not Table table) return;

            var command = new TeamODD.ODDB.Editors.Commands.RemoveRowCommand(table, rowId, (id) => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        /// <summary>
        /// Executes a command to add a new field to a View.
        /// </summary>
        public void AddField(string viewId, Field field)
        {
            var view = GetViewByKey(viewId);
            if (view == null) return;

            var command = new TeamODD.ODDB.Editors.Commands.AddFieldCommand(view, field, (id) => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        /// <summary>
        /// Executes a command to remove a field from a View.
        /// </summary>
        public void RemoveField(string viewId, int index)
        {
            var view = GetViewByKey(viewId);
            if (view == null) return;

            var command = new TeamODD.ODDB.Editors.Commands.RemoveFieldCommand(view, index, (id) => _database.NotifyDataChanged(new ODDBID(id)));
            _commandProcessor.Execute(command);
        }

        public Type GetViewBindType(string id)
        {
            var view = GetViewByKey(id);
            if (view == null)
                return null;
            return view.BindType;
        }

        /// <summary>
        /// Executes a command to change the bind type of a View.
        /// </summary>
        public void SetViewBindType(string id, Type type)
        {
            var view = GetViewByKey(id);
            if (view == null) return;

            var command = new TeamODD.ODDB.Editors.Commands.SetBindTypeCommand(view, type, (viewId) => _database.NotifyDataChanged(new ODDBID(viewId)));
            _commandProcessor.Execute(command);
        }

        public IView GetViewParent(string id)
        {
            var view = GetViewByKey(id);
            if (view == null)
                return null;
            return view.ParentView;
        }

        /// <summary>
        /// Executes a command to change the parent view of a View.
        /// </summary>
        public void SetViewParent(string id, string parentKey)
        {
            var view = GetViewByKey(id);
            if (view == null) return;
            var parent = GetViewByKey(parentKey);
            
            if (parent == null && !string.IsNullOrEmpty(parentKey)) return; 

            var command = new TeamODD.ODDB.Editors.Commands.SetParentCommand(view, parent, (viewId) => _database.NotifyDataChanged(new ODDBID(viewId)));
            _commandProcessor.Execute(command);
        }

        public void NotifyViewDataChanged(string viewId)
        {
            _database.NotifyDataChanged(new ODDBID(viewId));
        }

        public IEnumerable<IView> GetPureViews()
        {
            return _database.Views.GetAll();
        }

        public IEnumerable<Row> GetViewRows(string viewId)
        {
            var tableList = GetInheritedTables(viewId).ToList();
            var rows = new List<Row>();
            foreach (var table in tableList)
                rows.AddRange(table.Rows);
            return rows;
        }
        
        public IEnumerable<Table> GetInheritedTables(string viewId)
        {
            var view = GetViewByKey(viewId);
            if (view == null)
                return Enumerable.Empty<Table>();

            var children = _database.GetAll()
                .Where(v => v.ParentView != null && v.ParentView.ID == view.ID);
            var tables = new List<Table>();
            
            if (view is Table tableView)
                tables.Add(tableView);
            
            foreach (var child in children)
            {
                if (child is Table table)
                    tables.Add(table);
                tables.AddRange(GetInheritedTables(child.ID.ToString()));
            }
            return tables;
        }

        public Row GetRow(string rowId)
        {
            foreach (var view in _database.Tables.GetAll())
            {
                var table = view as Table;
                var row = table.GetRow(rowId);
                if (row != null)
                    return row;
            }
            return null;
        }

        public bool TryGetRow(string viewId, string rowId, out Row row)
        {
            row = null;
            var getViewRows = GetViewRows(viewId).ToList();
            if (getViewRows.Count == 0)
                return false;
            row = getViewRows.FirstOrDefault(r => r.ID.ToString() == rowId);
            return row != null;
        }

        public void SaveDatabase(string fullPath)
        {
            _dataService.SaveDatabase(_database, fullPath);
        }

        public void Undo() => _commandProcessor.Undo();
        public void Redo() => _commandProcessor.Redo();
        
        public IEnumerable<TeamODD.ODDB.Editors.Commands.ICommand> GetUndoHistory() => _commandProcessor.GetUndoList();
        public IEnumerable<TeamODD.ODDB.Editors.Commands.ICommand> GetRedoHistory() => _commandProcessor.GetRedoList();
        public void JumpToHistory(TeamODD.ODDB.Editors.Commands.ICommand command) => _commandProcessor.JumpTo(command);

        public void Dispose()
        {
            _commandProcessor.OnHistoryChanged -= () => OnHistoryChanged?.Invoke();
            
            _database.OnDataChanged -= OnDataChanged;
            _database.OnDataRemoved -= OnDataChanged;
            _database = null;
            OnViewChanged = null;
            OnHistoryChanged = null;
            _commandProcessor.Clear();
        }
    }
}