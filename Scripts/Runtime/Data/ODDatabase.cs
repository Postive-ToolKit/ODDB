using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime.Data
{
    public class ODDatabase
    {
        private readonly Dictionary<string, ODDBView> _viewDict = new Dictionary<string, ODDBView>();
        public int Count => _tables.Count + _views.Count;
        public IEnumerable<ODDBView> Views => _views;
        public IEnumerable<ODDBTable> Tables => _tables;
        private List<ODDBTable> _tables = new List<ODDBTable>();
        private List<ODDBView> _views = new List<ODDBView>();
        
        public ODDBTable CreateTable()
        {
            var newTable = new ODDBTable();
            var newid = new ODDBID().ID;
            //check if newid is unique
            while (
                _tables.Exists(x => x.Key == newid)&&
                _views.Exists(x => x.Key == newid))
            {
                newid = new ODDBID().ID;
            }
            newTable.Key = newid;
            newTable.Name = "Table " + newid;
            AddTable(newTable);
            return newTable;
        }

        public ODDBView CreateView()
        {
            var newView = new ODDBView();
            var newid = new ODDBID().ID;
            //check if newid is unique
            while (
                _tables.Exists(x => x.Key == newid) &&
                _views.Exists(x => x.Key == newid))
            {
                newid = new ODDBID().ID;
            }
            newView.Key = newid;
            newView.Name = "View " + newid;
            AddView(newView);
            return newView;
        }
        

        
        public void AddTable(ODDBTable table)
        {
            _tables.Add(table);
            _viewDict.Add(table.Key, table);
        }
        
        public void RemoveTable(ODDBTable table)
        {
            _tables.Remove(table);
            _viewDict.Remove(table.Key);
        }
        
        public void AddView(ODDBView view)
        {
            _views.Add(view);
            _viewDict.Add(view.Key, view);
        }
        public void RemoveView(ODDBView view)
        {
            _views.Remove(view);
            _viewDict.Remove(view.Key);
        }
        
        public ODDBView GetViewByKey(string key)
        {
            if (_viewDict.TryGetValue(key, out var view)) {
                return view;
            }
            return null;
        }
        
        public IEnumerable<ODDBView> GetViews()
        {
            return _viewDict.Values;
        }
    }
}