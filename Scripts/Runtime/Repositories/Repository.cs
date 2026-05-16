using TeamODD.ODDB.Runtime;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils.Converters;

namespace TeamODD.ODDB.Runtime
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