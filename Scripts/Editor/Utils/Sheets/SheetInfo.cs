// using System.Collections.Generic;
//
// namespace Plugins.ODDB.Scripts.Editor.Utils.Sheets
// {
//     public class SheetInfo
//     {
//         public string SheetName { get; set; }
//         public List<List<string>> Values { get; set; }
//         
//         public SheetInfo()
//         {
//             Values = new List<List<string>>();
//         }
//         
//         public SheetInfo(string sheetName)
//         {
//             SheetName = sheetName;
//             Values = new List<List<string>>();
//         }
//         
//         public SheetInfo(string sheetName, List<List<string>> values)
//         {
//             SheetName = sheetName;
//             Values = values ?? new List<List<string>>();
//         }
//         
//         /// <summary>
//         /// 행 수를 반환
//         /// </summary>
//         public int RowCount => Values?.Count ?? 0;
//         
//         /// <summary>
//         /// 시트가 비어있는지 확인
//         /// </summary>
//         public bool IsEmpty => RowCount == 0;
//         
//         /// <summary>
//         /// 시트 정보를 문자열로 표현
//         /// </summary>
//         public override string ToString()
//         {
//             return $"SheetInfo: {SheetName ?? "Unknown"} ({RowCount} rows)";
//         }
//     }
// }