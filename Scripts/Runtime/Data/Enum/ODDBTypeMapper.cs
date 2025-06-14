using System;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Data.Enum
{
    public static class ODDBTypeMapper
    {
        public static Type GetEnumType(ODDBDataType type)
        {
            switch (type)
            {
                case ODDBDataType.Bool:
                    return typeof(bool);
                case ODDBDataType.Int:
                    return typeof(int);
                case ODDBDataType.Float:
                    return typeof(float);
                case ODDBDataType.String:
                    return typeof(string);
                // case ODDBDataType.Vector2:
                //     return typeof(Vector2);
                // case ODDBDataType.Vector3:
                //     return typeof(Vector3);
                // case ODDBDataType.Vector4:
                //     return typeof(Vector4);
                // case ODDBDataType.Color:
                //     return typeof(Color);
                case ODDBDataType.ScriptableObject:
                    return typeof(ScriptableObject);
                case ODDBDataType.View:
                    return typeof(IODDBView);
                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        } 
    }
}