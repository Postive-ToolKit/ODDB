using System;
using System.Text;
using Newtonsoft.Json;

namespace Plugins.ODDB.Scripts.Editor.Utils
{
    /// <summary>
    /// Google Sheets API 응답 데이터 구조 (Newtonsoft.Json 사용)
    /// </summary>
    [Serializable]
    public class GoogleSheetsResponse
    {
        [JsonIgnore]
        public string SheetName { get; set; }
        
        [JsonProperty("range")]
        public string Range { get; set; }
        
        [JsonProperty("majorDimension")]
        public string MajorDimension { get; set; }
        
        [JsonProperty("values")]
        public string[][] Values { get; set; }
        
        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Range: {Range}");
            sb.AppendLine($"MajorDimension: {MajorDimension}");
            sb.AppendLine("Values:");
            if (Values == null)
                return sb + "  (null)";
            foreach (var row in Values)
                sb.AppendLine($"  - {string.Join(", ", row)}");

            return sb.ToString();
        }
    }
}