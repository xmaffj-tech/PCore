using System;

namespace PCore.App
{
    public sealed class SystemAlertService
    {
        private DateTime _lastCpuAlert = DateTime.MinValue;
        private DateTime _lastRamAlert = DateTime.MinValue;
        public event EventHandler<SystemAlertEventArgs> AlertRaised;

        public void Evaluate(SystemMonitorSnapshot snapshot, double thresholdPercent, TimeSpan cooldown)
        {
            if (snapshot == null) return;
            DateTime now = DateTime.UtcNow;
            if (snapshot.CpuUsage.HasValue && snapshot.CpuUsage.Value >= thresholdPercent &&
                now - _lastCpuAlert >= cooldown)
            {
                _lastCpuAlert = now;
                Raise("Высокая загрузка CPU", "Загрузка процессора достигла " + snapshot.CpuUsage.Value.ToString("0") + "%.");
            }
            if (snapshot.RamUsagePercent.HasValue && snapshot.RamUsagePercent.Value >= thresholdPercent &&
                now - _lastRamAlert >= cooldown)
            {
                _lastRamAlert = now;
                Raise("Мало свободной RAM", "Использование оперативной памяти достигло " + snapshot.RamUsagePercent.Value.ToString("0") + "%.");
            }
        }

        private void Raise(string title, string message)
        {
            EventHandler<SystemAlertEventArgs> handler = AlertRaised;
            if (handler != null) handler(this, new SystemAlertEventArgs(title, message));
        }
    }

    public sealed class SystemAlertEventArgs : EventArgs
    {
        public string Title { get; private set; }
        public string Message { get; private set; }
        public SystemAlertEventArgs(string title, string message) { Title = title; Message = message; }
    }
}