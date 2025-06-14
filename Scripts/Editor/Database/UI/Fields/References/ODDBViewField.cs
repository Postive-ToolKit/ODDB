using System;
using System.Collections.Generic;
using System.Linq;
using TeamODD.ODDB.Editors.Utils;
using TeamODD.ODDB.Editors.Window;
using TeamODD.ODDB.Runtime.Data;
using UnityEngine.UIElements;

namespace TeamODD.ODDB.Editors.UI.Fields.References
{
    public class ODDBViewField : IODDBField
    {
        private readonly VisualElement _container;
        private readonly DropdownField _viewSelector;
        public VisualElement Root => _container;
        private IODDBEditorUseCase _editorUseCase;
        private Dictionary<int, string> _idMapping = new Dictionary<int, string>();
        private Dictionary<string, int> _reverseIdMapping = new Dictionary<string, int>();
        public ODDBViewField()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            // Create container
            _container = new VisualElement();
            _container.style.flexGrow = 1;

            // Create and setup toggle
            _viewSelector = new DropdownField();
            _viewSelector.style.marginLeft = 0;
            _viewSelector.style.marginRight = 0;

            var allTables = _editorUseCase.GetViews(view => view is ODDBTable).Select(view => view as ODDBTable).ToList();
            
            var choices = new List<string>();
            foreach (var table in allTables)
            {
                var basePath = table.ID + "/";
                var namePath = table.Name + "/";
                foreach (var row in table.ReadOnlyRows)
                {
                    var path = basePath + row.ID;
                    var name = namePath + (row.GetData(0) == null ? row.ID : row.GetData(0));
                    _idMapping.Add(choices.Count, path);
                    _reverseIdMapping[path] = choices.Count;
                    choices.Add(name);
                }
            }
            _viewSelector.choices = choices;
            // Add toggle to container
            _container.Add(_viewSelector);
        }

        public void SetValue(object value)
        {
            var path = value?.ToString();
            if (string.IsNullOrEmpty(path)) {
                _viewSelector.value = string.Empty;
                return;
            }
            if (_reverseIdMapping.TryGetValue(path, out var index))
            {
                _viewSelector.value = _viewSelector.choices[index];
                return;
            }
            _viewSelector.value = string.Empty;
        }

        public object GetValue()
        {
            return _viewSelector.value;
        }

        public void RegisterValueChangedCallback(Action<object> callback)
        {
            if (_viewSelector == null)
                return;
            _viewSelector.RegisterValueChangedCallback(evt =>
            {
                var path = evt.newValue;
                if (string.IsNullOrEmpty(path))
                    return;
                var index = _viewSelector.choices.IndexOf(path);
                var mappedPath = _idMapping[index];
                if (string.IsNullOrEmpty(mappedPath))
                    return;
                callback?.Invoke(mappedPath);
            });
        }
    }
}