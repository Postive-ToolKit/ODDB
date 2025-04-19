using System;
using System.Collections.Generic;
using TeamODD.ODDB.Runtime.Data.Enum;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI
{
    public class ODDBMetaSelectView : DropdownField
    {
        private readonly Dictionary<string, ODDBDataType> _oddbDataTypes = new();
        public event Action<ODDBDataType> OnTypeChanged;
        public ODDBMetaSelectView()
        {
            CreateDropDown();
            labelElement.style.minWidth = 0;
            labelElement.style.alignSelf = Align.FlexStart;
        }
        
        private void CreateDropDown()
        {
            foreach (ODDBDataType type in Enum.GetValues(typeof(ODDBDataType)))
            {
                _oddbDataTypes[type.ToString()] = type;
                choices.Add(type.ToString());
            }
            value = choices[0];
            
            RegisterCallback<ChangeEvent<string>>(OnDropDownValueChanged);
        }
        
        public void SetType(ODDBDataType type)
        {
            value = type.ToString();
        }
        private void OnDropDownValueChanged(ChangeEvent<string> evt)
        {
            if (_oddbDataTypes.TryGetValue(evt.newValue, out var type)) {
                value = evt.newValue;
                OnTypeChanged?.Invoke(type);
            }
        }
    }
}