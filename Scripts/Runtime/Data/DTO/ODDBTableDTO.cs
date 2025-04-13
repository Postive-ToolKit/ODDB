using System.Collections.Generic;
using Plugins.ODDB.Scripts.Runtime.Data.Interfaces;
using TeamODD.ODDB.Scripts.Runtime.Data;

namespace Plugins.ODDB.Scripts.Runtime.Data.DTO
{
    public struct ODDBTableDTO : IODDBHasName, IODDBHasUniqueKey, IODDBHasTableMeta
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public List<ODDBTableMeta> TableMetas { get; set; }
        public string Data;
        public string BindType { get; set; }

        private ODDBTableDTO(string name, string key,List<ODDBTableMeta> tableMetas, string data, string bindType)
        {
            Name = name;
            Key = key;
            TableMetas = tableMetas;
            Data = data;
            BindType = bindType;
        }
        public class Builder
        {
            private IODDBHasName _nameInterface;
            private IODDBHasUniqueKey _keyInterface;
            private IODDBAvailableSerialize _serializationInterface;
            private IODDBHasTableMeta _tableMetaInterface;
            private IODDBHasBindType _bindTypeInterface;
            
            public Builder SetName(IODDBHasName nameInterface)
            {
                _nameInterface = nameInterface;
                return this;
            }
            
            public Builder SetKey(IODDBHasUniqueKey keyInterface)
            {
                _keyInterface = keyInterface;
                return this;
            }
            
            public Builder SetSerialization(IODDBAvailableSerialize serializationInterface)
            {
                _serializationInterface = serializationInterface;
                return this;
            }
            
            public Builder SetTableMeta(IODDBHasTableMeta tableMetaInterface)
            {
                _tableMetaInterface = tableMetaInterface;
                return this;
            }
            
            public Builder SetBindType(IODDBHasBindType bindTypeInterface)
            {
                _bindTypeInterface = bindTypeInterface;
                return this;
            }
            
            public ODDBTableDTO Build()
            {
                var name = _nameInterface?.Name ?? string.Empty;
                var key = _keyInterface?.Key ?? string.Empty;
                var data = _serializationInterface?.Serialize() ?? string.Empty;
                var tableMetas = _tableMetaInterface?.TableMetas ?? null;

                var convertedMeta = tableMetas == null ? new List<ODDBTableMeta>() : new List<ODDBTableMeta>(tableMetas);
                var convertedBindType = _bindTypeInterface?.BindType?.FullName ?? string.Empty;
                return new ODDBTableDTO(name, key,convertedMeta, data,convertedBindType);
            }
        }
    }
}