using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data.Interfaces;

namespace TeamODD.ODDB.Editors.Window
{
    public interface IODDBEditorUseCase : IDisposable
    {
        event Action<string> OnViewChanged;
        IODDBView GetViewByKey(string key);
        IEnumerable<IODDBView> GetViews();
        ODDBViewType GetViewTypeByKey(string key);
        string GetViewName(string key);
        void SetViewName(string key, string name);
        Type GetViewBindType(string key);
        void SetViewBindType(string key, Type type);
        IODDBView GetViewParent(string key);
        void SetViewParent(string key, string parentKey);
        void NotifyViewDataChanged(string viewId);
        IEnumerable<IODDBView> GetPureViews();
    }
}