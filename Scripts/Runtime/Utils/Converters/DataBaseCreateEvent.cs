using System;

namespace TeamODD.ODDB.Runtime.Utils.Converters
{
    /// <summary>
    /// Event structure for data processing events with priority and action
    /// </summary>
    public class DataBaseCreateEvent
    {
        public DataCreateProcess Priority;
        public Action<ODDatabase> OnEvent;
    }
}