using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PCore.App
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _monitorTimer;
        private SystemMonitor _systemMonitor;
        private readonly NetworkMonitor _networkMonitor = new NetworkMonitor();
        private readonly ProcessMonitor _processMonitor = new ProcessMonitor();\n        private readonly HistoryStore _historyStore = new HistoryStore();\n        private readonly SettingsStore _settingsStore = new SettingsStore();\n        private UserSettings _settings;

        public MainWindow()
        {
            InitializeComponent();
            _monitorTimer = new DispatcherTimer();
            _monitorTimer.Interval = TimeSpan.FromSeconds(1);
            _monitorTimer.Tick += MonitorTimer_Tick;
            StartMonitoring();
        }

        private void StartMonitoring()
        {
            try
            {
                _systemMonitor = new SystemMonitor();
                UpdateMonitoring();
                UpdateNetwork();
                _monitorTimer.Start();
            }
            catch (Exception)
            {
                CpuUsageValue.Text = "Недоступно";
                CpuUsageDetail.Text = "Системный счётчик недоступен";
                RamUsageValue.Text = "Недоступно";
                RamUsageDetail.Text = "Системный счётчик недоступен";
                DiskUsageValue.Text = "Недоступно";
                DiskUsageDetail.Text = "Не удалось прочитать накопители";
            }
        }

        private void MonitorTimer_Tick(object sender, EventArgs e)
        {
            UpdateMonitoring();
            if (NetworkContent.Visibility == Visibility.Visible)
                UpdateNetwork();
        }

        private void UpdateMonitoring()
        {
            if (_systemMonitor == null) return;
            try
            {
                SystemMonitorSnapshot snapshot = _systemMonitor.Read();\n                CpuPageValue.Text = snapshot.CpuUsage.HasValue ? snapshot.CpuUsage.Value.ToString("0") + "%" : "Недоступно";\n                RamPageValue.Text = snapshot.RamUsedGb.HasValue && snapshot.RamTotalGb.HasValue ? snapshot.RamUsedGb.Value.ToString("0.0") + " / " + snapshot.RamTotalGb.Value.ToString("0.0") + " ГБ" : "Недоступно";\n                RamPageDetail.Text = snapshot.RamUsagePercent.HasValue ? "Используется " + snapshot.RamUsagePercent.Value.ToString("0") + "%" : "Процент использования недоступен";\n                DiskPageValue.Text = snapshot.DiskUsedGb.HasValue && snapshot.DiskTotalGb.HasValue ? snapshot.DiskUsedGb.Value.ToString("0.0") + " / " + snapshot.DiskTotalGb.Value.ToString("0.0") + " ГБ" : "Недоступно";\n                DiskPageDetail.Text = snapshot.DiskUsagePercent.HasValue ? "Занято " + snapshot.DiskUsagePercent.Value.ToString("0") + "%" : "Процент использования недоступен";\n                _historyStore.Add(new HistoryEntry { Timestamp = DateTime.Now, CpuPercent = snapshot.CpuUsage, RamPercent = snapshot.RamUsagePercent, DiskPercent = snapshot.DiskUsagePercent });
                CpuUsageValue.Text = snapshot.CpuUsage.HasValue ? snapshot.CpuUsage.Value.ToString("0", CultureInfo.InvariantCulture) + "%" : "Ожидание...";
                CpuUsageDetail.Text = snapshot.CpuUsage.HasValue ? "Текущая загрузка процессора" : "Сбор первого измерения";
                RamUsageValue.Text = snapshot.RamUsedGb.HasValue && snapshot.RamTotalGb.HasValue
                    ? snapshot.RamUsedGb.Value.ToString("0.0") + " / " + snapshot.RamTotalGb.Value.ToString("0.0") + " ГБ" : "Недоступно";
                RamUsageDetail.Text = snapshot.RamUsagePercent.HasValue ? "Используется " + snapshot.RamUsagePercent.Value.ToString("0") + "%" : "Данные памяти недоступны";
                DiskUsageValue.Text = snapshot.DiskUsedGb.HasValue && snapshot.DiskTotalGb.HasValue
                    ? snapshot.DiskUsedGb.Value.ToString("0") + " / " + snapshot.DiskTotalGb.Value.ToString("0") + " ГБ" : "Недоступно";
                DiskUsageDetail.Text = snapshot.DiskUsagePercent.HasValue ? "Занято " + snapshot.DiskUsagePercent.Value.ToString("0") + "% на готовых дисках" : "Нет доступных дисков";
            }
            catch (Exception)
            {
                CpuUsageValue.Text = "Недоступно"; CpuUsageDetail.Text = "Ошибка чтения счётчика";
                RamUsageValue.Text = "Недоступно"; RamUsageDetail.Text = "Ошибка чтения памяти";
                DiskUsageValue.Text = "Недоступно"; DiskUsageDetail.Text = "Ошибка чтения дисков";
            }
        }

        private void UpdateNetwork()
        {
            try
            {
                NetworkSnapshot snapshot = _networkMonitor.Read();
                NetworkDownload.Text = snapshot.DownloadBytesPerSecond.HasValue ? FormatRate(snapshot.DownloadBytesPerSecond.Value) : "Сбор первого измерения";
                NetworkUpload.Text = snapshot.UploadBytesPerSecond.HasValue ? FormatRate(snapshot.UploadBytesPerSecond.Value) : "Сбор первого измерения";
                NetworkAdapters.Text = "Активные сетевые адаптеры: " + snapshot.ActiveAdapters;
                NetworkTotals.Text = "Получено: " + FormatBytes(snapshot.TotalReceivedBytes) + "  |  Отправлено: " + FormatBytes(snapshot.TotalSentBytes);
            }
            catch (Exception)
            {
                NetworkDownload.Text = "Недоступно"; NetworkUpload.Text = "Недоступно";
                NetworkAdapters.Text = "Не удалось прочитать сетевые адаптеры";
            }
        }

        private static string FormatRate(double bytesPerSecond)
        {
            if (bytesPerSecond >= 1048576d) return (bytesPerSecond / 1048576d).ToString("0.00") + " МБ/с";
            if (bytesPerSecond >= 1024d) return (bytesPerSecond / 1024d).ToString("0.0") + " КБ/с";
            return bytesPerSecond.ToString("0") + " Б/с";
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes >= 1073741824L) return (bytes / 1073741824d).ToString("0.00") + " ГБ";
            if (bytes >= 1048576L) return (bytes / 1048576d).ToString("0.0") + " МБ";
            return (bytes / 1024d).ToString("0") + " КБ";
        }

        private void LoadProcesses()
        {
            try
            {
                var rows = _processMonitor.Read();
                ProcessesGrid.ItemsSource = rows;
                ProcessCount.Text = "Найдено: " + rows.Count;
            }
            catch (Exception)
            {
                ProcessesGrid.ItemsSource = null;
                ProcessCount.Text = "Не удалось получить список процессов";
            }
        }

        private void RefreshProcesses_Click(object sender, RoutedEventArgs e) { LoadProcesses(); }\n\n        private void RefreshHistory_Click(object sender, RoutedEventArgs e) { LoadHistory(); }\n\n        private void LoadHistory()\n        {\n            var entries = _historyStore.ReadLatest(20);\n            if (entries.Count == 0) { HistorySummary.Text = "История пока пуста."; return; }\n            var latest = entries[0];\n            HistorySummary.Text = "Записей: " + entries.Count + " из последних измерений.\\nПоследнее измерение: " + latest.Timestamp.ToString("g") + "\\nCPU: " + (latest.CpuPercent.HasValue ? latest.CpuPercent.Value.ToString("0") + "%" : "Недоступно") + "\\nRAM: " + (latest.RamPercent.HasValue ? latest.RamPercent.Value.ToString("0") + "%" : "Недоступно") + "\\nDisk: " + (latest.DiskPercent.HasValue ? latest.DiskPercent.Value.ToString("0") + "%" : "Недоступно");\n        }\n\n        private void SaveSettings_Click(object sender, RoutedEventArgs e)\n        {\n            int index = RefreshIntervalBox.SelectedIndex;\n            _settings.RefreshSeconds = index == 2 ? 5 : (index == 1 ? 2 : 1);\n            _settings.NotificationsEnabled = NotificationsCheck.IsChecked == true;\n            _settingsStore.Save(_settings);\n            _monitorTimer.Interval = TimeSpan.FromSeconds(_settings.RefreshSeconds);\n            SettingsStatus.Text = "Настройки сохранены.";\n        }
        private void RefreshHardware_Click(object sender, RoutedEventArgs e) { LoadHardwareInformation(); }

        private void LoadHardwareInformation()
        {
            HardwareCpu.Text = "Загрузка..."; HardwareGpu.Text = "Загрузка...";
            HardwareRam.Text = "Загрузка..."; HardwareDisks.Text = "Загрузка..."; HardwareOs.Text = "Загрузка...";
            try
            {
                HardwareInformation info = HardwareInformation.Read();
                HardwareCpu.Text = info.Processor; HardwareGpu.Text = info.Graphics;
                HardwareRam.Text = info.Memory; HardwareDisks.Text = info.Drives; HardwareOs.Text = info.OperatingSystem;
            }
            catch (Exception)
            {
                HardwareCpu.Text = "Недоступно"; HardwareGpu.Text = "Недоступно";
                HardwareRam.Text = "Недоступно"; HardwareDisks.Text = "Недоступно";
                HardwareOs.Text = "Не удалось получить сведения. Попробуйте обновить данные.";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _monitorTimer.Stop();
            if (_systemMonitor != null) _systemMonitor.Dispose();
            base.OnClosed(e);
        }

        private void Navigation_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            if (button == null) return;
            string section = button.Tag as string;
            if (string.IsNullOrWhiteSpace(section)) return;

            PageTitle.Text = section;
            DashboardContent.Visibility = Visibility.Collapsed;
            PlaceholderContent.Visibility = Visibility.Collapsed;
            HardwareContent.Visibility = Visibility.Collapsed;
            NetworkContent.Visibility = Visibility.Collapsed;
            ProcessesContent.Visibility = Visibility.Collapsed;

            if (section == "Обзор")
            {
                PageSubtitle.Text = "Состояние и основные показатели компьютера";
                DashboardContent.Visibility = Visibility.Visible;
                return;
            }
            if (section == "Процессор") { PageSubtitle.Text = "Загрузка центрального процессора"; CpuContent.Visibility = Visibility.Visible; UpdateMonitoring(); return; }\n            if (section == "Оперативная память") { PageSubtitle.Text = "Использование физической памяти"; RamContent.Visibility = Visibility.Visible; UpdateMonitoring(); return; }\n            if (section == "Диски") { PageSubtitle.Text = "Занятое и свободное пространство"; DiskContent.Visibility = Visibility.Visible; UpdateMonitoring(); return; }\n            if (section == "История") { PageSubtitle.Text = "Локально сохранённые измерения"; HistoryContent.Visibility = Visibility.Visible; LoadHistory(); return; }\n            if (section == "Настройки") { PageSubtitle.Text = "Параметры работы приложения"; SettingsContent.Visibility = Visibility.Visible; return; }\n            if (section == "Оборудование")
            {
                PageSubtitle.Text = "Физические компоненты и сведения об операционной системе";
                HardwareContent.Visibility = Visibility.Visible;
                LoadHardwareInformation();
                return;
            }
            if (section == "Сеть")
            {
                PageSubtitle.Text = "Текущая скорость и счётчики сетевого трафика";
                NetworkContent.Visibility = Visibility.Visible;
                UpdateNetwork();
                return;
            }
            if (section == "Процессы")
            {
                PageSubtitle.Text = "Список процессов Windows и потребление памяти";
                ProcessesContent.Visibility = Visibility.Visible;
                LoadProcesses();
                return;
            }

            PageSubtitle.Text = "Раздел PCore";
            PlaceholderContent.Visibility = Visibility.Visible;
            PlaceholderTitle.Text = section;
            PlaceholderDescription.Text = "Этот раздел пока находится в разработке. Его функциональность будет добавлена поэтапно.";
        }
    }
}