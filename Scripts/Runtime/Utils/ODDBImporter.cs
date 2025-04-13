using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Plugins.ODDB.Scripts.Runtime.Data.DTO;
using TeamODD.ODDB.Runtime.Entities;
using TeamODD.ODDB.Runtime.Settings.Data;
using TeamODD.ODDB.Scripts.Runtime.Data;
using UnityEngine;

namespace TeamODD.ODDB.Runtime
{
    public class ODDBImporter
    {
        public bool TryCreateDatabase(ODDatabaseDTO databaseDto, out ODDatabase database)
        {
            try
            {
                database = new ODDatabase();
                var convertTargets = databaseDto.Tables;
                foreach (var target in convertTargets)
                {
                    if(TryConvertTable(target, out var table))
                    {
                        database.Tables.Add(table);
                    }
                }
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("ODDBImporter.TryCreateDatabase cannot create database from dto : " + e.Message);
                database = null;
                return false;
            }
        }

        private bool TryConvertTable(ODDBTableDTO convertTargets, out ODDBTable table)
        {
            try
            {
                table = new ODDBTable(convertTargets.TableMetas);
                table.Name = convertTargets.Name;
                table.Key = convertTargets.Key;
                // convert convertTargets.Data to TextStream
                var data = convertTargets.Data;
                table.Deserialize(data);
                // use reflection to get the type of the bind
                if (TryConvertBindType(convertTargets.BindType, out var bindType)) {
                    table.BindType = bindType;
                }
                else
                {
                    Debug.Log("ODDBImporter.TryConvertTable cannot convert bind type : " + convertTargets.BindType);
                }
                return true;
            }
            catch (Exception e)
            {
                table = null;
                return false;
            }
        }
        private bool TryConvertBindType(string bindType, out Type type)
        {
            type = null;
            if (string.IsNullOrEmpty(bindType))
                return true;
            
            // Quick check for common types
            type = Type.GetType(bindType);
            if (type != null) {
                if (!type.IsSubclassOf(typeof(ODDBEntity))) {
                    Debug.LogError($"[ODDBImporter] '{bindType}' is not a subclass of ODDBEntity.");
                    type = null;
                    return false;
                }
                return true;
            }
            Debug.Log("[ODDBImporter] Cannot find bind type: " + bindType + " in current assembly, searching all assemblies...");
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()) {
                Type[] types;
                try {
                    types = assembly.GetTypes();
                }
                catch (ReflectionTypeLoadException e) {
                    types = e.Types.Where(t => t != null).ToArray();
                }
                foreach (var t in types) {
                    if (t.FullName == bindType && !t.IsAbstract && t.IsSubclassOf(typeof(ODDBEntity))) {
                        type = t;
                        return true;
                    }
                }
            }
            Debug.LogError($"[ODDBImporter] Cannot find or convert bind type: '{bindType}'");
            return false;
        }

    }
}