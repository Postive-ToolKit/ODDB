using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.Enum;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Editors.Window
{
    public class ODDBEditorUseCase : IODDBEditorUseCase
    {
        public event Action<string> OnViewChanged;
        private ODDatabase _database;
        public ODDBEditorUseCase(ODDatabase database) {
            _database = database;
            _database.OnDataChanged += OnDataChanged;
        }
        
        private void OnDataChanged(ODDBID id)
        {
            OnViewChanged?.Invoke(id.ToString());
        }
            
        public IODDBView GetViewByKey(string id)
        {
            if (_database == null)
                return null;
            var view = _database.GetView(new ODDBID(id));
            return view;
        }

        public IEnumerable<IODDBView> GetViews(Predicate<IODDBView> predicate = null)
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
            if (view is ODDBTable)
                return ODDBViewType.Table;
            if (view is ODDBView)
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
            _database.NotifyDataChanged(view.Key);
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
            _database.NotifyDataChanged(view.Key);
        }

        public IODDBView GetViewParent(string id)
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
            _database.NotifyDataChanged(view.Key);
        }

        public void NotifyViewDataChanged(string viewId)
        {
            _database.NotifyDataChanged(new ODDBID(viewId));
        }

        public IEnumerable<IODDBView> GetPureViews()
        {
            return _database.Views.GetAll();
        }

        public void Dispose()
        {
            _database.OnDataChanged -= OnDataChanged;
            _database = null;
            OnViewChanged = null;
        }
    }
}