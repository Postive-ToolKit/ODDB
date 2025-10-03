using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Data.DTO.Builders
{
    public class ODDBViewDTOBuilder
    {
        private IODDBHasName _nameInterface;
        private IODDBHasUniqueID _idInterface;
        private IODDBHasTableMeta _tableMetaInterface;
        private IODDBHasBindType _bindTypeInterface;
        private IODDBHasParentView _parentViewInterface;
            
        public ODDBViewDTOBuilder SetName(IODDBHasName nameInterface)
        {
            _nameInterface = nameInterface;
            return this;
        }
            
        public ODDBViewDTOBuilder SetID(IODDBHasUniqueID idInterface)
        {
            this._idInterface = idInterface;
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
            var key = _idInterface?.ID ?? string.Empty;
            var tableMetas = _tableMetaInterface?.ScopedFields ?? null;
            var convertedMeta = tableMetas == null ? new List<ODDBField>() : new List<ODDBField>(tableMetas);
            var convertedBindType = _bindTypeInterface?.BindType?.FullName ?? string.Empty;
            var parentView = _parentViewInterface?.ParentView?.ID ?? string.Empty;
            
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