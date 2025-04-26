using System;
using System.Collections.Generic;

namespace TeamODD.ODDB.Runtime.Utils
{
    public class ODDBID
    {
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
        private readonly string _id;
        public string ID => _id;
        public ODDBID(string id = null)
        {
            if (string.IsNullOrEmpty(id))
            {
                _id = GenerateID();
                return;
            }
            if (!_currentCreatedId.Contains(id))
                _currentCreatedId.Add(id);
            _id = id;
            
        }
        public override string ToString()
        {
            return _id;
        }
        public static implicit operator string(ODDBID oddbid)
        {
            return oddbid.ID;
        }
    }
}