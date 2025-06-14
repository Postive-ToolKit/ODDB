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
        public ODDBViewField()
        {
            _editorUseCase = ODDBEditorDI.Resolve<IODDBEditorUseCase>();
            // Create container
            _container = new VisualElement();
            _container.style.flexGrow = 1;
            _container.style.alignItems = Align.Center;
            _container.style.justifyContent = Justify.Center;

            // Create and setup toggle
            _viewSelector = new DropdownField();
            _viewSelector.style.marginLeft = 0;
            _viewSelector.style.marginRight = 0;

            var allTables = _editorUseCase.GetViews(view => view is ODDBTable).Select(view => view as ODDBTable).ToList();
            
            var choices = new List<string>();
            foreach (var table in allTables)
            {
                var basePath = table.Key + "/";
                var namePath = table.Name + "/";
                foreach (var row in table.ReadOnlyRows)
                {
                    var path = basePath + row.Key;
                    var name = namePath + (row.GetData(0) == null ? row.Key : row.GetData(0));
                    _idMapping.Add(choices.Count, path);
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
            var destination = path.Split("/");
            var view = _editorUseCase.GetViewByKey(destination[0]);
            if (view == null) {
                _viewSelector.value = string.Empty;
                return;
            }
            _viewSelector.value = value.ToString();
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
                var destination = mappedPath.Split("/");
                if (destination.Length == 0)
                    return;
                var viewKey = destination[0];
                if (string.IsNullOrEmpty(viewKey))
                    return;
                var view = _editorUseCase.GetViewByKey(viewKey);
                if (view == null)
                    return;
                callback?.Invoke(viewKey);
            });
        }
    }
}