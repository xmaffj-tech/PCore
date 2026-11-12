using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
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

    // Uses the SQLite engine shipped with supported Windows 10 builds (winsqlite3.dll).
    // No external native package or installer is required.
    public sealed class HistoryStore
    {
        private readonly string _databasePath;
        private readonly string _legacyJsonPath;
        private const int SqliteOk = 0;
        private const int SqliteRow = 100;
        private const int SqliteDone = 101;

        public HistoryStore()
        {
            string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PCore");
            Directory.CreateDirectory(folder);
            _databasePath = Path.Combine(folder, "history.db");
            _legacyJsonPath = Path.Combine(folder, "history.json");
            Initialize();
        }

        private void Initialize()
        {
            WithDatabase(delegate(IntPtr db)
            {
                Execute(db, "CREATE TABLE IF NOT EXISTS samples (" +
                    "id INTEGER PRIMARY KEY AUTOINCREMENT, timestamp_ticks INTEGER NOT NULL, " +
                    "cpu REAL NULL, ram REAL NULL, disk REAL NULL);");
                Execute(db, "CREATE INDEX IF NOT EXISTS idx_samples_timestamp ON samples(timestamp_ticks DESC);");
                long count = ScalarInt64(db, "SELECT COUNT(*) FROM samples;");
                if (count == 0 && File.Exists(_legacyJsonPath))
                {
                    List<HistoryEntry> oldEntries = ReadLegacyJson();
                    foreach (HistoryEntry entry in oldEntries) Insert(db, entry);
                }
            });
        }

        public void Add(HistoryEntry entry)
        {
            if (entry == null) throw new ArgumentNullException("entry");
            WithDatabase(delegate(IntPtr db) { Insert(db, entry); });
        }

        public IList<HistoryEntry> ReadLatest(int count)
        {
            int limit = Math.Max(1, Math.Min(5000, count));
            var result = new List<HistoryEntry>();
            WithDatabase(delegate(IntPtr db)
            {
                IntPtr statement = Prepare(db, "SELECT timestamp_ticks, cpu, ram, disk FROM samples ORDER BY timestamp_ticks DESC LIMIT " + limit + ";");
                try
                {
                    int code;
                    while ((code = sqlite3_step(statement)) == SqliteRow)
                    {
                        long ticks = sqlite3_column_int64(statement, 0);
                        result.Add(new HistoryEntry
                        {
                            Timestamp = new DateTime(ticks, DateTimeKind.Local),
                            CpuPercent = ReadNullableDouble(statement, 1),
                            RamPercent = ReadNullableDouble(statement, 2),
                            DiskPercent = ReadNullableDouble(statement, 3)
                        });
                    }
                    if (code != SqliteDone) throw new InvalidOperationException("SQLite read failed: " + code);
                }
                finally { sqlite3_finalize(statement); }
            });
            return result;
        }

        private static double? ReadNullableDouble(IntPtr statement, int column)
        {
            return sqlite3_column_type(statement, column) == 5 ? (double?)null : sqlite3_column_double(statement, column);
        }

        private static void Insert(IntPtr db, HistoryEntry entry)
        {
            IntPtr statement = Prepare(db, "INSERT INTO samples(timestamp_ticks,cpu,ram,disk) VALUES(?,?,?,?);");
            try
            {
                sqlite3_bind_int64(statement, 1, entry.Timestamp.Ticks);
                BindNullableDouble(statement, 2, entry.CpuPercent);
                BindNullableDouble(statement, 3, entry.RamPercent);
                BindNullableDouble(statement, 4, entry.DiskPercent);
                int code = sqlite3_step(statement);
                if (code != SqliteDone) throw new InvalidOperationException("SQLite insert failed: " + code);
            }
            finally { sqlite3_finalize(statement); }
        }

        private static void BindNullableDouble(IntPtr statement, int index, double? value)
        {
            if (value.HasValue) sqlite3_bind_double(statement, index, value.Value);
            else sqlite3_bind_null(statement, index);
        }

        private List<HistoryEntry> ReadLegacyJson()
        {
            try
            {
                var serializer = new DataContractJsonSerializer(typeof(List<HistoryEntry>));
                using (var stream = new FileStream(_legacyJsonPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    return serializer.ReadObject(stream) as List<HistoryEntry> ?? new List<HistoryEntry>();
            }
            catch (Exception) { return new List<HistoryEntry>(); }
        }

        private static long ScalarInt64(IntPtr db, string sql)
        {
            IntPtr statement = Prepare(db, sql);
            try
            {
                if (sqlite3_step(statement) != SqliteRow) return 0;
                return sqlite3_column_int64(statement, 0);
            }
            finally { sqlite3_finalize(statement); }
        }

        private static IntPtr Prepare(IntPtr db, string sql)
        {
            IntPtr statement;
            int code = sqlite3_prepare_v2(db, sql, -1, out statement, IntPtr.Zero);
            if (code != SqliteOk) throw new InvalidOperationException("SQLite prepare failed: " + code);
            return statement;
        }

        private static void Execute(IntPtr db, string sql)
        {
            IntPtr error;
            int code = sqlite3_exec(db, sql, IntPtr.Zero, IntPtr.Zero, out error);
            if (code != SqliteOk)
            {
                string message = error == IntPtr.Zero ? "unknown error" : Marshal.PtrToStringAnsi(error);
                if (error != IntPtr.Zero) sqlite3_free(error);
                throw new InvalidOperationException("SQLite error: " + message);
            }
        }

        private static void WithDatabase(Action<IntPtr> action)
        {
            IntPtr db;
            string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "PCore", "history.db");
            int code = sqlite3_open16(path, out db);
            if (code != SqliteOk) throw new InvalidOperationException("Unable to open PCore history database: " + code);
            try { action(db); }
            finally { sqlite3_close(db); }
        }

        public string FilePath { get { return _databasePath; } }

        [DllImport("winsqlite3.dll", EntryPoint = "sqlite3_open16", CharSet = CharSet.Unicode, CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_open16(string filename, out IntPtr database);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_close(IntPtr database);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int sqlite3_exec(IntPtr database, string sql, IntPtr callback, IntPtr argument, out IntPtr errorMessage);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern void sqlite3_free(IntPtr memory);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
        private static extern int sqlite3_prepare_v2(IntPtr database, string sql, int bytes, out IntPtr statement, IntPtr tail);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_step(IntPtr statement);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_finalize(IntPtr statement);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_bind_int64(IntPtr statement, int index, long value);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_bind_double(IntPtr statement, int index, double value);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_bind_null(IntPtr statement, int index);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern long sqlite3_column_int64(IntPtr statement, int column);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern double sqlite3_column_double(IntPtr statement, int column);
        [DllImport("winsqlite3.dll", CallingConvention = CallingConvention.Cdecl)]
        private static extern int sqlite3_column_type(IntPtr statement, int column);
    }
}
