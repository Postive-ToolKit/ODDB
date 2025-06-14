using TeamODD.ODDB.Runtime.Data.Interfaces;

namespace TeamODD.ODDB.Runtime.Data.DTO.Builders
{
    public class ODDBTableDTOBuilder : ODDBViewDTOBuilder
    {
        private IODDBAvailableSerialize _serializationInterface;
        
        public ODDBTableDTOBuilder SetSerialization(IODDBAvailableSerialize serializationInterface)
        {
            _serializationInterface = serializationInterface;
            return this;
        }
            
        public override ODDBViewDTO Build()
        {
            var viewDto = base.Build();
            // convert as ODDBTableDTO
            var name = viewDto.Name;
            var key = viewDto.ID;
            var convertedMeta = viewDto.TableMetas;
            var convertedBindType = viewDto.BindType;
            var parentView = viewDto.ParentView;
            var data = _serializationInterface?.Serialize() ?? string.Empty;
            return new ODDBTableDTO(name, key, convertedMeta, convertedBindType, parentView,data);
        }
    }
}