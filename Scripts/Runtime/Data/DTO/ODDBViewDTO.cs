using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data.Interfaces;
namespace TeamODD.ODDB.Runtime.Data.DTO
{
    public struct ODDBViewDTO : IODDBHasName, IODDBHasUniqueKey, IODDBHasTableMeta
    {
        public string Name { get; set; }
        public string Key { get; set; }
        public List<ODDBTableMeta> TableMetas { get; set; }
        public string BindType { get; set; }
        
        public ODDBViewDTO(string name, string key, List<ODDBTableMeta> tableMetas, string bindType)
        {
            Name = name;
            Key = key;
            TableMetas = tableMetas;
            BindType = bindType;
        }
        public class Builder
        {
            private IODDBHasName _nameInterface;
            private IODDBHasUniqueKey _keyInterface;
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
            
            public ODDBViewDTO Build()
            {
                var name = _nameInterface?.Name ?? string.Empty;
                var key = _keyInterface?.Key ?? string.Empty;
                var tableMetas = _tableMetaInterface?.TableMetas ?? null;
                var convertedMeta = tableMetas == null ? new List<ODDBTableMeta>() : new List<ODDBTableMeta>(tableMetas);
                var convertedBindType = _bindTypeInterface?.BindType?.FullName ?? string.Empty;
                
                return new ODDBViewDTO(
                    name,
                    key,
                    convertedMeta,
                    convertedBindType
                );
            }
        }
    }
}