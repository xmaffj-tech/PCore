using System;
using System.Collections.Generic;
using System.Globalization;
using System.Management;

namespace PCore.App
{
    public sealed class HardwareInformation
    {
        public string Processor { get; private set; }
        public string Graphics { get; private set; }
        public string Memory { get; private set; }
        public string Drives { get; private set; }
        public string OperatingSystem { get; private set; }

        private HardwareInformation() { }

        public static HardwareInformation Read()
        {
            return new HardwareInformation
            {
                Processor = ReadFirst("Win32_Processor", "Name"),
                Graphics = ReadGraphics(),
                Memory = ReadMemory(),
                Drives = ReadDrives(),
                OperatingSystem = ReadOperatingSystem()
            };
        }

        private static string ReadFirst(string className, string propertyName)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT " + propertyName + " FROM " + className))
                using (var items = searcher.Get())
                {
                    foreach (ManagementObject item in items)
                    using (item)
                    {
                        object value = item[propertyName];
                        if (value != null && !string.IsNullOrWhiteSpace(value.ToString()))
                            return value.ToString().Trim();
                    }
                }
            }
            catch (ManagementException) { }
            catch (UnauthorizedAccessException) { }
            return "Недоступно";
        }

        private static string ReadGraphics()
        {
            var names = new List<string>();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_VideoController"))
                using (var items = searcher.Get())
                {
                    foreach (ManagementObject item in items)
                    using (item)
                    {
                        object value = item["Name"];
                        if (value == null) continue;
                        string name = value.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(name) && !names.Contains(name))
                            names.Add(name);
                    }
                }
            }
            catch (ManagementException) { }
            catch (UnauthorizedAccessException) { }
            return names.Count == 0 ? "Недоступно" : string.Join(Environment.NewLine, names);
        }

        private static string ReadMemory()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                using (var items = searcher.Get())
                {
                    foreach (ManagementObject item in items)
                    using (item)
                    {
                        object value = item["TotalPhysicalMemory"];
                        ulong bytes;
                        if (value != null && ulong.TryParse(value.ToString(), NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out bytes))
                            return (bytes / 1073741824d).ToString("0.0", CultureInfo.InvariantCulture) + " ГБ установлено";
                    }
                }
            }
            catch (ManagementException) { }
            catch (UnauthorizedAccessException) { }
            return "Недоступно";
        }

        private static string ReadDrives()
        {
            var drives = new List<string>();
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Model, Size FROM Win32_DiskDrive"))
                using (var items = searcher.Get())
                {
                    foreach (ManagementObject item in items)
                    using (item)
                    {
                        string model = item["Model"] == null ? "Накопитель" : item["Model"].ToString().Trim();
                        string size = "объём неизвестен";
                        ulong bytes;
                        object rawSize = item["Size"];
                        if (rawSize != null && ulong.TryParse(rawSize.ToString(), NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out bytes))
                            size = (bytes / 1000000000d).ToString("0", CultureInfo.InvariantCulture) + " ГБ";
                        drives.Add(model + " — " + size);
                    }
                }
            }
            catch (ManagementException) { }
            catch (UnauthorizedAccessException) { }
            return drives.Count == 0 ? "Недоступно" : string.Join(Environment.NewLine, drives);
        }

        private static string ReadOperatingSystem()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT Caption, Version, OSArchitecture FROM Win32_OperatingSystem"))
                using (var items = searcher.Get())
                {
                    foreach (ManagementObject item in items)
                    using (item)
                    {
                        string caption = Convert.ToString(item["Caption"]);
                        string version = Convert.ToString(item["Version"]);
                        string architecture = Convert.ToString(item["OSArchitecture"]);
                        return string.Join(" | ", new[]
                        {
                            string.IsNullOrWhiteSpace(caption) ? "Windows" : caption.Trim(),
                            string.IsNullOrWhiteSpace(version) ? "версия недоступна" : version.Trim(),
                            string.IsNullOrWhiteSpace(architecture) ? "разрядность недоступна" : architecture.Trim()
                        });
                    }
                }
            }
            catch (ManagementException) { }
            catch (UnauthorizedAccessException) { }
            return "Недоступно";
        }
    }
}