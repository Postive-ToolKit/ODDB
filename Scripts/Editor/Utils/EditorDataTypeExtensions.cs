using System.Text;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Enums;

namespace TeamODD.ODDB.Editors.Utils
{
    public static class EditorDataTypeExtensions
    {
        public static string GetName(this ODDBDataType dataType, string param)
        {
            var sb = new StringBuilder();
            sb.Append(dataType.ToString());
            switch (dataType)
            {
                #if ADDRESSABLE_EXIST
                case ODDBDataType.Addressable: 
                #endif
                case ODDBDataType.Resources: 
                case ODDBDataType.Enum:
                case ODDBDataType.Custom:
                    sb.Append(" - ").Append(param);
                    return sb.ToString();
                case ODDBDataType.View:
                    var useCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
                    var view = useCase?.GetViewByKey(param);
                    if (view != null)
                        sb.Append(" - ").Append(ODDBEditorDisplayUtility.FormatNameWithId(view.Name, view.ID));
                    else if (string.IsNullOrEmpty(param) == false)
                        sb.Append(" - ").Append(param);
                    return sb.ToString();
                default:
                    return sb.ToString();
            }
        }

    }
}
