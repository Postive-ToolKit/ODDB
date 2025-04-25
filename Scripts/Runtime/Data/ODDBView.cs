using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
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
        public string Key { get; set; }
        public string Name { get; set; }
        public Type BindType { get; set; }
        public IODDBView ParentView { get; set; }

        protected string _parentViewKey;
        
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
            Key = new ODDBID().ID;
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
            var dtoBuilder = new ODDBViewDTOBuilder();
            try
            {
                var viewDto = dtoBuilder
                    .SetName(this)
                    .SetKey(this)
                    .SetTableMeta(this)
                    .SetBindType(this)
                    .SetParentView(this)
                    .Build();
                // convert view to xml
                var serializer = new XmlSerializer(typeof(ODDBViewDTO));
                using var stringWriter = new System.IO.StringWriter();
                serializer.Serialize(stringWriter, viewDto);
                data = stringWriter.ToString();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                data = null;
                return false;
            }
        }

        public virtual bool TryDeserialize(string data)
        {
            try
            {
                var serializer = new XmlSerializer(typeof(ODDBViewDTO));
                using var stringReader = new System.IO.StringReader(data);
                var viewDto = (ODDBViewDTO)serializer.Deserialize(stringReader);
                Key = viewDto.Key;
                Name = viewDto.Name;
                BindType = TryConvertBindType(viewDto.BindType, out var bindType) ? bindType : null;
                _parentViewKey = viewDto.ParentView;
                _tableMetas.Clear();
                _tableMetas.AddRange(viewDto.TableMetas);
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return false;
            }
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
            ParentView = database.GetViewByKey(_parentViewKey);
        }
        
        public void OnDatabaseDispose(ODDatabase database)
        {
            
        }
    }
}