using System;

namespace TeamODD.ODDB.Runtime.Attributes
{
    /// <summary>
    /// Attribute to provide metadata for ODDB fields, including field drawer type and visibility in type selector.
    /// Retained for back-compat; the canonical way to mark types as hidden is via TypeRegistry / [ODDBType].
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, Inherited = false, AllowMultiple = false)]
    public class DataTypeOptionAttribute : Attribute
    {
        public bool IsHideInSelector { get; }

        public DataTypeOptionAttribute(bool isHideInSelector = false)
        {
            IsHideInSelector = isHideInSelector;
        }
    }
}
