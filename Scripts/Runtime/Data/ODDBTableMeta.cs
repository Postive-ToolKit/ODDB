using Plugins.ODDB.Scripts.Runtime.Data.Enum;

namespace TeamODD.ODDB.Runtime.Data
{
    public struct ODDBTableMeta
    {
        public ODDBDataType DataType;
        public string Name;

        public ODDBTableMeta(ODDBDataType dataType, string name)
        {
            DataType = dataType;
            Name = name;
        }
    }
}