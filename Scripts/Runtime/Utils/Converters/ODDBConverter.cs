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
            var decompressed = Decompress(binary);
            var json = Encoding.UTF8.GetString(decompressed);
            var databaseDto = new DatabaseDTO();
            try
            {
                databaseDto = JsonConvert.DeserializeObject<DatabaseDTO>(json);
            }
            catch (Exception e)
            {
                Debug.LogError("ODDBConverter.Import failed to deserialize database - use default database. Error: " + e);
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
            Assert.IsNotNull(database, "ODDBConverter.Export database is null");
            var dto = database.ToDTO();
            var data = JsonConvert.SerializeObject(dto);
            var compressed = Compress(Encoding.UTF8.GetBytes(data));
            OnDatabaseExported?.Invoke(database);
            return compressed;
        }
        
        public byte[] Compress(byte[] data)
        {
            using MemoryStream outputStream = new MemoryStream();
            using (GZipStream gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
                gzipStream.Write(data, 0, data.Length);
            return outputStream.ToArray();
        }

        public byte[] Decompress(byte[] compressedData)
        {
            using MemoryStream inputStream = new MemoryStream(compressedData);
            using MemoryStream outputStream = new MemoryStream();
            using (GZipStream gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
                gzipStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
    }
}