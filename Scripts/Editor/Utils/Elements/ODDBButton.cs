using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Plugins.ODDB.Scripts.Editor.Utils.Elements
{
    public class ODDBButton : Button
    {
        private List<Action<ClickEvent>> _callbacks = new();
        public ODDBButton()
        {
            style.flexGrow = 1;
            style.flexShrink = 0;
            style.unityTextAlign = TextAnchor.MiddleCenter;
            RegisterCallback<ClickEvent>(OnClickEvent);
        }

        private void OnClickEvent(ClickEvent evt)
        {
            foreach (var callback in _callbacks)
                callback.Invoke(evt);
        }
        
        public void AddOnClickCallback(Action<ClickEvent> callback)
        {
            if (!_callbacks.Contains(callback))
                _callbacks.Add(callback);
        }
        
        public void RemoveOnClickCallback(Action<ClickEvent> callback)
        {
            if (_callbacks.Contains(callback))
                _callbacks.Remove(callback);
        }
        
        public void ClearCallbacks()
        {
            _callbacks.Clear();
        }
    }
}