using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Management;

namespace PCore.App
{
    public sealed class SystemMonitor : IDisposable
    {
        private readonly PerformanceCounter _cpuCounter;
        private readonly PerformanceCounter _availableMemoryCounter;
        private readonly double? _totalMemoryGb;
        private bool _cpuPrimed;
        private bool _disposed;

        public SystemMonitor()
        {
            _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            _availableMemoryCounter = new PerformanceCounter("Memory", "Available MBytes");
            _cpuCounter.NextValue();
            _cpuPrimed = false;
            _totalMemoryGb = ReadTotalMemoryGb();
        }

        public SystemMonitorSnapshot Read()
        {
            ThrowIfDisposed();

            double? cpu = null;
            try
            {
                float value = _cpuCounter.NextValue();
                if (_cpuPrimed && !float.IsNaN(value) && !float.IsInfinity(value))
                    cpu = Math.Max(0, Math.Min(100, value));
                _cpuPrimed = true;
            }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }

            double? usedRam = null;
            double? ramPercent = null;
            try
            {
                float availableMb = _availableMemoryCounter.NextValue();
                if (_totalMemoryGb.HasValue && availableMb >= 0)
                {
                    double totalMb = _totalMemoryGb.Value * 1024d;
                    usedRam = Math.Max(0, totalMb - availableMb) / 1024d;
                    ramPercent = totalMb > 0 ? Math.Max(0, Math.Min(100, (totalMb - availableMb) / totalMb * 100d)) : (double?)null;
                }
            }
            catch (InvalidOperationException) { }
            catch (System.ComponentModel.Win32Exception) { }

            double? diskTotal = null;
            double? diskFree = null;
            try
            {
                double total = 0;
                double free = 0;
                foreach (DriveInfo drive in DriveInfo.GetDrives())
                {
                    if (!drive.IsReady || drive.DriveType != DriveType.Fixed) continue;
                    try
                    {
                        total += drive.TotalSize;
                        free += drive.AvailableFreeSpace;
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }

                if (total > 0)
                {
                    diskTotal = total / 1000000000d;
                    diskFree = free / 1000000000d;
                }
            }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }

            double? diskUsed = diskTotal.HasValue && diskFree.HasValue
                ? Math.Max(0, diskTotal.Value - diskFree.Value)
                : (double?)null;
            double? diskPercent = diskTotal.HasValue && diskTotal.Value > 0 && diskUsed.HasValue
                ? diskUsed.Value / diskTotal.Value * 100d
                : (double?)null;

            return new SystemMonitorSnapshot
            {
                CpuUsage = cpu,
                RamUsedGb = usedRam,
                RamTotalGb = _totalMemoryGb,
                RamUsagePercent = ramPercent,
                DiskUsedGb = diskUsed,
                DiskTotalGb = diskTotal,
                DiskUsagePercent = diskPercent
            };
        }

        private static double? ReadTotalMemoryGb()
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
                using (var items = searcher.Get())
                {
                    foreach (ManagementObject item in items)
                    using (item)
                    {
                        object raw = item["TotalPhysicalMemory"];
                        ulong bytes;
                        if (raw != null && ulong.TryParse(raw.ToString(), NumberStyles.Integer,
                            CultureInfo.InvariantCulture, out bytes))
                            return bytes / 1073741824d;
                    }
                }
            }
            catch (ManagementException) { }
            catch (UnauthorizedAccessException) { }
            return null;
        }

        private void ThrowIfDisposed()
        {
            if (_disposed) throw new ObjectDisposedException("SystemMonitor");
        }

        public void Dispose()
        {
            if (_disposed) return;
            _cpuCounter.Dispose();
            _availableMemoryCounter.Dispose();
            _disposed = true;
        }
    }

    public sealed class SystemMonitorSnapshot
    {
        public double? CpuUsage { get; set; }
        public double? RamUsedGb { get; set; }
        public double? RamTotalGb { get; set; }
        public double? RamUsagePercent { get; set; }
        public double? DiskUsedGb { get; set; }
        public double? DiskTotalGb { get; set; }
        public double? DiskUsagePercent { get; set; }
    }
}
