using System;
using System.Diagnostics;

namespace PCore.App
{
    public sealed class BenchmarkResult
    {
        public string TestName { get; set; }
        public TimeSpan Duration { get; set; }
        public long Operations { get; set; }
        public string Note { get; set; }
    }

    public sealed class BenchmarkService
    {
        // Lightweight repeatable arithmetic workload. It is a simple diagnostic, not an industry-standard score.
        public BenchmarkResult RunCpuTest(int seconds)
        {
            seconds = Math.Max(1, Math.Min(15, seconds));
            var watch = Stopwatch.StartNew();
            long operations = 0;
            double value = 1.000001d;
            while (watch.Elapsed.TotalSeconds < seconds)
            {
                value = Math.Sqrt(value + 0.000001d);
                if (value > 1000d) value = 1.000001d;
                operations++;
            }
            watch.Stop();
            GC.KeepAlive(value);
            return new BenchmarkResult
            {
                TestName = "CPU arithmetic",
                Duration = watch.Elapsed,
                Operations = operations,
                Note = "Диагностический тест на этом ПК; результат зависит от питания, температуры и фоновых задач."
            };
        }
    }
}