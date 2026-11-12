using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace PCore.App
{
    public sealed class GpuMonitor
    {
        public GpuSnapshot Read()
        {
            try
            {
                var category = new PerformanceCounterCategory("GPU Engine");
                var values = new List<float>();
                foreach (string instance in category.GetInstanceNames())
                {
                    if (instance.IndexOf("engtype_3D", StringComparison.OrdinalIgnoreCase) < 0) continue;
                    using (var counter = new PerformanceCounter("GPU Engine", "Utilization Percentage", instance, true))
                    {
                        counter.NextValue();
                        System.Threading.Thread.Sleep(80);
                        float value = counter.NextValue();
                        if (!float.IsNaN(value) && !float.IsInfinity(value))
                            values.Add(Math.Max(0, Math.Min(100, value)));
                    }
                }
                if (values.Count == 0) return GpuSnapshot.Unavailable("Счётчики GPU Engine недоступны");
                return new GpuSnapshot { UtilizationPercent = Math.Min(100, values.Sum()), Status = "Данные Windows GPU Engine" };
            }
            catch (Exception ex)
            {
                return GpuSnapshot.Unavailable("Счётчик GPU недоступен: " + ex.GetType().Name);
            }
        }
    }

    public sealed class GpuSnapshot
    {
        public double? UtilizationPercent { get; set; }
        public string Status { get; set; }
        public static GpuSnapshot Unavailable(string reason)
        {
            return new GpuSnapshot { UtilizationPercent = null, Status = reason };
        }
    }
}