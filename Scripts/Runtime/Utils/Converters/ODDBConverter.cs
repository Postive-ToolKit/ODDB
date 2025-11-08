using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;
using NUnit.Framework;
using TeamODD.ODDB.Runtime.DTO;
using UnityEngine;


namespace TeamODD.ODDB.Runtime.Utils.Converters
{
    public class ODDBConverter
    {
        public static readonly List<DataBaseCreateEvent> OnDatabaseCreated = new List<DataBaseCreateEvent>();
        public static event Action<ODDatabase> OnDatabaseExported;
        public ODDatabase Import(byte[] binary)
        {
            var databaseDto = ImportDTO(binary);
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
            Assert.IsNotNull(database, "ODDBConverter.Export database is null");
            var dto = database.ToDTO();
            var binary = ExportDTO(dto);
            OnDatabaseExported?.Invoke(database);
            return binary;
        }
        
        public DatabaseDTO ImportDTO(byte[] binary)
        {
            var decompressed = Decompress(binary);
            var json = Encoding.UTF8.GetString(decompressed);
            var databaseDto = new DatabaseDTO();
            try
            {
                databaseDto = JsonConvert.DeserializeObject<DatabaseDTO>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("ODDBConverter.ImportDTO failed to deserialize database DTO - use default database DTO. Error: " + e);
            }
            return databaseDto;
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