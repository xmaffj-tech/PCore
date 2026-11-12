using System;
using System.Collections.Generic;
using System.IO;

namespace PCore.App
{
    public sealed class StorageDrive
    {
        public string Name { get; set; }
        public string Label { get; set; }
        public string Format { get; set; }
        public double TotalGb { get; set; }
        public double FreeGb { get; set; }
        public double UsedGb { get { return Math.Max(0, TotalGb - FreeGb); } }
        public double UsedPercent { get { return TotalGb <= 0 ? 0 : UsedGb / TotalGb * 100d; } }
    }

    public sealed class StorageAnalyzer
    {
        public IList<StorageDrive> ReadDrives()
        {
            var result = new List<StorageDrive>();
            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                if (!drive.IsReady) continue;
                try
                {
                    result.Add(new StorageDrive
                    {
                        Name = drive.Name,
                        Label = string.IsNullOrWhiteSpace(drive.VolumeLabel) ? "Локальный диск" : drive.VolumeLabel,
                        Format = drive.DriveFormat,
                        TotalGb = drive.TotalSize / 1073741824d,
                        FreeGb = drive.AvailableFreeSpace / 1073741824d
                    });
                }
                catch (IOException) { }
                catch (UnauthorizedAccessException) { }
            }
            return result;
        }
    }
}