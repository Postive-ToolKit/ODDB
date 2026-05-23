using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using TeamODD.ODDB.Runtime.Infrastructure;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Utils.Converters
{
    [Serializable]
    public class ODDBID : IEquatable<ODDBID>
    {
        public const string ID_FIELD_NAME = nameof(_id);
        private const int ID_LENGTH = 8;
        private static readonly HashSet<string> _currentCreatedId = new HashSet<string>();

        private sealed class TrackedIdCache : IClearableCache
        {
            public void Clear()
            {
                // Reset the in-memory uniqueness tracker on domain reload. Live IDs
                // from loaded databases are re-added lazily when each ODDBID is
                // deserialized (its ID setter calls _currentCreatedId.Add).
                _currentCreatedId.Clear();
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterTrackedIdCacheAtRuntime()
        {
            DomainReloadHub.Register(new TrackedIdCache());
        }

#if UNITY_EDITOR
        [UnityEditor.InitializeOnLoadMethod]
        private static void RegisterTrackedIdCacheInEditor()
        {
            DomainReloadHub.Register(new TrackedIdCache());
        }
#endif

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
            return oddbid?.ID ?? string.Empty;
        }
        
        public static bool operator ==(ODDBID a, ODDBID b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;
            return string.Equals(a.ID, b.ID);
        }

        public static bool operator !=(ODDBID a, ODDBID b)
        {
            return !(a == b);
        }
        
        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public bool Equals(ODDBID other)
        {
            if (other is null) return false;
            return string.Equals(ID, other.ID);
        }

        public override bool Equals(object obj)
        {
            return obj is ODDBID other && Equals(other);
        }
    }
}