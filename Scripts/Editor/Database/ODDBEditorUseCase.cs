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
    public class ODDBEditorUseCase : IODDBEditorUseCase
    {
        public IODDatabase DataBase => _database;
        public ODDBDataService Service => _dataService;
        public event Action<string> OnViewChanged;
        private ODDatabase _database;
        private readonly ODDBDataService _dataService;
        public ODDBEditorUseCase() 
        {
            
            _dataService = new ODDBDataService();
            if(ODDBSettings.Setting.IsInitialized == false) 
            {
                var pathSelector = new ODDBPathUtility();
                ODDBSettings.Setting.Path = pathSelector.GetPath(ODDBSettings.BASE_PATH,ODDBSettings.BASE_PATH);
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

        public void SetViewName(string id, string name)
        {
            var view = GetViewByKey(id);
            if (view == null)
                return;
            view.Name = name;
            _database.NotifyDataChanged(view.ID);
        }

        public Type GetViewBindType(string id)
        {
            var view = GetViewByKey(id);
            if (view == null)
                return null;
            return view.BindType;
        }

        public void SetViewBindType(string id, Type type)
        {
            var view = GetViewByKey(id);
            if (view == null)
                return;
            view.BindType = type;
            _database.NotifyDataChanged(view.ID);
        }

        public IView GetViewParent(string id)
        {
            var view = GetViewByKey(id);
            if (view == null)
                return null;
            return view.ParentView;
        }

        public void SetViewParent(string id, string parentKey)
        {
            var view = GetViewByKey(id);
            if (view == null)
                return;
            var parent = GetViewByKey(parentKey);
            if (parent == null)
                return;
            view.ParentView = parent;
            
            if(view.BindType != null && view.BindType.IsSubclassOf(parent.BindType))
                view.BindType = parent.BindType;
            _database.NotifyDataChanged(view.ID);
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
            var view = GetViewByKey(viewId);
            if (view == null)
                return Enumerable.Empty<Row>();

            if (view is Table table)
                return table.Rows;
            
            var children = _database.GetAll()
                .Where(v => v.ParentView != null && v.ParentView.ID == view.ID);
            var rows = new List<Row>();
            foreach (var child in children)
                rows.AddRange(GetViewRows(child.ID.ToString()));
            return rows;
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

        public void Dispose()
        {
            _database.OnDataChanged -= OnDataChanged;
            _database = null;
            OnViewChanged = null;
        }
    }
}