using System;
using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBDataObserver
    {
        public event Action<ODDBID> OnDataChanged;
        public event Action<ODDBID> OnDataRemoved; 
    }
}