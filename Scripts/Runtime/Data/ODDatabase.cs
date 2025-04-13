using System.Collections.Generic;
using TeamODD.ODDB.Scripts.Runtime.Data;

namespace TeamODD.ODDB.Runtime.Settings.Data
{
    public class ODDatabase
    {
        public List<ODDBTable> Tables = new List<ODDBTable>();
        
        public ODDBTable CreateTable()
        {
            var newTable = new ODDBTable();
            var newid = new ODDBID().ID;
            //check if newid is unique
            while (Tables.Exists(x => x.Key == newid))
            {
                newid = new ODDBID().ID;
            }
            newTable.Key = newid;
            Tables.Add(newTable);
            return newTable;
        }
    }
}