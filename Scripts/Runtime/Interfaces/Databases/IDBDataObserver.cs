using System;
using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime.Interfaces
{
    public interface IDBDataObserver
    {
        public event Action<ODDBID> OnDataChanged;
        public event Action<ODDBID> OnDataRemoved; 
    }
}