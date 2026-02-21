using System.Text;
using UnityEditor;
using UnityEngine;
namespace TeamODD.ODDB.Editors.Utils
{
    public class ODDBPathUtility
    {
        public string GetPath(string mustContain = null, string basePath = null)
        {
            mustContain = mustContain ?? string.Empty;
            basePath = basePath ?? Application.dataPath;

            string result;
            do{
                result = EditorUtility.OpenFolderPanel("Set Path of Database File", basePath, "");
                if (string.IsNullOrEmpty(result))
                {
                    EditorUtility.DisplayDialog("Canceled", "The path selection was canceled.", "OK");
                    return null;
                }
                
                if(!result.Contains(mustContain))
                {
                    //Show error dialog
                    var sb = new StringBuilder();
                    sb.AppendLine("The selected path is not a valid database file path.");
                    sb.AppendLine($"Please select a path in the {mustContain} folder.");
                    EditorUtility.DisplayDialog("Error", sb.ToString(), "OK");
                }
                //check is the derectory in the project resources folder
            } while(!result.Contains(mustContain));
            //remove all backslash
            result = result.Replace("\\", "/");
            return result;
        }
    }
}
