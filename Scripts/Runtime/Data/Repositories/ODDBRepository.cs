﻿using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using TeamODD.ODDB.Runtime.Utils;

namespace Plugins.ODDB.Scripts.Runtime.Data.Repositories
{
    public class ODDBRepository<T> : ODDBRepositoryBase<T> where T : IODDBHasUniqueKey, IODDBSerialize, new()
    {
        protected override T CreateInternal(ODDBID id = null)
        {
            var instance = new T();
            instance.Key = id;
            return instance;
        }
    }
}