using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils;

namespace Plugins.ODDB.Scripts.Runtime.Data.Repositories
{
    public class Repository<T> : RepositoryBase<T> where T : IHasODDBID, new()
    {
        protected override T CreateInternal(ODDBID id = null)
        {
            var instance = new T();
            instance.ID = id;
            return instance;
        }
    }
}