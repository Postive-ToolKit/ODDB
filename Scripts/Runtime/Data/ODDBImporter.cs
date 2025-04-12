using System;
using System.Collections.Generic;
using Plugins.ODDB.Scripts.Runtime.Data.DTO;
using TeamODD.ODDB.Scripts.Runtime.Data;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Settings.Data
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
                return true;
            }
            catch (Exception e)
            {
                table = null;
                return false;
            }
        }
    }
}