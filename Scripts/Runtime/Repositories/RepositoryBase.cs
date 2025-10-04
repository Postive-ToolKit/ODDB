using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Interfaces;
using TeamODD.ODDB.Runtime.Utils;

namespace TeamODD.ODDB.Runtime
{
    public abstract class RepositoryBase<T> : IRepository<T>, IDBDataObserver where T : IHasODDBID
    {
        public event Action<ODDBID> OnDataChanged;
        public event Action<ODDBID> OnDataRemoved;
        public IODDBIDProvider KeyProvider { get; set; }
        private readonly Dictionary<string, T> _dictionary = new Dictionary<string, T>();
        private readonly List<T> _list = new List<T>();
        public int Count => _dictionary.Count;

        public T Create(ODDBID id = null)
        {
            var uniqueID = id ?? new ODDBID();
            var item = CreateInternal(uniqueID);
            _dictionary.Add(uniqueID, item);
            _list.Add(item);
            OnDataChanged?.Invoke(uniqueID);
            return item;
        }
        
        protected abstract T CreateInternal(ODDBID id = null);

        public T Read(ODDBID id)
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
        
        public void Update(ODDBID id, T item) 
        {
            if (_dictionary.ContainsKey(id)) {
                _dictionary[id] = item;
            }
            else {
                _dictionary.Add(id, item);
                _list.Add(item);
            }
            OnDataChanged?.Invoke(id);
        }

        public void Update(int index, T item)
        {
            if (index < 0 || index >= _list.Count)
                return;
            _list[index] = item;
            OnDataChanged?.Invoke(item.ID);
        }

        public void Delete(ODDBID id)
        {
            if (_dictionary.ContainsKey(id))
            {
                var item = _dictionary[id];
                _dictionary.Remove(id);
                _list.Remove(item);
            }
            OnDataRemoved?.Invoke(id);
        }

        public void Delete(int index)
        {
            if (index < 0 || index >= _list.Count)
                return;
            var item = _list[index];
            _dictionary.Remove(item.ID);
            _list.RemoveAt(index);
            OnDataRemoved?.Invoke(item.ID);
        }

        public void Swap(int first, int second)
        {
            if (first < 0 || first >= _list.Count || second < 0 || second >= _list.Count)
                return;
            var firstItem = _list[first];
            var secondItem = _list[second];
            _list[first] = secondItem;
            _list[second] = firstItem;
            OnDataChanged?.Invoke(firstItem.ID);
            OnDataChanged?.Invoke(secondItem.ID);
        }

        public IReadOnlyList<T> GetAll()
        {
            return _list.AsReadOnly();
        }
    }
}