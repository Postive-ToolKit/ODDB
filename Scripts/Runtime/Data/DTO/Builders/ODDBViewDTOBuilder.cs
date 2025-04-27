using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Data.DTO.Builders
{
    public class ODDBViewDTOBuilder
    {
        private IODDBHasName _nameInterface;
        private IODDBHasUniqueKey _keyInterface;
        private IODDBHasTableMeta _tableMetaInterface;
        private IODDBHasBindType _bindTypeInterface;
        private IODDBHasParentView _parentViewInterface;
            
        public ODDBViewDTOBuilder SetName(IODDBHasName nameInterface)
        {
            _nameInterface = nameInterface;
            return this;
        }
            
        public ODDBViewDTOBuilder SetKey(IODDBHasUniqueKey keyInterface)
        {
            _keyInterface = keyInterface;
            return this;
        }
            
        public ODDBViewDTOBuilder SetTableMeta(IODDBHasTableMeta tableMetaInterface)
        {
            _tableMetaInterface = tableMetaInterface;
            return this;
        }
            
        public ODDBViewDTOBuilder SetBindType(IODDBHasBindType bindTypeInterface)
        {
            _bindTypeInterface = bindTypeInterface;
            return this;
        }
        
        public ODDBViewDTOBuilder SetParentView(IODDBHasParentView parentViewInterface)
        {
            _parentViewInterface = parentViewInterface;
            return this;
        }
            
        public virtual ODDBViewDTO Build()
        {
            var name = _nameInterface?.Name ?? string.Empty;
            var key = _keyInterface?.Key ?? string.Empty;
            var tableMetas = _tableMetaInterface?.ScopedTableMetas ?? null;
            var convertedMeta = tableMetas == null ? new List<ODDBField>() : new List<ODDBField>(tableMetas);
            var convertedBindType = _bindTypeInterface?.BindType?.FullName ?? string.Empty;
            var parentView = _parentViewInterface?.ParentView?.Key ?? string.Empty;
   
            return new ODDBViewDTO(
                name,
                key,
                convertedMeta,
                convertedBindType,
                parentView
            );
        }
    }
}