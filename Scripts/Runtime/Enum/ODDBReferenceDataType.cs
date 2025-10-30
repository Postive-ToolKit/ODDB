using System;
using TeamODD.ODDB.Runtime.Attributes;
using UnityEngine;
using Object = UnityEngine.Object;

namespace TeamODD.ODDB.Runtime.Enums
{
    /// <summary>
    /// Enum representing different types of reference data in ODDB.
    /// Add Data Type Binding to Link ODDBReferenceDataType with ODDBDataType.
    /// </summary>
    public enum ODDBReferenceDataType
    {
        [ReferenceDataBind(typeof(Object))]
        Object = 0,
        [ReferenceDataBind(typeof(ScriptableObject))]
        ScriptableObject = 1,
        [ReferenceDataBind(typeof(GameObject))]
        GameObject = 2,
        [ReferenceDataBind(typeof(Sprite))]
        Sprite = 3,
        [ReferenceDataBind(typeof(Texture))]
        Texture = 4,
        [ReferenceDataBind(typeof(AudioClip))]
        AudioClip = 5,
    }
}