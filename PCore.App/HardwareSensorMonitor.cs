using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LibreHardwareMonitor.Hardware;

namespace PCore.App
{
    public sealed class HardwareSensorMonitor : IDisposable
    {
        private readonly Computer _computer;
        private readonly object _sync = new object();
        private bool _disposed;
        private string _status;

        public HardwareSensorMonitor()
        {
            _computer = new Computer
            {
                IsCpuEnabled = true,
                IsGpuEnabled = true,
                IsMemoryEnabled = true,
                IsMotherboardEnabled = true,
                IsControllerEnabled = true,
                IsNetworkEnabled = true,
                IsStorageEnabled = true
            };

            try
            {
                _computer.Open();
                _status = "LibreHardwareMonitor подключён";
            }
            catch (Exception ex)
            {
                _status = "Не удалось открыть аппаратные датчики: " + ex.GetType().Name;
            }
        }

        public HardwareSensorSnapshot Read()
        {
            lock (_sync)
            {
                if (_disposed)
                    return HardwareSensorSnapshot.Unavailable("Монитор датчиков остановлен");

                var readings = new List<HardwareSensorReading>();
                try
                {
                    foreach (IHardware hardware in _computer.Hardware)
                        ReadHardware(hardware, readings);

                    _status = readings.Count == 0
                        ? "Устройство не предоставило показания датчиков. Проверьте поддержку и права доступа."
                        : "Получено датчиков: " + readings.Count;
                }
                catch (Exception ex)
                {
                    _status = "Ошибка чтения датчиков: " + ex.GetType().Name;
                }

                return new HardwareSensorSnapshot(readings, _status);
            }
        }

        private static void ReadHardware(IHardware hardware, IList<HardwareSensorReading> readings)
        {
            try { hardware.Update(); } catch { }

            foreach (ISensor sensor in hardware.Sensors)
            {
                if (!sensor.Value.HasValue) continue;
                float value = sensor.Value.Value;
                if (float.IsNaN(value) || float.IsInfinity(value)) continue;

                readings.Add(new HardwareSensorReading
                {
                    HardwareName = hardware.Name,
                    HardwareType = hardware.HardwareType.ToString(),
                    Name = sensor.Name,
                    SensorType = sensor.SensorType.ToString(),
                    Value = value,
                    Unit = GetUnit(sensor.SensorType),
                    Minimum = sensor.Min.HasValue ? (double?)sensor.Min.Value : null,
                    Maximum = sensor.Max.HasValue ? (double?)sensor.Max.Value : null
                });
            }

            foreach (IHardware subHardware in hardware.SubHardware)
                ReadHardware(subHardware, readings);
        }

        private static string GetUnit(SensorType type)
        {
            switch (type)
            {
                case SensorType.Temperature: return "°C";
                case SensorType.Fan: return "RPM";
                case SensorType.Clock: return "МГц";
                case SensorType.Voltage: return "В";
                case SensorType.Power: return "Вт";
                case SensorType.Load: return "%";
                case SensorType.Control: return "%";
                case SensorType.Flow: return "л/ч";
                case SensorType.Data: return "ГБ";
                case SensorType.SmallData: return "МБ";
                default: return "";
            }
        }

        public void Dispose()
        {
            lock (_sync)
            {
                if (_disposed) return;
                try { _computer.Close(); } catch { }
                _disposed = true;
            }
        }
    }

    public sealed class HardwareSensorReading
    {
        public string HardwareName { get; set; }
        public string HardwareType { get; set; }
        public string Name { get; set; }
        public string SensorType { get; set; }
        public float Value { get; set; }
        public string Unit { get; set; }
        public double? Minimum { get; set; }
        public double? Maximum { get; set; }

        public string DisplayValue
        {
            get { return Value.ToString("0.##", CultureInfo.InvariantCulture) + (string.IsNullOrEmpty(Unit) ? "" : " " + Unit); }
        }
    }

    public sealed class HardwareSensorSnapshot
    {
        public IList<HardwareSensorReading> Sensors { get; private set; }
        public string Status { get; private set; }

        public double? CpuTemperature
        {
            get { return FindMaximum("Cpu", "Temperature"); }
        }

        public double? GpuTemperature
        {
            get { return FindMaximum("Gpu", "Temperature"); }
        }

        public double? GpuLoad
        {
            get { return FindMaximum("Gpu", "Load"); }
        }

        public double? CpuClock
        {
            get { return FindMaximum("Cpu", "Clock"); }
        }

        public double? GpuClock
        {
            get { return FindMaximum("Gpu", "Clock"); }
        }

        public HardwareSensorSnapshot(IList<HardwareSensorReading> sensors, string status)
        {
            Sensors = sensors ?? new List<HardwareSensorReading>();
            Status = status;
        }

        private double? FindMaximum(string hardwarePrefix, string sensorType)
        {
            var matches = Sensors.Where(s =>
                s.HardwareType.IndexOf(hardwarePrefix, StringComparison.OrdinalIgnoreCase) >= 0 &&
                string.Equals(s.SensorType, sensorType, StringComparison.OrdinalIgnoreCase))
                .Select(s => (double?)s.Value).ToList();
            return matches.Count == 0 ? (double?)null : matches.Max();
        }

        public static HardwareSensorSnapshot Unavailable(string status)
        {
            return new HardwareSensorSnapshot(new List<HardwareSensorReading>(), status);
        }
    }
}