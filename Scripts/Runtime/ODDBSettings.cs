using TeamODD.ODDB.Runtime.Settings.Attributes;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Settings
{
    public class ODDBSettings : ScriptableObject
    {
        public static readonly string BASE_PATH = Application.dataPath + "/Resources";
        public bool IsInitialized => _isInitialized;
        public string Path {
            get => Application.dataPath + DBPath;
            set{
                _dbPath = value.Replace(Application.dataPath,"");
                var curPath = value.Replace(BASE_PATH + "/", "");
                _pathFromResources = curPath;
                _isInitialized = true;
            }
        }
        public string DBPath => _dbPath;
        public string PathFromResources => _pathFromResources;
        public string DBName {
            get => _dbName;
        }
        [HideInInspector] private bool _isInitialized = false;
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