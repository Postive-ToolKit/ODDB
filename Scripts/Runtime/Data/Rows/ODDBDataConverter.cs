using UnityEngine;

namespace TeamODD.ODDB.Runtime.Data
{
    public class ODDBDataConverter<T>
    {
        public static T TryConvert(object data)
        {
            if (data is T converted) 
                return converted;

            Debug.LogError($"Cannot convert {data.GetType()} to {typeof(T)}");
            return default;
        }
    }
}