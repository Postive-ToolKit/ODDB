using UnityEngine;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI.Fields
{
    public class ODDBFieldBase : VisualElement
    {
        public ODDBFieldBase()
        {
            style.flexGrow = 1;
            style.paddingBottom = 0;
            style.paddingTop = 0;
            style.paddingLeft = 0;
            style.paddingRight = 0;
            
            style.marginBottom = 0;
            style.marginTop = 0;
            style.marginLeft = 0;
            style.marginRight = 0;
            style.borderBottomLeftRadius = 0;
            style.borderBottomRightRadius = 0;
            style.borderTopLeftRadius = 0;
            style.borderTopRightRadius = 0;

            style.borderBottomColor = new StyleColor(Color.black);
            style.borderBottomWidth = 1;
            style.borderTopColor = new StyleColor(Color.black);
            style.borderTopWidth = 1;
            style.borderLeftColor = new StyleColor(Color.black);
            style.borderLeftWidth = 1;
        }
    }
}