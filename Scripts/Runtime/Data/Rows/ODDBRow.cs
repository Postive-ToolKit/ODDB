using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using TeamODD.ODDB.Runtime.Utils;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Data
{
    public class ODDBRow : IODDBHasUniqueKey
    {
        public string Key { get; set; } = new ODDBID().ID;
        private List<string> _fields = new List<string>();
        public ODDBRow(int dataCount)
        {
            Key = new ODDBID().ID;
            for (int i = 0; i < dataCount; i++)
            {
                _fields.Add(null);
            }
        }
        public ODDBRow(string key,IEnumerable<string> data)
        {
            Key = key;
            _fields.AddRange(data);
        }
        public string GetData(int index)
        {
            if (index >= _fields.Count || index < 0)
                return null;
            return _fields[index];
        }
        public void SetData(int index, string data)
        {
            if (index >= _fields.Count || index < 0)
                return;
            _fields[index] = data;
        }
        public void InsertData(int index, string data)
        {
            if (index >= _fields.Count || index < 0)
                return;
            _fields.Insert(index, data);
        }

        public void RemoveData(int index)
        {
            if (index >= _fields.Count || index < 0)
                return;
            _fields.RemoveAt(index);
        }

        public void SwapData(int indexA, int indexB)
        {
            if (indexA >= 0 && indexA < _fields.Count && indexB >= 0 && indexB < _fields.Count)
            {
                (_fields[indexA], _fields[indexB]) = (_fields[indexB], _fields[indexA]);
            }
        }


        public void AddData(string data)
        {
            _fields.Add(data);
        }
    }
}