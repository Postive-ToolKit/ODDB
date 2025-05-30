using System;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI.Fields
{
    public interface IODDBField
    {
        VisualElement Root { get; }
        void SetValue(object value);
        object GetValue();
        void RegisterValueChangedCallback(Action<object> callback);
    }
} 