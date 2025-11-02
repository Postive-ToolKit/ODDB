using System.Text;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Enums;

namespace Plugins.ODDB.Scripts.Editor.Utils
{
    public static class EditorDataTypeExtensions
    {
        public static string GetName(this ODDBDataType dataType, string param)
        {
            var sb = new StringBuilder();
            sb.Append(dataType.ToString());
            switch (dataType)
            {
                case ODDBDataType.Addressable: case ODDBDataType.Resources: case ODDBDataType.Enum:
                    sb.Append(" - ").Append(param);
                    return sb.ToString();
                case ODDBDataType.View:
                    var useCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
                    if (useCase != null && useCase.GetViewByKey(param) != null)
                        sb.Append(" - ").Append(useCase.GetViewName(param));
                    return sb.ToString();
                default:
                    return sb.ToString();
            }
        }
    }
}