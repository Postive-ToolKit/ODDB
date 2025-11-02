using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Enums;
using TeamODD.ODDB.Runtime.Interfaces;

namespace TeamODD.ODDB.Editors.Window
{
    public interface IODDBEditorUseCase : IDisposable
    {
        public event Action<string> OnViewChanged;
        public IODDatabase DataBase { get; }
        public IView GetViewByKey(string key);
        public IEnumerable<IView> GetViews(Predicate<IView> predicate = null);
        public ODDBViewType GetViewTypeByKey(string key);
        public string GetViewName(string key);
        public void SetViewName(string key, string name);
        public Type GetViewBindType(string key);
        public void SetViewBindType(string key, Type type);
        public IView GetViewParent(string key);
        public void SetViewParent(string key, string parentKey);
        public void NotifyViewDataChanged(string viewId);
        public IEnumerable<IView> GetPureViews();
        
        /// <summary>
        /// Get view rows as a string representation.
        /// if target view is table return all rows in the table,
        /// if target view is view return all rows in child tables.
        /// </summary>
        /// <param name="viewId"> The ID of the view.</param>
        /// <returns> A string representing the rows of the view.</returns>
        public IEnumerable<Row> GetViewRows(string viewId);

        /// <summary>
        /// Find and return a Row object by its string identifier from the database.
        /// </summary>
        /// <param name="rowId"> The string identifier of the row.</param>
        /// <returns> The Row object corresponding to the given identifier.</returns>
        public Row GetRow(string rowId);
        
        /// <summary>
        /// Try to get a Row object by its string identifier from the specified view in the database.
        /// </summary>
        /// <param name="viewId"> The ID of the view.</param>
        /// <param name="rowId"> The string identifier of the row.</param>
        /// <param name="row"> The output Row object if found; otherwise, null.</param>
        /// <returns> True if the row is found; otherwise, false.</returns>
        public bool TryGetRow(string viewId, string rowId, out Row row);
        
        /// <summary>
        /// Save the given database to the specified file path.
        /// </summary>
        /// <param name="fullPath"> The full file path where the database should be saved.</param>
        public void SaveDatabase(string fullPath);
    }
}