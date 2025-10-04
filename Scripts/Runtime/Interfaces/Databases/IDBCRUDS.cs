using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime.Interfaces
{
    public interface IDBCRUDS<T>
    {
        public int Count { get; }
        T Create(ODDBID id = null);
        T Read(ODDBID id);
        T Read(int index);
        void Update(ODDBID id, T item);
        void Update(int index, T item);
        void Delete(ODDBID id);
        void Delete(int index);
        void Swap(int first, int second);
    }
}