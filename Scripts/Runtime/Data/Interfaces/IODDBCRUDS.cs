using System.Collections.Generic;

namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBCRUDS<T>
    {
        public int Count { get; }
        T Create(string id = null);
        T Read(string id);
        T Read(int index);
        void Update(string id, T item);
        void Update(int index, T item);
        void Delete(string id);
        void Delete(int index);
        void Swap(int first, int second);
        IReadOnlyList<T> GetAll();
    }
}