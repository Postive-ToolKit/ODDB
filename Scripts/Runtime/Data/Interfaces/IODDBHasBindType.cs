using System;

namespace Plugins.ODDB.Scripts.Runtime.Data.Interfaces
{
    public interface IODDBHasBindType
    {
        Type BindType { get; set; }
    }
}