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
    internal sealed class GoogleSheetsApiClient : IDisposable
    {
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

            return snapshots;
        }

        public async Task<GoogleSheetSnapshot> CreateSheetAsync(
            string spreadsheetId,
            string title,
            CancellationToken ct)
        {
            var body = new BatchUpdateSpreadsheetRequest
            {
                Requests = new List<Request>
                {
                    new Request
                    {
                        AddSheet = new AddSheetRequest
                        {
                            Properties = new SheetProperties { Title = title }
                        }
                    }
                }
            };

            var response = await BatchUpdateAsync(spreadsheetId, body, ct);
            var properties = response.Replies?.FirstOrDefault()?.AddSheet?.Properties;
            if (properties?.SheetId == null)
                throw new InvalidOperationException($"Google Sheets did not return an ID for newly created sheet '{title}'.");

            return new GoogleSheetSnapshot
            {
                SheetId = properties.SheetId.Value,
                Title = properties.Title ?? title,
                ColumnCount = properties.GridProperties?.ColumnCount ?? 26,
                RowCount = properties.GridProperties?.RowCount ?? 1000,
                Values = new List<List<string>>()
            };
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

            const int chunkSize = 500;
            for (var offset = 0; offset < ranges.Count; offset += chunkSize)
            {
                var chunk = ranges.Skip(offset).Take(chunkSize).ToList();
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

            const int chunkSize = 500;
            for (var offset = 0; offset < ranges.Count; offset += chunkSize)
            {
                var body = new BatchClearValuesRequest
                {
                    Ranges = ranges.Skip(offset).Take(chunkSize).ToList()
                };
                await ExecuteWithRetryAsync(
                    () => _service.Spreadsheets.Values.BatchClear(body, spreadsheetId).ExecuteAsync(ct),
                    ct);
            }
        }

        public async Task<List<List<string>>> ReadRangeAsync(
            string spreadsheetId,
            string range,
            CancellationToken ct)
        {
            var response = await ExecuteWithRetryAsync(
                () => _service.Spreadsheets.Values.Get(spreadsheetId, range).ExecuteAsync(ct),
                ct);
            return ToStringRows(response.Values);
        }

        public void Dispose()
        {
            _service?.Dispose();
        }

        public static string QuoteTitle(string title)
        {
            return "'" + (title ?? string.Empty).Replace("'", "''") + "'";
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
            const int maxAttempts = 5;
            for (var attempt = 0; ; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    return await operation();
                }
                catch (GoogleApiException e) when (attempt + 1 < maxAttempts && IsTransient(e.HttpStatusCode))
                {
                    var jitter = new Random(unchecked(Environment.TickCount * 31 + attempt)).Next(0, 500);
                    var delay = Math.Min(8000, (1 << attempt) * 1000) + jitter;
                    await Task.Delay(delay, ct);
                }
            }
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
