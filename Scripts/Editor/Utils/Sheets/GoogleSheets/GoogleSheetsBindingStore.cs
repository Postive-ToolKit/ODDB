using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    internal interface IGoogleSheetsBindingStore
    {
        bool TryGet(string spreadsheetId, string tableId, out GoogleSheetBinding binding);
        void Upsert(string spreadsheetId, string tableId, int sheetId, string lastKnownTitle);
    }

    [Serializable]
    internal sealed class GoogleSheetBinding
    {
        public string tableId = string.Empty;
        public int sheetId;
        public string lastKnownTitle = string.Empty;

        public GoogleSheetBinding Clone()
        {
            return new GoogleSheetBinding
            {
                tableId = tableId,
                sheetId = sheetId,
                lastKnownTitle = lastKnownTitle
            };
        }
    }

    internal sealed class GoogleSheetsBindingStore : IGoogleSheetsBindingStore
    {
        internal const string DefaultFilePath = "UserSettings/ODDBGoogleSheetsBindings.json";
        private const int CurrentVersion = 1;

        private readonly string _filePath;
        private readonly GoogleSheetsBindingPayload _payload;

        private GoogleSheetsBindingStore(string filePath, GoogleSheetsBindingPayload payload)
        {
            _filePath = filePath;
            _payload = payload ?? CreateEmptyPayload();
        }

        public static GoogleSheetsBindingStore LoadDefault()
        {
            return Load(DefaultFilePath);
        }

        internal static GoogleSheetsBindingStore Load(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("A binding store path is required.", nameof(filePath));

            try
            {
                if (!File.Exists(filePath))
                    return new GoogleSheetsBindingStore(filePath, CreateEmptyPayload());

                var json = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(json))
                    return new GoogleSheetsBindingStore(filePath, CreateEmptyPayload());

                var payload = JsonUtility.FromJson<GoogleSheetsBindingPayload>(json);
                ValidateAndNormalize(payload);
                return new GoogleSheetsBindingStore(filePath, payload);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[ODDB Google Sheets] Failed to read local sheet bindings: {e.Message}. Bindings will be rebuilt from the spreadsheet metadata.");
                return new GoogleSheetsBindingStore(filePath, CreateEmptyPayload());
            }
        }

        public bool TryGet(string spreadsheetId, string tableId, out GoogleSheetBinding binding)
        {
            binding = null;
            var spreadsheet = FindSpreadsheet(spreadsheetId);
            var found = spreadsheet?.tables?.FirstOrDefault(item =>
                item != null && string.Equals(item.tableId, tableId, StringComparison.Ordinal));
            if (found == null)
                return false;

            binding = found.Clone();
            return true;
        }

        public void Upsert(string spreadsheetId, string tableId, int sheetId, string lastKnownTitle)
        {
            spreadsheetId = RequireValue(spreadsheetId, nameof(spreadsheetId));
            tableId = RequireValue(tableId, nameof(tableId));
            if (sheetId < 0)
                throw new ArgumentOutOfRangeException(nameof(sheetId));

            var spreadsheet = FindSpreadsheet(spreadsheetId);
            if (spreadsheet == null)
            {
                spreadsheet = new GoogleSpreadsheetBinding
                {
                    spreadsheetId = spreadsheetId,
                    tables = new List<GoogleSheetBinding>()
                };
                _payload.spreadsheets.Add(spreadsheet);
            }

            var conflicting = spreadsheet.tables.FirstOrDefault(item =>
                item != null
                && item.sheetId == sheetId
                && !string.Equals(item.tableId, tableId, StringComparison.Ordinal));
            if (conflicting != null)
            {
                throw new InvalidOperationException(
                    $"Google Sheet ID '{sheetId}' is already bound to ODDB table '{conflicting.tableId}'.");
            }

            var existing = spreadsheet.tables.FirstOrDefault(item =>
                item != null && string.Equals(item.tableId, tableId, StringComparison.Ordinal));
            if (existing == null)
            {
                existing = new GoogleSheetBinding { tableId = tableId };
                spreadsheet.tables.Add(existing);
            }
            else if (existing.sheetId == sheetId
                && string.Equals(existing.lastKnownTitle ?? string.Empty, lastKnownTitle ?? string.Empty, StringComparison.Ordinal))
            {
                return;
            }

            existing.sheetId = sheetId;
            existing.lastKnownTitle = lastKnownTitle ?? string.Empty;
            Save();
        }

        private GoogleSpreadsheetBinding FindSpreadsheet(string spreadsheetId)
        {
            if (string.IsNullOrWhiteSpace(spreadsheetId))
                return null;
            return _payload.spreadsheets.FirstOrDefault(item =>
                item != null && string.Equals(item.spreadsheetId, spreadsheetId, StringComparison.Ordinal));
        }

        private void Save()
        {
            ValidateAndNormalize(_payload);
            var directory = Path.GetDirectoryName(_filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            var temporaryPath = _filePath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(_payload, true));
            try
            {
                if (File.Exists(_filePath))
                    File.Replace(temporaryPath, _filePath, null);
                else
                    File.Move(temporaryPath, _filePath);
            }
            catch (PlatformNotSupportedException)
            {
                File.Copy(temporaryPath, _filePath, true);
                File.Delete(temporaryPath);
            }
        }

        private static GoogleSheetsBindingPayload CreateEmptyPayload()
        {
            return new GoogleSheetsBindingPayload
            {
                version = CurrentVersion,
                spreadsheets = new List<GoogleSpreadsheetBinding>()
            };
        }

        private static void ValidateAndNormalize(GoogleSheetsBindingPayload payload)
        {
            if (payload == null)
                throw new InvalidDataException("The binding file is empty or invalid.");
            if (payload.version != CurrentVersion)
                throw new InvalidDataException($"Unsupported Google Sheets binding version '{payload.version}'.");

            payload.spreadsheets = payload.spreadsheets ?? new List<GoogleSpreadsheetBinding>();
            var spreadsheetIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var spreadsheet in payload.spreadsheets)
            {
                if (spreadsheet == null || string.IsNullOrWhiteSpace(spreadsheet.spreadsheetId))
                    throw new InvalidDataException("A binding has no spreadsheet ID.");
                if (!spreadsheetIds.Add(spreadsheet.spreadsheetId))
                    throw new InvalidDataException($"Spreadsheet '{spreadsheet.spreadsheetId}' appears more than once in the binding file.");

                spreadsheet.tables = spreadsheet.tables ?? new List<GoogleSheetBinding>();
                var tableIds = new HashSet<string>(StringComparer.Ordinal);
                var sheetIds = new HashSet<int>();
                foreach (var table in spreadsheet.tables)
                {
                    if (table == null || string.IsNullOrWhiteSpace(table.tableId) || table.sheetId < 0)
                        throw new InvalidDataException($"Spreadsheet '{spreadsheet.spreadsheetId}' contains an invalid table binding.");
                    if (!tableIds.Add(table.tableId))
                        throw new InvalidDataException($"Table '{table.tableId}' appears more than once in the binding file.");
                    if (!sheetIds.Add(table.sheetId))
                        throw new InvalidDataException($"Sheet ID '{table.sheetId}' is bound more than once in spreadsheet '{spreadsheet.spreadsheetId}'.");
                    table.lastKnownTitle = table.lastKnownTitle ?? string.Empty;
                }
            }
        }

        private static string RequireValue(string value, string parameterName)
        {
            value = value?.Trim();
            if (string.IsNullOrEmpty(value))
                throw new ArgumentException("A non-empty value is required.", parameterName);
            return value;
        }

        [Serializable]
        private sealed class GoogleSheetsBindingPayload
        {
            public int version = CurrentVersion;
            public List<GoogleSpreadsheetBinding> spreadsheets = new List<GoogleSpreadsheetBinding>();
        }

        [Serializable]
        private sealed class GoogleSpreadsheetBinding
        {
            public string spreadsheetId = string.Empty;
            public List<GoogleSheetBinding> tables = new List<GoogleSheetBinding>();
        }
    }
}
