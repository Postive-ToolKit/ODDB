using System.Collections.Generic;
using System.IO;
using TeamODD.ODDB.Editors.Settings;
using UnityEditor;
using UnityEngine;

namespace TeamODD.ODDB.Editors.CodeGen.UI
{
    /// <summary>
    /// Reusable menu helpers for triggering code generation from the toolbar
    /// dropdown and the TreeView right-click menu.
    /// </summary>
    internal static class ODDBCodeGenMenu
    {
        public static void RunGenerateAll()
        {
            ODDBCodeGenerator.GenerateAll();
        }

        public static void RunGenerateSelection(IEnumerable<string> viewIds)
        {
            ODDBCodeGenerator.GenerateSelection(viewIds);
        }

        public static void OpenGeneratedFolder()
        {
            var rel = ODDBEditorSettings.Setting != null ? ODDBEditorSettings.Setting.GeneratedCodePath : null;
            if (string.IsNullOrEmpty(rel))
            {
                EditorUtility.DisplayDialog("ODDB CodeGen",
                    "Set ODDBEditorSettings.GeneratedCodePath first.", "OK");
                return;
            }
            string assetsRel = rel.Replace('\\', '/').TrimStart('/');
            if (!assetsRel.StartsWith("Assets/")) assetsRel = "Assets/" + assetsRel;
            string abs = Path.GetFullPath(assetsRel);
            if (!Directory.Exists(abs))
            {
                EditorUtility.DisplayDialog("ODDB CodeGen",
                    $"Folder does not exist: {assetsRel}", "OK");
                return;
            }
            EditorUtility.RevealInFinder(abs);
        }
    }
}
