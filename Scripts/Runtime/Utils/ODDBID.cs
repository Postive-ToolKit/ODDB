using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Utils
{
    [Serializable]
    public class ODDBID
    {
        public const string ID_FIELD_NAME = nameof(_id);
        private const int ID_LENGTH = 20;
        private static readonly HashSet<string> _currentCreatedId = new HashSet<string>();
        private static string GenerateID()
        {
            var id = Guid.NewGuid().ToString().Replace("-", "").Substring(0, ID_LENGTH);
            while (_currentCreatedId.Contains(id))
            {
                id = Guid.NewGuid().ToString().Replace("-", "").Substring(0, ID_LENGTH);
            }
            _currentCreatedId.Add(id);
            return id;
        }
        [SerializeField]
        private string _id = null;
        [JsonProperty("id")]
        public string ID
        {
            get => _id;
            set {
                if (string.IsNullOrEmpty(value)) {
                    _id = GenerateID();
                    _currentCreatedId.Add(_id);
                    return;
                }
                if (!_currentCreatedId.Contains(value)) {
                    _currentCreatedId.Add(value);
                }
                _id = value;
            }
        }

        public ODDBID()
        {
            ID = null;
        }
        [JsonConstructor]
        public ODDBID(string id)
        {
            ID = id;
        }
        
        public override string ToString()
        {
            return _id;
        }
        
        public static implicit operator string(ODDBID oddbid)
        {
            return oddbid.ID;
        }
        
        public static bool operator ==(ODDBID a, ODDBID b)
        {
            if (a is null || b is null) return false;
            return a.ID.Equals(b.ID);
        }

        public static bool operator !=(ODDBID a, ODDBID b)
        {
            return !(a == b);
        }
        
        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            return ID.Equals(obj);
        }
    }
}