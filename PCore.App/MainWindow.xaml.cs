using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;

namespace PCore.App
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _monitorTimer;
        private SystemMonitor _systemMonitor;
        private readonly NetworkMonitor _networkMonitor = new NetworkMonitor();
        private readonly ProcessMonitor _processMonitor = new ProcessMonitor();
        private readonly HistoryStore _historyStore = new HistoryStore();
        private readonly SettingsStore _settingsStore = new SettingsStore();
        private UserSettings _settings;
        private readonly GpuMonitor _gpuMonitor = new GpuMonitor();
        private readonly StorageAnalyzer _storageAnalyzer = new StorageAnalyzer();
        private readonly StartupManager _startupManager = new StartupManager();
        private readonly GameCatalog _gameCatalog = new GameCatalog();
        private readonly BenchmarkService _benchmarkService = new BenchmarkService();
        private readonly SystemAlertService _alertService = new SystemAlertService();
        private readonly LicenseService _licenseService = new LicenseService();
        private DateTime _lastHistoryWrite = DateTime.MinValue;

        public MainWindow()
        {
            InitializeComponent();
            _settings = _settingsStore.Load();
            _monitorTimer = new DispatcherTimer();
            _monitorTimer.Interval = TimeSpan.FromSeconds(Math.Max(1, _settings.RefreshSeconds));
            RefreshIntervalBox.SelectedIndex = _settings.RefreshSeconds == 5 ? 2 : (_settings.RefreshSeconds == 2 ? 1 : 0);
            NotificationsCheck.IsChecked = _settings.NotificationsEnabled;
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
                SystemMonitorSnapshot snapshot = _systemMonitor.Read();
                _alertService.Evaluate(snapshot, 90, TimeSpan.FromMinutes(5));
                CpuPageValue.Text = snapshot.CpuUsage.HasValue ? snapshot.CpuUsage.Value.ToString("0") + "%" : "Недоступно";
                RamPageValue.Text = snapshot.RamUsedGb.HasValue && snapshot.RamTotalGb.HasValue ? snapshot.RamUsedGb.Value.ToString("0.0") + " / " + snapshot.RamTotalGb.Value.ToString("0.0") + " ГБ" : "Недоступно";
                RamPageDetail.Text = snapshot.RamUsagePercent.HasValue ? "Используется " + snapshot.RamUsagePercent.Value.ToString("0") + "%" : "Процент использования недоступен";
                DiskPageValue.Text = snapshot.DiskUsedGb.HasValue && snapshot.DiskTotalGb.HasValue ? snapshot.DiskUsedGb.Value.ToString("0.0") + " / " + snapshot.DiskTotalGb.Value.ToString("0.0") + " ГБ" : "Недоступно";
                DiskPageDetail.Text = snapshot.DiskUsagePercent.HasValue ? "Занято " + snapshot.DiskUsagePercent.Value.ToString("0") + "%" : "Процент использования недоступен";
                if (DateTime.Now - _lastHistoryWrite >= TimeSpan.FromSeconds(10))\n                {\n                    try { _historyStore.Add(new HistoryEntry { Timestamp = DateTime.Now, CpuPercent = snapshot.CpuUsage, RamPercent = snapshot.RamUsagePercent, DiskPercent = snapshot.DiskUsagePercent }); _lastHistoryWrite = DateTime.Now; }\n                    catch (Exception) { }\n                }\n                GpuSnapshot gpu = _gpuMonitor.Read();\n                GpuUsageValue.Text = gpu.UtilizationPercent.HasValue ? gpu.UtilizationPercent.Value.ToString("0") + "%" : "Недоступно";\n                GpuUsageDetail.Text = gpu.Status + " | Температура: недоступно";
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

        private void RefreshProcesses_Click(object sender, RoutedEventArgs e) { LoadProcesses(); }

        private void RefreshHistory_Click(object sender, RoutedEventArgs e) { LoadHistory(); }

        private void LoadHistory()
        {
            var entries = _historyStore.ReadLatest(20);
            if (entries.Count == 0)
            {
                HistorySummary.Text = "История пока пуста.";
                return;
            }

            var latest = entries[0];
            string nl = Environment.NewLine;
            HistorySummary.Text = "Записей: " + entries.Count + " из последних измерений." + nl
                + "Последнее измерение: " + latest.Timestamp.ToString("g") + nl
                + "CPU: " + (latest.CpuPercent.HasValue ? latest.CpuPercent.Value.ToString("0") + "%" : "Недоступно") + nl
                + "RAM: " + (latest.RamPercent.HasValue ? latest.RamPercent.Value.ToString("0") + "%" : "Недоступно") + nl
                + "Disk: " + (latest.DiskPercent.HasValue ? latest.DiskPercent.Value.ToString("0") + "%" : "Недоступно");
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            int index = RefreshIntervalBox.SelectedIndex;
            _settings.RefreshSeconds = index == 2 ? 5 : (index == 1 ? 2 : 1);
            _settings.NotificationsEnabled = NotificationsCheck.IsChecked == true;
            _settingsStore.Save(_settings);
            _monitorTimer.Interval = TimeSpan.FromSeconds(_settings.RefreshSeconds);
            SettingsStatus.Text = "Настройки сохранены.";
        }
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


        private TextBlock AddText(string text, double size, bool bold)
        {
            var block = new TextBlock { Text = text, FontSize = size, FontWeight = bold ? FontWeights.SemiBold : FontWeights.Normal,
                Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E8E8EA")),
                TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 5, 0, 5) };
            DynamicPageContent.Children.Add(block);
            return block;
        }

        private Button AddAction(string text, RoutedEventHandler action)
        {
            var button = new Button { Content = text, Width = 220, HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(0, 8, 0, 8) };
            button.Click += action;
            DynamicPageContent.Children.Add(button);
            return button;
        }

        private void BuildDynamicPage(string section)
        {
            DynamicPageContent.Children.Clear();
            AddText(section, 19, true);
            if (section == "Хранилище")
            {
                foreach (StorageDrive drive in _storageAnalyzer.ReadDrives())
                    AddText(drive.Name + "  " + drive.Label + "  " + drive.Format + Environment.NewLine +
                        "Занято: " + drive.UsedGb.ToString("0.0") + " ГБ из " + drive.TotalGb.ToString("0.0") +
                        " ГБ (" + drive.UsedPercent.ToString("0") + "%), свободно: " + drive.FreeGb.ToString("0.0") + " ГБ", 13, false);
                AddText("Показаны готовые тома Windows. Удаление файлов не выполняется.", 11, false);
                return;
            }
            if (section == "Автозагрузка")
            {
                AddText("Элементы автозапуска из реестра Windows и папок Startup. Просмотр безопасен: PCore пока ничего не отключает.", 12, false);
                foreach (StartupEntry item in _startupManager.ReadEntries())
                    AddText(item.Name + " — " + item.Location + Environment.NewLine + item.Command, 12, false);
                return;
            }
            if (section == "Игровой центр")
            {
                AddText("Обнаружение игр и игровых платформ по зарегистрированным установкам Windows.", 12, false);
                IList<GameEntry> games = _gameCatalog.FindInstalledGames();
                if (games.Count == 0) AddText("Подходящие записи об играх не найдены. Портативные игры могут не отображаться.", 12, false);
                foreach (GameEntry game in games) AddText(game.Name + (string.IsNullOrWhiteSpace(game.Publisher) ? "" : " — " + game.Publisher) +
                    (string.IsNullOrWhiteSpace(game.InstallLocation) ? "" : Environment.NewLine + game.InstallLocation), 12, false);
                return;
            }
            if (section == "Benchmark")
            {
                AddText("Лёгкий CPU-тест на 5 секунд. Это диагностическая нагрузка, не универсальный рейтинг производительности.", 12, false);
                AddAction("Запустить CPU Benchmark", RunBenchmark_Click);
                return;
            }
            if (section == "Оповещения")
            {
                AddText("PCore отслеживает высокую загрузку CPU и RAM во время работы приложения. Порог: 90%, повтор не чаще 5 минут.", 12, false);
                _alertService.AlertRaised -= AlertService_AlertRaised;
                _alertService.AlertRaised += AlertService_AlertRaised;
                AddText("Служба оповещений подключена к циклу мониторинга.", 12, false);
                return;
            }
            if (section == "Инструменты")
            {
                AddText("NetSpeed: текущая скорость сети доступна в разделе «Сеть». Storage: обзор дисков доступен в разделе «Хранилище».", 12, false);
                AddText("Window Inspector и расширенные очистка/оптимизация пока не выполняют действий с системой.", 12, false);
                return;
            }
            if (section == "Overlay" || section == "Widgets" || section == "Quick Menu")
            {
                AddText("Компонент подготовлен как отдельный модуль интерфейса, но постоянное игровое Overlay, глобальные горячие клавиши и виджеты рабочего стола требуют отдельной реализации и тестирования на Windows.", 12, false);
                AddAction("Открыть компактную панель PCore", OpenMiniPanel_Click);
                return;
            }
            if (section == "О программе")
            {
                AddText("PCore 0.2 development build", 14, true);
                AddText("Лицензия: " + _licenseService.Status, 12, false);
                AddText("PRO — $1.99/мес. MAX — $4.99/мес. Покупка и активация пока отключены до подключения безопасной серверной проверки лицензий и платёжного провайдера.", 12, false);
                return;
            }
            AddText("Раздел создан. Дополнительные функции будут подключаться после проверки совместимости.", 12, false);
        }

        private void RunBenchmark_Click(object sender, RoutedEventArgs e)
        {
            AddText("Выполняется тест CPU (до 5 секунд)...", 12, false);
            BenchmarkResult result = _benchmarkService.RunCpuTest(5);
            AddText("Результат: " + result.Operations.ToString("N0") + " операций за " + result.Duration.TotalSeconds.ToString("0.00") + " с. " + result.Note, 13, true);
        }

        private void AlertService_AlertRaised(object sender, SystemAlertEventArgs e)
        {
            if (_settings.NotificationsEnabled)
                MessageBox.Show(e.Message, e.Title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void OpenMiniPanel_Click(object sender, RoutedEventArgs e)
        {
            var panel = new Window { Title = "PCore Mini", Width = 330, Height = 190, WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = this, Topmost = true, ResizeMode = ResizeMode.NoResize, Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#15161A")) };
            var stack = new StackPanel { Margin = new Thickness(18) };
            stack.Children.Add(new TextBlock { Text = "PCore", FontSize = 18, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White });
            stack.Children.Add(new TextBlock { Text = "CPU: " + CpuUsageValue.Text + "   GPU: " + GpuUsageValue.Text, Margin = new Thickness(0, 12, 0, 5), Foreground = Brushes.White });
            stack.Children.Add(new TextBlock { Text = "RAM: " + RamUsageValue.Text, Foreground = Brushes.White });
            stack.Children.Add(new TextBlock { Text = "Это компактное окно, не игровой overlay.", Margin = new Thickness(0, 12, 0, 0), Foreground = Brushes.Gray, FontSize = 10 });
            panel.Content = stack;
            panel.Show();
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
            if (section == "Процессор") { PageSubtitle.Text = "Загрузка центрального процессора"; CpuContent.Visibility = Visibility.Visible; UpdateMonitoring(); return; }
            if (section == "Оперативная память") { PageSubtitle.Text = "Использование физической памяти"; RamContent.Visibility = Visibility.Visible; UpdateMonitoring(); return; }
            if (section == "Диски") { PageSubtitle.Text = "Занятое и свободное пространство"; DiskContent.Visibility = Visibility.Visible; UpdateMonitoring(); return; }
            if (section == "История") { PageSubtitle.Text = "Локально сохранённые измерения"; HistoryContent.Visibility = Visibility.Visible; LoadHistory(); return; }
            if (section == "Настройки") { PageSubtitle.Text = "Параметры работы приложения"; SettingsContent.Visibility = Visibility.Visible; return; }
            if (section == "Оборудование")
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