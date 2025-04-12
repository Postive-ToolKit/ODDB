using TeamODD.ODDB.Runtime.Settings.Attributes;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Settings
{
    public class ODDBSettings : ScriptableObject
    {
        public static readonly string BASE_PATH = Application.dataPath + "/Resources/";
        public string Path {
            get => Application.dataPath + _dbPath;
            set{
                _dbPath = value.Replace(Application.dataPath,"");
                var curPath = value.Replace(BASE_PATH, "");
                _pathFromResources = curPath;
            }
        }
        public string PathFromResources => _pathFromResources;
        public string DBName {
            get => _dbName;
        }

        [ODDBPathSelector(true)]
        [SerializeField] private string _dbPath;
        [SerializeField] private string _pathFromResources;
        [SerializeField] private string _dbName = "ODDB.db";
        private void OnValidate()
        {
            if(!string.IsNullOrEmpty(_dbPath))
            {
                Path = _dbPath;
            }
        }
    }
}