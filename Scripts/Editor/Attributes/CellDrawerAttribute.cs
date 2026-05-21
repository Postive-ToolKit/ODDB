using System;

namespace TeamODD.ODDB.Editors.Attributes
{
    /// <summary>
    /// Attribute to specify a custom field drawer for a field in ODDB.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true, AllowMultiple = false)]
    public class CellDrawerAttribute : Attribute
    {
        public string TypeKey { get; }

        public CellDrawerAttribute(string typeKey)
        {
            TypeKey = typeKey;
        }
    }
}
