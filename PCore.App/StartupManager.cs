using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace PCore.App
{
    public sealed class StartupEntry
    {
        public string Name { get; set; }
        public string Command { get; set; }
        public string Location { get; set; }
    }

    public sealed class StartupManager
    {
        public IList<StartupEntry> ReadEntries()
        {
            var entries = new List<StartupEntry>();
            ReadRunKey(Registry.CurrentUser, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKCU Run", entries);
            ReadRunKey(Registry.LocalMachine, @"Software\Microsoft\Windows\CurrentVersion\Run", "HKLM Run", entries);
            ReadStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.Startup), "Папка автозагрузки пользователя", entries);
            ReadStartupFolder(Environment.GetFolderPath(Environment.SpecialFolder.CommonStartup), "Общая папка автозагрузки", entries);
            return entries;
        }

        private static void ReadRunKey(RegistryKey root, string path, string location, IList<StartupEntry> entries)
        {
            try
            {
                using (RegistryKey key = root.OpenSubKey(path, false))
                {
                    if (key == null) return;
                    foreach (string name in key.GetValueNames())
                    {
                        object value = key.GetValue(name);
                        entries.Add(new StartupEntry { Name = name, Command = value == null ? "" : value.ToString(), Location = location });
                    }
                }
            }
            catch (UnauthorizedAccessException) { }
            catch (System.Security.SecurityException) { }
        }

        private static void ReadStartupFolder(string folder, string location, IList<StartupEntry> entries)
        {
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder)) return;
            try
            {
                foreach (string file in Directory.GetFiles(folder))
                    entries.Add(new StartupEntry { Name = Path.GetFileNameWithoutExtension(file), Command = file, Location = location });
            }
            catch (UnauthorizedAccessException) { }
            catch (IOException) { }
        }
    }
}