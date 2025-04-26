using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using TeamODD.ODDB.Runtime.Data.DTO;
using TeamODD.ODDB.Runtime.Data.DTO.Builders;
using TeamODD.ODDB.Runtime.Data.Interfaces;
using TeamODD.ODDB.Runtime.Utils;
using Unity.Plastic.Newtonsoft.Json;
using UnityEngine;

namespace TeamODD.ODDB.Runtime.Data
{
    public sealed class ODDBTable : ODDBView, IODDBAvailableSerialize
    {
        private const char DELIMITER = ',';
        private const char QUOTE = '"';
        private const string DOUBLE_QUOTE = "\"\"";
        private readonly List<ODDBRow> _rows = new();

        public IReadOnlyList<ODDBRow> ReadOnlyRows => _rows.AsReadOnly();

        public ODDBTable()
        {
            
        }
        
        public ODDBTable(IEnumerable<ODDBTableMeta> tableMetas = null) : base(tableMetas)
        {
            
        }

        protected override void OnAddTableMeta(ODDBTableMeta tableMeta)
        {
            foreach (var row in _rows) {
                row.AddData(null);
            }
        }

        protected override void OnRemoveTableMeta(int index)
        {
            foreach (var row in _rows) {
                row.RemoveData(index);
            }
        }

        protected override void OnSwapTableMeta(int indexA, int indexB)
        {
            foreach (var row in _rows) {
                row.SwapData(indexA, indexB);
            }
        }
        
        public void AddRow(ODDBRow row = null)
        {
            if (row == null)
            {
                row = new ODDBRow(TableMetas.Count);
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
            if (columnIndex < 0 || columnIndex >= TableMetas.Count) {
                return null;
            }
            var row = _rows[rowIndex];
            return row.GetData(columnIndex);
        }

        #region Serialization

        public override bool TrySerialize(out string data)
        {
            try
            {
                var dtoBuilder = new ODDBTableDTOBuilder();
                var tableDto = dtoBuilder
                    .SetSerialization(this)
                    .SetName(this)
                    .SetKey(this)
                    .SetTableMeta(this)
                    .SetBindType(this)
                    .SetParentView(this)
                    .Build();
                // serialize to json
                data = JsonConvert.SerializeObject(tableDto, Formatting.Indented);
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                data = null;
                return false;
            }
        }

        public string Serialize()
        {
            var data = new StringBuilder();
            // Append header
            data.AppendLine("Key" + DELIMITER + string.Join(DELIMITER.ToString(), TableMetas.ConvertAll(meta => EscapeCSV(meta.Name))));
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
            // data cut off
            for (int i = 0; i < TableMetas.Count; i++)
            {
                var value = row.GetData(i)?.ToString() ?? string.Empty;
                builder.Append(EscapeCSV(value));
                
                if (i < TableMetas.Count - 1)
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
        public override bool TryDeserialize(string data)
        {
            var tableDto = JsonUtility.FromJson<ODDBTableDTO>(data);
            if (tableDto == null)
                return false;
            Key = new ODDBID(tableDto.Key);
            Name = tableDto.Name;
            BindType = TryConvertBindType(tableDto.BindType, out var bindType) ? bindType : null;
            _parentViewKey = new ODDBID(tableDto.ParentView);
            ScopedTableMetas.Clear();
            ScopedTableMetas.AddRange(tableDto.TableMetas);
            Deserialize(tableDto.Data);
            ODDBConverter.OnDatabaseCreated += OnDatabaseInitialize;
            return true;
        }

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
            //NormalizeValues(values);
            _rows.Add(new ODDBRow(new ODDBID(key), values.ToArray()));
        }

        private string NormalizeLineEndings(string data)
        {
            return data.Replace("\r\n", "\n").Replace("\r", "\n");
        }

        private void NormalizeValues(List<string> values)
        {
            while (values.Count < TableMetas.Count)
            {
                values.Add(string.Empty);
            }
            if (values.Count > TableMetas.Count)
            {
                values.RemoveRange(TableMetas.Count, values.Count - TableMetas.Count);
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
            else
            {
                values.Add(string.Empty);
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