using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Plugins.ODDB.Scripts.Runtime.Data.Enum;
using Plugins.ODDB.Scripts.Runtime.Data.Interfaces;
using UnityEngine;

namespace TeamODD.ODDB.Scripts.Runtime.Data
{
    public class ODDBTable : IODDBHasUniqueKey, IODDBHasName, IODDBHasTableMeta, IODDBAvailableSerialize, IODDBHasBindType
    {
        private const char DELIMITER = ',';
        private const char QUOTE = '"';
        private const string DOUBLE_QUOTE = "\"\"";

        public string Key { get; set; }
        public string Name { get; set; }
        public Type BindType { get; set; }
        private readonly List<ODDBTableMeta> _tableMetas = new();
        private readonly List<ODDBRow> _rows = new();
        public List<ODDBTableMeta> TableMetas => _tableMetas;
        public IReadOnlyList<ODDBRow> ReadOnlyRows => _rows.AsReadOnly();
        
        public ODDBTable(IEnumerable<ODDBTableMeta> tableMetas = null)
        {
            if (tableMetas == null)
                return;
            _tableMetas.AddRange(tableMetas);
        }
        public void AddField(ODDBTableMeta tableMeta)
        {
            _tableMetas.Add(tableMeta);
            foreach (var row in _rows)
            {
                row.AddData(null);
            }
        }
        public void RemoveTableMeta(int index)
        {
            _tableMetas.RemoveAt(index);
            foreach (var row in _rows)
            {
                row.RemoveData(index);
            }
        }
        
        public void SwapTableMeta(int indexA, int indexB)
        {
            (_tableMetas[indexA], _tableMetas[indexB]) = (_tableMetas[indexB], _tableMetas[indexA]);
            foreach (var row in _rows)
            {
                row.SwapData(indexA, indexB);
            }
        }
        
        public void AddRow(ODDBRow row = null)
        {
            if (row == null)
            {
                row = new ODDBRow(_tableMetas.Count);
            }
            _rows.Add(row);
        }
        
        public void InsertRow(int index, ODDBRow row)
        {
            _rows.Insert(index, row);
        }
        
        public void SwapRow(int indexA, int indexB)
        {
            (_rows[indexA], _rows[indexB]) = (_rows[indexB], _rows[indexA]);
        }
        
        public void RemoveRow(int index)
        {
            _rows.RemoveAt(index);
        }
        
        public void ClearRows()
        {
            _rows.Clear();
        }
        
        public object GetValue(int rowIndex, int columnIndex)
        {
            if (rowIndex < 0 || rowIndex >= _rows.Count) {
                return null;
            }
            if (columnIndex < 0 || columnIndex >= _tableMetas.Count) {
                return null;
            }
            var row = _rows[rowIndex];
            return row.GetData(columnIndex);
        }
        public Type GetTypeOfColumn(int columnIndex)
        {
            if (columnIndex < 0 || columnIndex >= _tableMetas.Count) {
                return null;
            }
            var tableMeta = _tableMetas[columnIndex];
            return ODDBTypeMapper.GetEnumType(tableMeta.DataType);
        }

        #region Serialization
        public string Serialize()
        {
            var data = new StringBuilder();
            // Append header
            data.AppendLine("Key" + DELIMITER + string.Join(DELIMITER.ToString(), _tableMetas.ConvertAll(meta => EscapeCSV(meta.Name))));
            foreach (var row in _rows)
            {
                SerializeRow(row, data);
                data.AppendLine();
            }
            return data.ToString();
        }

        private void SerializeRow(ODDBRow row, StringBuilder builder)
        {
            builder.Append(EscapeCSV(row.Key));
            builder.Append(DELIMITER);
            for (int i = 0; i < _tableMetas.Count; i++)
            {
                var value = row.GetData(i)?.ToString() ?? string.Empty;
                builder.Append(EscapeCSV(value));
                
                if (i < _tableMetas.Count - 1)
                    builder.Append(DELIMITER);
            }
        }

        private string EscapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            
            bool needsQuotes = value.Contains(DELIMITER) || 
                             value.Contains(QUOTE) || 
                             value.Contains("\n");
            
            if (!needsQuotes) return value;

            return $"{QUOTE}{value.Replace(QUOTE.ToString(), DOUBLE_QUOTE)}{QUOTE}";
        }
        #endregion

        #region Deserialization
        public void Deserialize(string data)
        {
            _rows.Clear();
            if (string.IsNullOrEmpty(data)) return;

            var lines = NormalizeLineEndings(data).Split('\n');
            // remove headers
            if (lines.Length > 0)
            {
                lines = lines[1..];
            }
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                DeserializeLine(line);
            }
        }

        private void DeserializeLine(string line)
        {
            var values = ParseCSVLine(line);
            var key = values[0];
            values.RemoveAt(0);
            NormalizeValues(values);
            _rows.Add(new ODDBRow(key, values.ToArray()));
        }

        private string NormalizeLineEndings(string data)
        {
            return data.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private void NormalizeValues(List<string> values)
        {
            // 메타데이터 개수에 맞춰 값 조정
            while (values.Count < _tableMetas.Count)
            {
                values.Add(string.Empty);
            }
            if (values.Count > _tableMetas.Count)
            {
                values.RemoveRange(_tableMetas.Count, values.Count - _tableMetas.Count);
            }
        }

        private List<string> ParseCSVLine(string line)
        {
            var values = new List<string>();
            if (string.IsNullOrEmpty(line)) return values;

            var currentValue = new StringBuilder();
            bool inQuotes = false;
            
            for (int i = 0; i < line.Length; i++)
            {
                char currentChar = line[i];
                
                if (currentChar == QUOTE)
                {
                    if (inQuotes && HasNextDoubleQuote(line, i))
                    {
                        currentValue.Append(QUOTE);
                        i++; // Skip next quote
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (currentChar == DELIMITER && !inQuotes)
                {
                    values.Add(UnescapeCSV(currentValue.ToString()));
                    currentValue.Clear();
                }
                else
                {
                    currentValue.Append(currentChar);
                }
            }
            
            if (currentValue.Length > 0)
            {
                values.Add(UnescapeCSV(currentValue.ToString()));
            }

            return values;
        }

        private bool HasNextDoubleQuote(string line, int currentIndex)
        {
            return currentIndex + 1 < line.Length && line[currentIndex + 1] == QUOTE;
        }

        private string UnescapeCSV(string value)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            
            if (value.StartsWith(QUOTE.ToString()) && value.EndsWith(QUOTE.ToString()))
            {
                return value.Substring(1, value.Length - 2).Replace(DOUBLE_QUOTE, QUOTE.ToString());
            }
            
            return value;
        }
        #endregion
    }
}