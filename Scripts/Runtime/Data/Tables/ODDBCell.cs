using System;
using Newtonsoft.Json;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Data.Enum;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Data
{
    [Serializable]
    public class ODDBCell
    {
        public const string SERIALIZED_DATA_FIELD = nameof(_serializedData);
        public const string DATA_TYPE_FIELD = nameof(_dataType);
        /// <summary>
        /// The serialized string representation of the cell's data.
        /// </summary>
        [JsonIgnore]
        public string SerializedData => _serializedData;

        /// <summary>
        /// The data type of the cell, which determines how the data is serialized and deserialized.
        /// </summary>
        [JsonIgnore]
        public ODDBDataType DataType
        {
            get => _dataType;
            set => _dataType = value;
        }
        
        [JsonIgnore]
        [SerializeField] private ODDBDataType _dataType;

        [JsonProperty("serializedData")]
        [SerializeField] private string _serializedData;

        public ODDBCell()
        {
            _serializedData = string.Empty;
        }
        
        [JsonConstructor]
        public ODDBCell(string serializedData)
        {
            _serializedData = serializedData;
        }
        
        public ODDBCell(ODDBDataType dataType)
        {
            DataType = dataType;
        }
        
        public ODDBCell(string serializedData, ODDBDataType dataType)
        {
            _serializedData = serializedData;
            DataType = dataType;
        }
        
        /// <summary>
        /// Sets the data for the cell by serializing the provided object.
        /// </summary>
        /// <param name="data"> The data to be serialized and stored in the cell.</param>
        public void SetData(object data)
        {
            _serializedData = DataType.GetDataSerializer().Serialize(data);
        }

        /// <summary>
        /// Gets the data from the cell by deserializing the stored string.
        /// </summary>
        /// <returns> The deserialized object stored in the cell.</returns>
        public object GetData()
        {
            return DataType.GetDataSerializer().Deserialize(_serializedData);
        }
    }
}