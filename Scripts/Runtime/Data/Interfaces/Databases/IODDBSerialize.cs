namespace TeamODD.ODDB.Runtime.Data.Interfaces
{
    public interface IODDBSerialize
    {
        bool TrySerialize(out string data);
        bool TryDeserialize(string data);
    }
}