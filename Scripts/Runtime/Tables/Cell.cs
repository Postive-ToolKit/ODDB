using System;
using Newtonsoft.Json;
using TeamODD.ODDB.Runtime.Serializers;
using TeamODD.ODDB.Runtime.Types;
using UnityEngine;

namespace TeamODD.ODDB.Runtime
{
    [Serializable]
    public class Cell
    {
        public const string SERIALIZED_DATA_FIELD = nameof(_serializedData);
        public const string DATA_TYPE_FIELD = nameof(_fieldType);
        /// <summary>
        /// The serialized string representation of the cell's data.
        /// </summary>
        [JsonIgnore]
        public string SerializedData => _serializedData;

        /// <summary>
        /// The data type of the cell, which determines how the data is serialized and deserialized.
        /// </summary>
        [JsonIgnore]
        public FieldType FieldType
        {
            get => _fieldType;
            set => _fieldType = value;
        }

        [JsonIgnore]
        [SerializeField] private FieldType _fieldType;

        [JsonProperty("serializedData")]
        [SerializeField] private string _serializedData;

        public Cell()
        {
            _serializedData = string.Empty;
        }

        [JsonConstructor]
        public Cell(string serializedData)
        {
            _serializedData = serializedData;
        }

        public Cell(FieldType fieldType)
        {
            FieldType = fieldType;
        }

        public Cell(string serializedData, FieldType fieldType)
        {
            _serializedData = serializedData;
            FieldType = fieldType;
        }

        private IDataSerializer ResolveSerializer()
        {
            var key = FieldType?.TypeKey ?? string.Empty;
            var serializer = TypeRegistry.Get(key);
            if (serializer != null)
                return serializer;
            // Last-resort fallback so a missing/unknown type key never crashes a round-trip.
            return new StringSerializer();
        }

        /// <summary>
        /// Sets the data for the cell by serializing the provided object.
        /// </summary>
        public void SetData(object data, bool direct = false)
        {
            if (direct)
            {
                _serializedData = data as string;
                return;
            }
            var serializer = ResolveSerializer();
            _serializedData = serializer.Serialize(data, FieldType?.Param ?? string.Empty);
        }

        /// <summary>
        /// Gets the data from the cell by deserializing the stored string.
        /// </summary>
        public object GetData()
        {
            var serializer = ResolveSerializer();
            return serializer.Deserialize(_serializedData, FieldType?.Param ?? string.Empty);
        }
    }
}
