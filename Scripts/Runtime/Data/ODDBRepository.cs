using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime.Data
{
    public abstract class ODDBRepository<T> : IODDBRepository<T> where T : IODDBHasUniqueKey, IODDBSerialize
    {
        private readonly Dictionary<string, T> _dictionary = new Dictionary<string, T>();
        private readonly List<T> _list = new List<T>();
        public int Count => _dictionary.Count;

        public T Create(string id = null)
        {
            var uniqueID = id ?? new ODDBID().ID;
            var item = CreateInternal(uniqueID);
            _dictionary.Add(uniqueID, item);
            _list.Add(item);
            return item;
        }
        
        protected abstract T CreateInternal(string id);

        public T Read(string id)
        {
            if (_dictionary.TryGetValue(id, out var view))
                return view;
            return default;
        }

        public T Read(int index) {
            if (index < 0 || index >= _list.Count)
                return default;
            return _list[index];
        }
        
        public void Update(string id, T item) 
        {
            if (_dictionary.ContainsKey(id)) {
                _dictionary[id] = item;
            }
            else {
                _dictionary.Add(id, item);
                _list.Add(item);
            }
        }

        public void Update(int index, T item)
        {
            if (index < 0 || index >= _list.Count)
                return;
            _list[index] = item;
        }

        public void Delete(string id)
        {
            if (_dictionary.ContainsKey(id))
            {
                var item = _dictionary[id];
                _dictionary.Remove(id);
                _list.Remove(item);
            }
        }

        public void Delete(int index)
        {
            if (index < 0 || index >= _list.Count)
                return;
            var item = _list[index];
            _dictionary.Remove(item.Key);
            _list.RemoveAt(index);
        }

        public void Swap(int first, int second)
        {
            if (first < 0 || first >= _list.Count || second < 0 || second >= _list.Count)
                return;
            var firstItem = _list[first];
            var secondItem = _list[second];
            _list[first] = secondItem;
            _list[second] = firstItem;
        }

        public IReadOnlyList<T> GetAll()
        {
            return _list.AsReadOnly();
        }

        public abstract bool TrySerialize(out string data);

        public abstract bool TryDeserialize(string data);
    }
}