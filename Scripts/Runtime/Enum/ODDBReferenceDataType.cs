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
        Object,
        [ReferenceDataBind(typeof(ScriptableObject))]
        ScriptableObject,
        [ReferenceDataBind(typeof(GameObject))]
        GameObject,
        [ReferenceDataBind(typeof(Sprite))]
        Sprite,
        [ReferenceDataBind(typeof(Texture))]
        Texture,
        [ReferenceDataBind(typeof(AudioClip))]
        AudioClip,
        [ReferenceDataBind(typeof(AnimationClip))]
        AnimationClip,
        [ReferenceDataBind(typeof(Animator))]
        Animator,
        [ReferenceDataBind(typeof(Material))]
        Material,
    }
}