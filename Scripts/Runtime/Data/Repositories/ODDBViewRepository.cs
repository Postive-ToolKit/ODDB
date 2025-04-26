using TeamODD.ODDB.Runtime.Data.Interfaces;
using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime.Data
{
    public class ODDBViewRepository<T> : ODDBRepositoryBase<IODDBView> where T : IODDBView ,new()
    {
        public string Key { get; set; }
        protected override IODDBView CreateInternal(ODDBID id = null)
        {
            var view = new T();
            if (id == null)
                id = new ODDBID();
            view.Key = id;
            return view;
        }
    }
}