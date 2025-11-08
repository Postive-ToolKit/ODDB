using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Plugins.ODDB.Scripts.Editor.Utils.Sheets;
using UnityEngine;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    [Serializable]
    public class GoogleSheet
    {
        [JsonIgnore]
        public string ODDBName
        {
            get
            {
                var splitUrl = Name.Split('_');
                if (splitUrl.Length < 1)
                    return string.Empty;
                return splitUrl[0];
            }
        }
        
        [JsonIgnore]
        public string ODDBID
        {
            get
            {
                var splitUrl = Name.Split('_');
                if (splitUrl.Length < 2)
                    return string.Empty;
                return splitUrl[1];
            }
        }
        
        public string Name;
        public string ID;
        public List<List<string>> Values;
        
        public SheetInfo ToSheetInfo()
        {
            var sheetInfo = new SheetInfo
            {
                Name = ODDBName,
                ID = ODDBID
            };
            if (Values == null || Values.Count == 0)
                return sheetInfo;
            sheetInfo.Values = Values;
            return sheetInfo;
        }
        
        public void FromSheetInfo(SheetInfo sheetInfo)
        {
            Name = $"{sheetInfo.Name}_{sheetInfo.ID}";
            ID = string.Empty;
            if (sheetInfo.Values == null || sheetInfo.Values.Count == 0)
            {
                Values = new List<List<string>>();
                return;
            }
            Values = sheetInfo.Values;
        }
    }
}