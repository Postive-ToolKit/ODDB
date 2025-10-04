using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Interfaces;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.DTO.Builders
{
    public class ViewDTOBuilder
    {
        private IHasName _nameInterface;
        private IHasODDBID _idInterface;
        private IHasFields _fieldsInterface;
        private IHasBindType _bindTypeInterface;
        private IHasParentView _parentViewInterface;
            
        public ViewDTOBuilder SetName(IHasName nameInterface)
        {
            _nameInterface = nameInterface;
            return this;
        }
            
        public ViewDTOBuilder SetID(IHasODDBID idInterface)
        {
            this._idInterface = idInterface;
            return this;
        }
            
        public ViewDTOBuilder SetTableMeta(IHasFields fieldsInterface)
        {
            _fieldsInterface = fieldsInterface;
            return this;
        }
            
        public ViewDTOBuilder SetBindType(IHasBindType bindTypeInterface)
        {
            _bindTypeInterface = bindTypeInterface;
            return this;
        }
        
        public ViewDTOBuilder SetParentView(IHasParentView parentViewInterface)
        {
            _parentViewInterface = parentViewInterface;
            return this;
        }
            
        public virtual ViewDTO Build()
        {
            var name = _nameInterface?.Name ?? string.Empty;
            var key = _idInterface?.ID ?? string.Empty;
            var tableMetas = _fieldsInterface?.ScopedFields ?? null;
            var convertedMeta = tableMetas == null ? new List<Field>() : new List<Field>(tableMetas);
            var convertedBindType = _bindTypeInterface?.BindType?.FullName ?? string.Empty;
            var parentView = _parentViewInterface?.ParentView?.ID ?? string.Empty;
            
            return new ViewDTO(
                name,
                key,
                convertedMeta,
                convertedBindType,
                parentView
            );
        }
    }
}