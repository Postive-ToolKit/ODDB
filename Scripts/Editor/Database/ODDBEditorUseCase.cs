using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.Enum;
using TeamODD.ODDB.Runtime.Data.Interfaces;

namespace TeamODD.ODDB.Editors.Window
{
    public class ODDBEditorUseCase : IODDBEditorUseCase
    {
        public event Action<string> OnViewChanged;
        private ODDatabase _database;
        public ODDBEditorUseCase(ODDatabase database) {
            _database = database;
        }
            
        public IODDBView GetViewByKey(string key)
        {
            if (_database == null)
                return null;
            var view = _database.GetViewByKey(key);
            return view;
        }

        public IEnumerable<IODDBView> GetViews()
        {
            if (_database == null)
                return null;
            return _database.GetViews();
        }

        public ODDBViewType GetViewTypeByKey(string key)
        {
            var view = GetViewByKey(key);
            if (view is ODDBTable)
                return ODDBViewType.Table;
            if (view is ODDBView)
                return ODDBViewType.View;
            return ODDBViewType.None;
        }

        public string GetViewName(string key)
        {
            return GetViewByKey(key)?.Name ?? string.Empty;
        }

        public void SetViewName(string key, string name)
        {
            var view = GetViewByKey(key);
            if (view == null)
                return;
            view.Name = name;
            OnViewChanged?.Invoke(key);
        }

        public Type GetViewBindType(string key)
        {
            var view = GetViewByKey(key);
            if (view == null)
                return null;
            return view.BindType;
        }

        public void SetViewBindType(string key, Type type)
        {
            var view = GetViewByKey(key);
            if (view == null)
                return;
            view.BindType = type;
            OnViewChanged?.Invoke(key);
        }

        public IODDBView GetViewParent(string key)
        {
            var view = GetViewByKey(key);
            if (view == null)
                return null;
            return view.ParentView;
        }

        public void SetViewParent(string key, string parentKey)
        {
            var view = GetViewByKey(key);
            if (view == null)
                return;
            var parent = GetViewByKey(parentKey);
            if (parent == null)
                return;
            view.ParentView = parent;
            
            if(view.BindType != null && view.BindType.IsSubclassOf(parent.BindType))
                view.BindType = parent.BindType;
            
            OnViewChanged?.Invoke(key);
        }

        public void NotifyViewDataChanged(string viewId)
        {
            OnViewChanged?.Invoke(viewId);
        }

        public IEnumerable<IODDBView> GetPureViews()
        {
            return _database.Views;
        }

        public void Dispose()
        {
            _database = null;
            OnViewChanged = null;
        }
    }
}