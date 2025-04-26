using System;
using System.Linq;
using System.Reflection;
using TeamODD.ODDB.Runtime.Data;
using TeamODD.ODDB.Runtime.Data.DTO;
using TeamODD.ODDB.Runtime.Entities;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Utils
{
    public class ODDBImporter
    {
        public ODDatabase CreateDatabase(string data)
        {
            // var database = new ODDatabase();
            //
            // var tableDTOs = databaseDto.Tables;
            // foreach (var tableDto in tableDTOs)
            //     if (TryConvertTable(tableDto, out var table))
            //         database.AddTable(table);
            //
            // var viewDTOs = databaseDto.Views;
            // foreach (var viewDto in viewDTOs)
            //     if (TryConvertView(viewDto, out var view))
            //         database.AddView(view);
            //
            //
            //
            // foreach (var tableDto in tableDTOs)
            // {
            //     if (string.IsNullOrEmpty(tableDto.ParentView))
            //         continue;
            //     var targetView = database.GetViewByKey(tableDto.Key);
            //     var parentView = database.GetViewByKey(tableDto.ParentView);
            //     if (targetView == null || parentView == null)
            //     {
            //         Debug.LogError("ODDBImporter.TryCreateDatabase cannot find view : " + tableDto.ParentView);
            //         continue;
            //     }
            //
            //     targetView.ParentView = parentView;
            // }
            //
            // foreach (var viewDto in viewDTOs)
            // {
            //     if (string.IsNullOrEmpty(viewDto.ParentView))
            //         continue;
            //     var targetView = database.GetViewByKey(viewDto.Key);
            //     var parentView = database.GetViewByKey(viewDto.ParentView);
            //     if (targetView == null || parentView == null)
            //     {
            //         Debug.LogError("ODDBImporter.TryCreateDatabase cannot find view : " + viewDto.ParentView);
            //         continue;
            //     }
            //
            //     targetView.ParentView = parentView;
            // }
            var database = new ODDatabase();
            database.TryDeserialize(data);
            return database;
        }

        private bool TryConvertView(ODDBViewDTO viewDto, out ODDBView view)
        {
            try
            {
                view = new ODDBView(viewDto.TableMetas);
                view.Name = viewDto.Name;
                view.Key = new ODDBID(viewDto.Key);
                if (TryConvertBindType(viewDto.BindType, out var bindType))
                    view.BindType = bindType;
                else
                    Debug.Log("ODDBImporter.TryConvertView cannot convert bind type : " + viewDto.BindType);

                return true;
            }
            catch (Exception e)
            {
                view = null;
                Debug.LogError("ODDBImporter.TryConvertView cannot convert view to dto : " + e.Message);
                return false;
            }
        }

        private bool TryConvertTable(ODDBTableDTO convertTargets, out ODDBTable table)
        {
            try
            {
                table = new ODDBTable(convertTargets.TableMetas);
                table.Name = convertTargets.Name;
                table.Key = new ODDBID(convertTargets.Key);
                // convert convertTargets.Data to TextStream
                var data = convertTargets.Data;
                table.Deserialize(data);
                // use reflection to get the type of the bind
                if (TryConvertBindType(convertTargets.BindType, out var bindType))
                    table.BindType = bindType;
                else
                    Debug.Log("ODDBImporter.TryConvertTable cannot convert bind type : " + convertTargets.BindType);
                return true;
            }
            catch (Exception e)
            {
                table = null;
                Debug.LogError("ODDBImporter.TryConvertTable cannot convert table : " + e.Message);
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
    }
}