﻿using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBHasUniqueKey
    {
        public ODDBID Key { get; set; }
    }
}