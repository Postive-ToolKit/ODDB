using System.Collections.Generic;
using Plugins.ODDB.Scripts.Runtime.Data;
using TeamODD.ODDB.Scripts.Runtime.Data;

namespace TeamODD.ODDB.Runtime.Settings.Data
{
    public class ODDatabase
    {
        public int Count => Tables.Count + Views.Count;
        public List<ODDBTable> Tables = new List<ODDBTable>();
        public List<ODDBView> Views = new List<ODDBView>();
        
        public ODDBTable CreateTable()
        {
            var newTable = new ODDBTable();
            var newid = new ODDBID().ID;
            //check if newid is unique
            while (
                Tables.Exists(x => x.Key == newid)&&
                Views.Exists(x => x.Key == newid))
            {
                newid = new ODDBID().ID;
            }
            newTable.Key = newid;
            Tables.Add(newTable);
            return newTable;
        }

        public ODDBView CreateView()
        {
            var newView = new ODDBView();
            var newid = new ODDBID().ID;
            //check if newid is unique
            while (
                Tables.Exists(x => x.Key == newid) &&
                Views.Exists(x => x.Key == newid))
            {
                newid = new ODDBID().ID;
            }

            newView.Key = newid;
            Views.Add(newView);
            return newView;
        }
        
        public void RemoveTable(ODDBTable table)
        {
            Tables.Remove(table);
        }
        
        public void RemoveView(ODDBView view)
        {
            Views.Remove(view);
        }
    }
}