using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime
{
    public class ViewRepository<T> : RepositoryBase<IView> where T : IView ,new()
    {
        public string Key { get; set; }
        protected override IView CreateInternal(ODDBID id = null)
        {
            var view = new T();
            if (id == null)
                id = new ODDBID();
            view.ID = id;
            return view;
        }
    }
}