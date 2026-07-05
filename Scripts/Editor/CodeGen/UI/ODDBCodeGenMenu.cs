using System.Collections.Generic;
using UnityEditor;

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
            if (!OutputPathResolver.TryGetValidOutputFolder(out var abs, out var failureReason))
            {
                EditorUtility.DisplayDialog("ODDB CodeGen",
                    failureReason, "OK");
                return;
            }
            EditorUtility.RevealInFinder(abs);
        }
    }
}
