using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Attributes;
using TeamODD.ODDB.Runtime.Enum;
using TeamODD.ODDB.Runtime.Params.Interfaces;

namespace TeamODD.ODDB.Runtime.Params
{
    /// <summary>
    /// Sub selector creator for Data Type
    /// </summary>
    [UseSubSelector(ODDBDataType.View)]
    public class ViewParamSelector : IFieldParamSelector
    {
        public Dictionary<string, string> GetOptions()
        {
            var useCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            var views = useCase.GetViews();
            var result = new Dictionary<string, string>();
            foreach (var view in views)
            {
                var key = view.ID.ToString();
                var name = $"{view.Name} - {key.Substring(0, 6)}";
                result.Add(key, name);
            }
            return result;
        }
    }
}