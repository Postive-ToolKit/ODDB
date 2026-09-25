using System;
using System.Collections.Generic;
using TeamODD.ODDB.Editors.Commands;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Interfaces;

namespace TeamODD.ODDB.Editors.Window
{
    // Standalone build surface used by the existing ODDB operation handlers.
    // Keep this subset aligned with Scripts/Editor/Database/IODDBEditorUseCase.cs.
    public interface IODDBEditorUseCase
    {
        IEnumerable<IView> GetViews(Predicate<IView> predicate = null);
        IView GetViewByKey(string key);
        ODDBViewType GetViewTypeByKey(string key);
        void AddTable();
        void AddView();
        void RemoveTable(string tableId);
        void RemoveView(string viewId);
        void AddRow(string tableId);
        void RemoveRow(string tableId, string rowId);
        void SetRowId(string tableId, string rowId, string newRowId);
        Row GetRow(string rowId);
        void SetCellData(string tableId, string rowId, int fieldIndex, object value, bool direct = false);
        void AddField(string viewId, Field field);
        void RemoveField(string viewId, int index);
        void MoveField(string viewId, int oldIndex, int newIndex);
        void SetFieldType(string viewId, int fieldIndex, string typeKey, string param);
        void SetViewName(string viewId, string name);
        void SetViewId(string viewId, string newViewId);
        void SetViewBindType(string viewId, Type type);
        void SetViewParent(string viewId, string parentId);
        IEnumerable<Table> GetInheritedTables(string viewId);
        IEnumerable<ICommand> GetUndoHistory();
        IEnumerable<ICommand> GetRedoHistory();
    }
}
