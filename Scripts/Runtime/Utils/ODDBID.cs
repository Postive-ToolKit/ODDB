using System;

namespace TeamODD.ODDB.Runtime
{
    public class ODDBID
    {
        private const int ID_LENGTH = 12;
        public string ID => _id;
        private readonly string _id;

        public ODDBID()
        {
            _id = Guid.NewGuid().ToString().Substring(0, ID_LENGTH);
        }
        public override string ToString()
        {
            return _id;
        }
        public static explicit operator string(ODDBID oddbid)
        {
            return oddbid.ID;
        }
    }
}