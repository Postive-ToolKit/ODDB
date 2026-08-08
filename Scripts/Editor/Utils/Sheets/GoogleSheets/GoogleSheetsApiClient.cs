using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Google;
using Google.Apis.Sheets.v4;
using Google.Apis.Sheets.v4.Data;

namespace TeamODD.ODDB.Editors.Utils.Sheets.GoogleSheets
{
    internal interface IGoogleSheetsApiClient : IDisposable
    {
        Task<List<GoogleSheetSnapshot>> ReadSpreadsheetAsync(string spreadsheetId, CancellationToken ct);
        Task<List<GoogleSheetSnapshot>> CreateSheetsAsync(
            string spreadsheetId,
            IReadOnlyList<GoogleSheetCreateSpec> sheets,
            CancellationToken ct);
        Task<BatchUpdateSpreadsheetResponse> BatchUpdateAsync(
            string spreadsheetId,
            BatchUpdateSpreadsheetRequest body,
            CancellationToken ct);
        Task BatchWriteValuesAsync(string spreadsheetId, IReadOnlyList<ValueRange> ranges, CancellationToken ct);
        Task BatchClearValuesAsync(string spreadsheetId, IReadOnlyList<string> ranges, CancellationToken ct);
        Task<List<List<List<string>>>> ReadRangesAsync(
            string spreadsheetId,
            IReadOnlyList<string> ranges,
            CancellationToken ct);
    }

    internal sealed class GoogleSheetsApiClient : IGoogleSheetsApiClient
    {
        private const int MaxRangesPerBatch = 5000;
        private const int MaxBatchPayloadCharacters = 1500000;
        private const int MaxNoteRangesPerGet = 200;
        private const int MaxNoteRangeCharacters = 12000;
        private readonly SheetsService _service;

        private GoogleSheetsApiClient(SheetsService service)
        {
            _service = service ?? throw new ArgumentNullException(nameof(service));
        }

        public static async Task<GoogleSheetsApiClient> CreateAsync(CancellationToken ct)
        {
            return new GoogleSheetsApiClient(await GoogleSheetsServiceFactory.CreateAsync(ct));
        }

        public async Task<List<GoogleSheetSnapshot>> ReadSpreadsheetAsync(
            string spreadsheetId,
            CancellationToken ct)
        {
            var spreadsheet = await ExecuteWithRetryAsync(() =>
            {
                var request = _service.Spreadsheets.Get(spreadsheetId);
                request.IncludeGridData = false;
                request.Fields = "spreadsheetId,sheets(properties(sheetId,title,gridProperties),developerMetadata(metadataId,metadataKey,metadataValue,visibility,location))";
                return request.ExecuteAsync(ct);
            }, ct);

            var snapshots = new List<GoogleSheetSnapshot>();
            if (spreadsheet.Sheets == null)
                return snapshots;

            foreach (var sheet in spreadsheet.Sheets)
            {
                var properties = sheet.Properties;
                if (properties?.SheetId == null)
                    continue;

                var snapshot = new GoogleSheetSnapshot
                {
                    SheetId = properties.SheetId.Value,
                    Title = properties.Title ?? string.Empty,
                    ColumnCount = properties.GridProperties?.ColumnCount ?? 0,
                    RowCount = properties.GridProperties?.RowCount ?? 0
                };
                ReadMetadata(sheet.DeveloperMetadata, snapshot);
                snapshots.Add(snapshot);
            }

            if (snapshots.Count == 0)
                return snapshots;

            var ranges = snapshots.Select(snapshot => QuoteTitle(snapshot.Title)).ToList();
            var values = await ExecuteWithRetryAsync(() =>
            {
                var request = _service.Spreadsheets.Values.BatchGet(spreadsheetId);
                request.Ranges = ranges;
                request.MajorDimension = SpreadsheetsResource.ValuesResource.BatchGetRequest.MajorDimensionEnum.ROWS;
                return request.ExecuteAsync(ct);
            }, ct);

            for (var index = 0; index < snapshots.Count; index++)
            {
                var range = values.ValueRanges != null && index < values.ValueRanges.Count
                    ? values.ValueRanges[index]
                    : null;
                snapshots[index].Values = ToStringRows(range?.Values);
                if (string.IsNullOrEmpty(snapshots[index].TableId))
                    snapshots[index].TableId = TryReadLegacyTableId(snapshots[index].Title);
            }

            await ReadCellNotesAsync(spreadsheetId, snapshots, ct);

            return snapshots;
        }

        private async Task ReadCellNotesAsync(
            string spreadsheetId,
            IReadOnlyList<GoogleSheetSnapshot> snapshots,
            CancellationToken ct)
        {
            var ranges = new List<string>();
            foreach (var snapshot in snapshots)
            {
                for (var rowIndex = 0; rowIndex < snapshot.Values.Count; rowIndex++)
                {
                    var row = snapshot.Values[rowIndex];
                    if (row == null
                        || row.Count <= 2
                        || !string.Equals(row[0], SheetConfig.ROW_TYPE_MARKER, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var rowNumber = rowIndex + 1;
                    // BatchGet trims trailing empty values. Every View type has a
                    // non-empty value, so the returned row width is sufficient and
                    // avoids scanning a potentially very wide designer grid.
                    var lastColumn = row.Count;
                    ranges.Add(
                        $"{QuoteTitle(snapshot.Title)}!C{rowNumber}:{ToColumnName(lastColumn)}{rowNumber}");
                }
            }

            if (ranges.Count == 0)
                return;

            foreach (var rangeChunk in ChunkNoteRanges(ranges))
            {
                var spreadsheet = await ExecuteWithRetryAsync(() =>
                {
                    var request = _service.Spreadsheets.Get(spreadsheetId);
                    request.IncludeGridData = true;
                    request.Ranges = rangeChunk;
                    request.Fields = "sheets(properties(sheetId),data(startRow,startColumn,rowData(values(note))))";
                    return request.ExecuteAsync(ct);
                }, ct);

                ApplyCellNotes(snapshots, spreadsheet);
            }
        }

        private static IEnumerable<List<string>> ChunkNoteRanges(IReadOnlyList<string> ranges)
        {
            var current = new List<string>();
            var characterCount = 0;
            foreach (var range in ranges)
            {
                var length = range?.Length ?? 0;
                if (current.Count > 0
                    && (current.Count >= MaxNoteRangesPerGet
                        || characterCount + length > MaxNoteRangeCharacters))
                {
                    yield return current;
                    current = new List<string>();
                    characterCount = 0;
                }

                current.Add(range);
                characterCount += length;
            }

            if (current.Count > 0)
                yield return current;
        }

        internal static void ApplyCellNotes(
            IReadOnlyList<GoogleSheetSnapshot> snapshots,
            Spreadsheet spreadsheet)
        {
            if (snapshots == null || spreadsheet == null)
                return;
            var bySheetId = snapshots.ToDictionary(snapshot => snapshot.SheetId);
            foreach (var sheet in spreadsheet.Sheets ?? Array.Empty<Sheet>())
            {
                var sheetId = sheet.Properties?.SheetId;
                if (sheetId == null || !bySheetId.TryGetValue(sheetId.Value, out var snapshot))
                    continue;

                foreach (var data in sheet.Data ?? Array.Empty<GridData>())
                {
                    var startRow = data.StartRow ?? 0;
                    var startColumn = data.StartColumn ?? 0;
                    var rows = data.RowData ?? Array.Empty<RowData>();
                    for (var rowOffset = 0; rowOffset < rows.Count; rowOffset++)
                    {
                        var cells = rows[rowOffset]?.Values ?? Array.Empty<CellData>();
                        for (var columnOffset = 0; columnOffset < cells.Count; columnOffset++)
                        {
                            var note = cells[columnOffset]?.Note;
                            if (!string.IsNullOrEmpty(note))
                            {
                                snapshot.CellNotes[new SheetCellAddress(
                                    startRow + rowOffset,
                                    startColumn + columnOffset)] = note;
                            }
                        }
                    }
                }
            }
        }

        public async Task<List<GoogleSheetSnapshot>> CreateSheetsAsync(
            string spreadsheetId,
            IReadOnlyList<GoogleSheetCreateSpec> sheets,
            CancellationToken ct)
        {
            var result = new List<GoogleSheetSnapshot>();
            if (sheets == null || sheets.Count == 0)
                return result;

            var body = new BatchUpdateSpreadsheetRequest
            {
                Requests = sheets.Select(spec => new Request
                {
                    AddSheet = new AddSheetRequest
                    {
                        Properties = new SheetProperties
                        {
                            Title = spec.Title,
                            GridProperties = new GridProperties
                            {
                                ColumnCount = Math.Max(1, spec.ColumnCount),
                                RowCount = Math.Max(1, spec.RowCount)
                            }
                        }
                    }
                }).ToList()
            };

            var response = await BatchUpdateAsync(spreadsheetId, body, ct);
            for (var index = 0; index < sheets.Count; index++)
            {
                var spec = sheets[index];
                var properties = response.Replies != null && index < response.Replies.Count
                    ? response.Replies[index]?.AddSheet?.Properties
                    : null;
                if (properties?.SheetId == null)
                    throw new InvalidOperationException($"Google Sheets did not return an ID for newly created sheet '{spec.Title}'.");

                result.Add(new GoogleSheetSnapshot
                {
                    SheetId = properties.SheetId.Value,
                    Title = properties.Title ?? spec.Title,
                    ColumnCount = properties.GridProperties?.ColumnCount ?? Math.Max(1, spec.ColumnCount),
                    RowCount = properties.GridProperties?.RowCount ?? Math.Max(1, spec.RowCount),
                    Values = new List<List<string>>()
                });
            }

            return result;
        }

        private static string ToColumnName(int oneBasedColumn)
        {
            if (oneBasedColumn <= 0)
                throw new ArgumentOutOfRangeException(nameof(oneBasedColumn));

            var result = string.Empty;
            var value = oneBasedColumn;
            while (value > 0)
            {
                value--;
                result = (char)('A' + value % 26) + result;
                value /= 26;
            }
            return result;
        }

        public Task<BatchUpdateSpreadsheetResponse> BatchUpdateAsync(
            string spreadsheetId,
            BatchUpdateSpreadsheetRequest body,
            CancellationToken ct)
        {
            return ExecuteWithRetryAsync(
                () => _service.Spreadsheets.BatchUpdate(body, spreadsheetId).ExecuteAsync(ct),
                ct);
        }

        public async Task BatchWriteValuesAsync(
            string spreadsheetId,
            IReadOnlyList<ValueRange> ranges,
            CancellationToken ct)
        {
            if (ranges == null || ranges.Count == 0)
                return;

            foreach (var chunk in ChunkValueRanges(ranges))
            {
                var body = new BatchUpdateValuesRequest
                {
                    ValueInputOption = "RAW",
                    Data = chunk
                };
                await ExecuteWithRetryAsync(
                    () => _service.Spreadsheets.Values.BatchUpdate(body, spreadsheetId).ExecuteAsync(ct),
                    ct);
            }
        }

        public async Task BatchClearValuesAsync(
            string spreadsheetId,
            IReadOnlyList<string> ranges,
            CancellationToken ct)
        {
            if (ranges == null || ranges.Count == 0)
                return;

            foreach (var chunk in ChunkStrings(ranges))
            {
                var body = new BatchClearValuesRequest
                {
                    Ranges = chunk
                };
                await ExecuteWithRetryAsync(
                    () => _service.Spreadsheets.Values.BatchClear(body, spreadsheetId).ExecuteAsync(ct),
                    ct);
            }
        }

        public async Task<List<List<List<string>>>> ReadRangesAsync(
            string spreadsheetId,
            IReadOnlyList<string> ranges,
            CancellationToken ct)
        {
            var result = new List<List<List<string>>>();
            if (ranges == null || ranges.Count == 0)
                return result;

            foreach (var chunk in ChunkStrings(ranges))
            {
                var response = await ExecuteWithRetryAsync(() =>
                {
                    var request = _service.Spreadsheets.Values.BatchGet(spreadsheetId);
                    request.Ranges = chunk;
                    request.MajorDimension = SpreadsheetsResource.ValuesResource.BatchGetRequest.MajorDimensionEnum.ROWS;
                    return request.ExecuteAsync(ct);
                }, ct);

                for (var index = 0; index < chunk.Count; index++)
                {
                    var values = response.ValueRanges != null && index < response.ValueRanges.Count
                        ? response.ValueRanges[index]?.Values
                        : null;
                    result.Add(ToStringRows(values));
                }
            }

            return result;
        }

        public void Dispose()
        {
            _service?.Dispose();
        }

        public static string QuoteTitle(string title)
        {
            return "'" + (title ?? string.Empty).Replace("'", "''") + "'";
        }

        private static IEnumerable<List<ValueRange>> ChunkValueRanges(IReadOnlyList<ValueRange> ranges)
        {
            var chunk = new List<ValueRange>();
            var estimatedSize = 0;
            foreach (var range in ranges)
            {
                var itemSize = EstimateSize(range);
                if (chunk.Count > 0
                    && (chunk.Count >= MaxRangesPerBatch
                        || estimatedSize + itemSize > MaxBatchPayloadCharacters))
                {
                    yield return chunk;
                    chunk = new List<ValueRange>();
                    estimatedSize = 0;
                }

                chunk.Add(range);
                estimatedSize += itemSize;
            }

            if (chunk.Count > 0)
                yield return chunk;
        }

        private static IEnumerable<List<string>> ChunkStrings(IReadOnlyList<string> values)
        {
            var chunk = new List<string>();
            var estimatedSize = 0;
            foreach (var value in values)
            {
                var itemSize = (value?.Length ?? 0) + 16;
                if (chunk.Count > 0
                    && (chunk.Count >= MaxRangesPerBatch
                        || estimatedSize + itemSize > MaxBatchPayloadCharacters))
                {
                    yield return chunk;
                    chunk = new List<string>();
                    estimatedSize = 0;
                }

                chunk.Add(value);
                estimatedSize += itemSize;
            }

            if (chunk.Count > 0)
                yield return chunk;
        }

        private static int EstimateSize(ValueRange range)
        {
            var size = (range?.Range?.Length ?? 0) + 64;
            if (range?.Values == null)
                return size;

            foreach (var row in range.Values)
            {
                if (row == null)
                    continue;
                foreach (var value in row)
                    size += (Convert.ToString(value, CultureInfo.InvariantCulture)?.Length ?? 0) + 8;
            }
            return size;
        }

        private static void ReadMetadata(IList<DeveloperMetadata> metadata, GoogleSheetSnapshot snapshot)
        {
            if (metadata == null)
                return;

            foreach (var item in metadata)
            {
                if (item == null)
                    continue;

                if (item.MetadataKey == GoogleSheetConfig.TABLE_ID_METADATA_KEY)
                {
                    snapshot.TableId = item.MetadataValue;
                    snapshot.HasTableMetadata = true;
                }
                else if (item.MetadataKey == GoogleSheetConfig.SCHEMA_VERSION_METADATA_KEY)
                {
                    snapshot.HasSchemaVersionMetadata = true;
                }
                else if (item.MetadataKey == GoogleSheetConfig.COLUMN_KEY_METADATA_KEY)
                {
                    var range = item.Location?.DimensionRange;
                    if (range?.Dimension == "COLUMNS" && range.StartIndex != null)
                        snapshot.ColumnKeys[range.StartIndex.Value] = item.MetadataValue;
                }
            }
        }

        private static List<List<string>> ToStringRows(IList<IList<object>> values)
        {
            var result = new List<List<string>>();
            if (values == null)
                return result;

            foreach (var row in values)
            {
                var converted = new List<string>();
                if (row != null)
                {
                    foreach (var value in row)
                        converted.Add(Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
                }
                result.Add(converted);
            }
            return result;
        }

        private static string TryReadLegacyTableId(string title)
        {
            if (string.IsNullOrEmpty(title))
                return string.Empty;

            var separator = title.LastIndexOf('_');
            return separator >= 0 && separator + 1 < title.Length
                ? title.Substring(separator + 1)
                : string.Empty;
        }

        private static async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> operation, CancellationToken ct)
        {
            for (var attempt = 0; ; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    return await operation();
                }
                catch (GoogleApiException e) when (CanRetry(e.HttpStatusCode, attempt))
                {
                    var jitter = new Random(unchecked(Environment.TickCount * 31 + attempt)).Next(0, 500);
                    var delay = e.HttpStatusCode == (HttpStatusCode)429
                        ? Math.Min(60000, (attempt + 1) * 15000) + jitter
                        : Math.Min(8000, (1 << attempt) * 1000) + jitter;
                    await Task.Delay(delay, ct);
                }
            }
        }

        private static bool CanRetry(HttpStatusCode statusCode, int completedAttempts)
        {
            var maxAttempts = statusCode == (HttpStatusCode)429 ? 7 : 5;
            return completedAttempts + 1 < maxAttempts && IsTransient(statusCode);
        }

        private static bool IsTransient(HttpStatusCode statusCode)
        {
            return statusCode == (HttpStatusCode)429
                   || statusCode == HttpStatusCode.InternalServerError
                   || statusCode == HttpStatusCode.BadGateway
                   || statusCode == HttpStatusCode.ServiceUnavailable
                   || statusCode == HttpStatusCode.GatewayTimeout;
        }
    }
}
