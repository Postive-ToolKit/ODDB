using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;
using TeamODD.ODDB.Runtime.Data.DTO;
using TeamODD.ODDB.Runtime.Data.DTO.Builders;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Utils;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Data
{
    public class ODDBView : IODDBView
    {
        private const string DEFAULT_NAME = "Default Name";
        public ODDBID Key { get; set; }
        public string Name { get; set; }
        public Type BindType { get; set; }
        public IODDBView ParentView { get; set; }

        protected ODDBID _parentViewKey;
        
        public List<ODDBTableMeta> TableMetas
        {
            get
            {
                if (ParentView == null)
                    return _tableMetas;
                var parentTableMetas = ParentView.TableMetas;
                var tableMetas = new List<ODDBTableMeta>();
                tableMetas.AddRange(parentTableMetas);
                tableMetas.AddRange(_tableMetas);
                return tableMetas;
            }
        }
        public List<ODDBTableMeta> ScopedTableMetas => _tableMetas;
        private readonly List<ODDBTableMeta> _tableMetas = new();
        
        public ODDBView()
        {
            Key = new ODDBID();
            Name = DEFAULT_NAME;
        }
        public ODDBView(IEnumerable<ODDBTableMeta> tableMetas = null)
        {
            if (tableMetas == null)
                return;
            _tableMetas.AddRange(tableMetas);
        }
        
        public void AddField(ODDBTableMeta tableMeta)
        {
            _tableMetas.Add(tableMeta);
            OnAddTableMeta(tableMeta);
        }
        
        public void RemoveTableMeta(int index)
        {
            if (!IsScopedMeta(index)) {
                Debug.LogError($"Index {index} is out of range for this view.");
                return;
            }
            _tableMetas.RemoveAt(index);
            OnRemoveTableMeta(index);
        }
        
        public void SwapTableMeta(int indexA, int indexB)
        {
            if (!IsScopedMeta(indexA) || !IsScopedMeta(indexB))
            {
                Debug.LogError($"Index {indexA} or {indexB} is out of range for this view.");
                return;
            }
                
            (_tableMetas[indexA], _tableMetas[indexB]) = (_tableMetas[indexB], _tableMetas[indexA]);
            OnSwapTableMeta(indexA, indexB);
        }

        public bool IsScopedMeta(int index)
        {
            if (ParentView == null)
            {
                return index >= 0 && index < _tableMetas.Count;
            }
            var parentTableMetas = ParentView.TableMetas;
            return index >= parentTableMetas.Count && index < parentTableMetas.Count + _tableMetas.Count;
        }

        protected virtual void OnAddTableMeta(ODDBTableMeta tableMeta) { }
        protected virtual void OnRemoveTableMeta(int index) { }
        protected virtual void OnSwapTableMeta(int indexA, int indexB) { }
        public virtual bool TrySerialize(out string data)
        {
            data = null;
            var dtoBuilder = new ODDBViewDTOBuilder();
            var viewDto = dtoBuilder
                .SetName(this)
                .SetKey(this)
                .SetTableMeta(this)
                .SetBindType(this)
                .SetParentView(this)
                .Build();
            if (viewDto == null)
                return false;
            // serialize to json
            data = JsonConvert.SerializeObject(viewDto, Formatting.Indented);
            return true;
        }

        public virtual bool TryDeserialize(string data)
        {
            var viewDto = JsonConvert.DeserializeObject<ODDBViewDTO>(data);
            if (viewDto == null)
                return false;
            Key = new ODDBID(viewDto.Key);
            Name = viewDto.Name;
            BindType = TryConvertBindType(viewDto.BindType, out var bindType) ? bindType : null;
            _parentViewKey = new ODDBID(viewDto.ParentView);
            ScopedTableMetas.Clear();
            ScopedTableMetas.AddRange(viewDto.TableMetas);
            ODDBConverter.OnDatabaseCreated += OnDatabaseInitialize;
            return true;
        }
        
        protected bool TryConvertBindType(string bindType, out Type type)
        {
            type = null;
            if (string.IsNullOrEmpty(bindType))
                return true;

            // Quick check for common types
            type = Type.GetType(bindType);
            if (type != null)
            {
                if (!type.IsSubclassOf(typeof(ODDBEntity)))
                {
                    Debug.LogError($"[ODDBImporter] '{bindType}' is not a subclass of ODDBEntity.");
                    type = null;
                    return false;
                }

                return true;
            }

            Debug.Log("[ODDBImporter] Cannot find bind type: " + bindType +
                      " in current assembly, searching all assemblies...");
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try
                {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e)
                {
                    types = e.Types.Where(t => t != null).ToArray();
                }

                foreach (var t in types)
                    if (t.FullName == bindType && !t.IsAbstract && t.IsSubclassOf(typeof(ODDBEntity)))
                    {
                        type = t;
                        return true;
                    }
            }

            Debug.LogError($"[ODDBImporter] Cannot find or convert bind type: '{bindType}'");
            return false;
        }

        public void OnDatabaseInitialize(ODDatabase database)
        {
            ParentView = database.Views.Read(_parentViewKey);
            ODDBConverter.OnDatabaseCreated -= OnDatabaseInitialize;
        }
    }
}