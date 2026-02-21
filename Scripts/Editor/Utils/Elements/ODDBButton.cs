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
            style.flexShrink = 1;
            style.unityTextAlign = TextAnchor.MiddleCenter;
            
            // Ensure the button fits perfectly in its container
            style.width = Length.Percent(100);
            style.height = Length.Percent(100);
            style.marginTop = 0;
            style.marginBottom = 0;
            style.marginLeft = 0;
            style.marginRight = 0;
            style.paddingTop = 0;
            style.paddingBottom = 0;
            style.paddingLeft = 0;
            style.paddingRight = 0;
            style.borderTopWidth = 0;
            style.borderBottomWidth = 0;
            style.borderLeftWidth = 0;
            style.borderRightWidth = 0;
            
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