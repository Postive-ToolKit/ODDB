using UnityEngine;
namespace TeamODD.ODDB.Runtime.Attributes
{
    public class PathSelectorAttribute : PropertyAttribute
    {
        public bool IsFolder;
        public string BasePath;

        public PathSelectorAttribute(bool isFolder = true, string basePath = null)
        {
            this.IsFolder = isFolder;
            this.BasePath = basePath;
        }
    }
}