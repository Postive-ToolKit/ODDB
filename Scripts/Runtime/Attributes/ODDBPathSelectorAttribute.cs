using UnityEngine;
namespace TeamODD.ODDB.Runtime.Settings.Attributes
{
    public class ODDBPathSelectorAttribute : PropertyAttribute
    {
        public bool IsFolder;
        public string BasePath;

        public ODDBPathSelectorAttribute(bool isFolder = true, string basePath = null)
        {
            this.IsFolder = isFolder;
            this.BasePath = basePath;
        }
    }
}