using System;
using Newtonsoft.Json;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Enums;
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
        
        /// <summary>
        /// Sets the data for the cell by serializing the provided object.
        /// </summary>
        /// <param name="data"> The data to be serialized and stored in the cell.</param>
        public void SetData(object data)
        {
            _serializedData = FieldType.Type.GetDataSerializer().Serialize(data, FieldType.Param);
        }

        /// <summary>
        /// Gets the data from the cell by deserializing the stored string.
        /// </summary>
        /// <returns> The deserialized object stored in the cell.</returns>
        public object GetData()
        {
            return FieldType.Type.GetDataSerializer().Deserialize(_serializedData, FieldType.Param);
        }
    }
}