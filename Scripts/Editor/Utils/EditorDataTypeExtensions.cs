using System.Text;
using TeamODD.ODDB.Editors.Window;

namespace TeamODD.ODDB.Editors.Utils
{
    public static class EditorDataTypeExtensions
    {
        /// <summary>
        /// Format a field type for display, e.g. "int", "enum - Rarity", "view - Heroes (a1b2c3)".
        /// </summary>
        public static string GetDisplayName(string typeKey, string param)
        {
            var sb = new StringBuilder();
            sb.Append(typeKey ?? string.Empty);

            switch (typeKey)
            {
                case "addressable":
                case "resource":
                case "enum":
                case "custom":
                    if (!string.IsNullOrEmpty(param))
                        sb.Append(" - ").Append(param);
                    return sb.ToString();
                case "view":
                    var useCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
                    var view = useCase?.GetViewByKey(param);
                    if (view != null)
                        sb.Append(" - ").Append(ODDBEditorDisplayUtility.FormatNameWithId(view.Name, view.ID));
                    else if (!string.IsNullOrEmpty(param))
                        sb.Append(" - ").Append(param);
                    return sb.ToString();
                default:
                    return sb.ToString();
            }
        }
    }
}
