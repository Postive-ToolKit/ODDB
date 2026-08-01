using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using TeamODD.ODDB.Runtime.DTO;

namespace TeamODD.ODDB.Runtime.Utils.Converters
{
    public class ODDBConverter
    {
        public static readonly List<DataBaseCreateEvent> OnDatabaseCreated = new List<DataBaseCreateEvent>();
        public static event Action<ODDatabase> OnDatabaseExported;

        internal static void ResetRuntimeState()
        {
            OnDatabaseCreated.Clear();
            OnDatabaseExported = null;
        }

        public ODDatabase Import(byte[] binary)
        {
            if (!TryImportDTO(binary, out var databaseDto, out _, out _))
            {
                OnDatabaseCreated.Clear();
                return null;
            }

            var database = new ODDatabase();
            database.FromDTO(databaseDto);

            OnDatabaseCreated.Sort((a, b) => a.Priority.CompareTo(b.Priority));

            foreach (var createEvent in OnDatabaseCreated)
                createEvent.OnEvent?.Invoke(database);

            OnDatabaseCreated.Clear();
            return database;
        }

        public byte[] Export(ODDatabase database)
        {
            if (database == null)
                throw new ArgumentNullException(nameof(database), "ODDBConverter.Export database is null");
            var dto = database.ToDTO();
            var binary = ExportDTO(dto);
            OnDatabaseExported?.Invoke(database);
            return binary;
        }

        public DatabaseDTO ImportDTO(byte[] binary)
        {
            var databaseDto = new DatabaseDTO();
            try
            {
                var decompressed = Decompress(binary);
                var json = Encoding.UTF8.GetString(decompressed);
                databaseDto = JsonConvert.DeserializeObject<DatabaseDTO>(json);
            }
            catch (Exception e)
            {
                ODDB.Logger.Error("ODDBConverter.ImportDTO failed to deserialize database DTO - use default database DTO. Error: " + e);
                return new DatabaseDTO();
            }
            return databaseDto ?? new DatabaseDTO();
        }

        public bool TryImportDTO(byte[] binary, out DatabaseDTO dto, out string failureStage, out string failureReason)
        {
            dto = null;
            failureStage = ODDBLoadFailureStage.None;
            failureReason = null;

            byte[] decompressed;
            try
            {
                decompressed = Decompress(binary);
            }
            catch (Exception e)
            {
                failureStage = ODDBLoadFailureStage.Gzip;
                failureReason = e.Message;
                return false;
            }

            string json;
            try
            {
                json = Encoding.UTF8.GetString(decompressed);
            }
            catch (Exception e)
            {
                failureStage = ODDBLoadFailureStage.Gzip;
                failureReason = "UTF8 decode: " + e.Message;
                return false;
            }

            try
            {
                dto = JsonConvert.DeserializeObject<DatabaseDTO>(json);
            }
            catch (Exception e)
            {
                failureStage = ODDBLoadFailureStage.Json;
                failureReason = e.Message;
                return false;
            }

            if (dto == null)
            {
                failureStage = ODDBLoadFailureStage.Json;
                failureReason = "JsonConvert returned null DTO";
                return false;
            }

            return true;
        }

        public DatabaseDTO TryImportDTO(byte[] binary, out string failureStage, out string failureReason)
        {
            TryImportDTO(binary, out var dto, out failureStage, out failureReason);
            return dto;
        }

        public byte[] ExportDTO(DatabaseDTO databaseDto)
        {
            var data = JsonConvert.SerializeObject(databaseDto);
            var compressed = Compress(Encoding.UTF8.GetBytes(data));
            return compressed;
        }

        private byte[] Compress(byte[] data)
        {
            using MemoryStream outputStream = new MemoryStream();
            using (GZipStream gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
                gzipStream.Write(data, 0, data.Length);
            return outputStream.ToArray();
        }

        private byte[] Decompress(byte[] compressedData)
        {
            using MemoryStream inputStream = new MemoryStream(compressedData);
            using MemoryStream outputStream = new MemoryStream();
            using (GZipStream gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                gzipStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
    }
}
