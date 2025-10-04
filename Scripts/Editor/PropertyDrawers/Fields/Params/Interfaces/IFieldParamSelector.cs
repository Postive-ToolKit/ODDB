using System.Collections.Generic;

namespace TeamODD.ODDB.Runtime.Params.Interfaces
{
    /// <summary>
    /// Interface for selecting field parameters that provide a set of options.
    /// </summary>
    public interface IFieldParamSelector
    {
        /// <summary>
        /// Retrieves a dictionary of options where each key is the option's value and each value is the option's display name.
        /// </summary>
        /// <returns> A dictionary of options.</returns>
        public Dictionary<string, string> GetOptions();
    }
}