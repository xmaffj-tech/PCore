using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace PCore.App
{
    [DataContract]
    public sealed class HistoryEntry
    {
        [DataMember] public DateTime Timestamp { get; set; }
        [DataMember] public double? CpuPercent { get; set; }
        [DataMember] public double? RamPercent { get; set; }
        [DataMember] public double? DiskPercent { get; set; }
    }

    public sealed class HistoryStore
    {
        private readonly string _filePath;

        public HistoryStore()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PCore");
            Directory.CreateDirectory(folder);
            _filePath = Path.Combine(folder, "history.json");
        }

        public void Add(HistoryEntry entry)
        {
            List<HistoryEntry> entries = ReadAll();
            entries.Add(entry);
            if (entries.Count > 5000)
                entries = entries.Skip(entries.Count - 5000).ToList();

            var serializer = new DataContractJsonSerializer(typeof(List<HistoryEntry>));
            using (var stream = new FileStream(_filePath + ".tmp", FileMode.Create, FileAccess.Write, FileShare.None))
                serializer.WriteObject(stream, entries);

            if (File.Exists(_filePath)) File.Delete(_filePath);
            File.Move(_filePath + ".tmp", _filePath);
        }

        public IList<HistoryEntry> ReadLatest(int count)
        {
            return ReadAll().OrderByDescending(x => x.Timestamp).Take(count).ToList();
        }

        private List<HistoryEntry> ReadAll()
        {
            if (!File.Exists(_filePath)) return new List<HistoryEntry>();
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(List<HistoryEntry>));
                using (var stream = new FileStream(_filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    return serializer.ReadObject(stream) as List<HistoryEntry> ?? new List<HistoryEntry>();
            }
            catch (SerializationException) { return new List<HistoryEntry>(); }
            catch (IOException) { return new List<HistoryEntry>(); }
        }

        public string FilePath { get { return _filePath; } }
    }
}