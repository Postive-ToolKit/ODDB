using System;

namespace Plugins.ODDB.Scripts.Editor.Utils
{
    [Serializable]
    public class GoogleSheets
    {
        public SheetProperties properties;
    }
    
    /// <summary>
    /// 스프레드시트 메타데이터 응답 구조
    /// </summary>
    [Serializable]
    public class SpreadsheetMetadataResponse
    {
        public GoogleSheets[] sheets;
    }
    
    [Serializable]
    public class SheetProperties
    {
        public string title;
    }
}