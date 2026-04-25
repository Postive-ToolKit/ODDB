using System;
using System.Linq;
using TeamODD.ODDB.Runtime.Entities;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Attributes
{
    /// <summary>
    /// Inspector attribute to select an ODDB Entity by its ID.
    /// </summary>
    public class ODDBIDSelectorAttribute : PropertyAttribute
    {
        /// <summary>
        /// Allowed tables for selection.
        /// </summary>
        public Type[] AllowEntities { get; private set; }
        
        /// <summary>
        /// Constructor for ODDBIDSelectorAttribute.
        /// Constructor for ODDBIDSelectorAttribute.
        /// </summary>
        /// <param name="allowEntities"> Optional list of allowed tables for selection.</param>
        public ODDBIDSelectorAttribute(params Type[] allowEntities)
        {
            AllowEntities = allowEntities
                .Where(IsValidSelectorFilter)
                .ToArray();

        }

        private static bool IsValidSelectorFilter(Type type)
        {
            if (type == null)
                return false;

            return type.IsInterface || typeof(ODDBEntity).IsAssignableFrom(type);
        }
    }
}
