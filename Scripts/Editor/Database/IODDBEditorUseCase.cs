using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Interfaces;

namespace TeamODD.ODDB.Editors.Window
{
    public interface IODDBEditorUseCase : IDisposable
    {
        event Action<string> OnViewChanged;
        IView GetViewByKey(string key);
        IEnumerable<IView> GetViews(Predicate<IView> predicate = null);
        ODDBViewType GetViewTypeByKey(string key);
        string GetViewName(string key);
        void SetViewName(string key, string name);
        Type GetViewBindType(string key);
        void SetViewBindType(string key, Type type);
        IView GetViewParent(string key);
        void SetViewParent(string key, string parentKey);
        void NotifyViewDataChanged(string viewId);
        IEnumerable<IView> GetPureViews();
    }
}