using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.Windows.Media;
using System.Windows.Shapes;

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
        private readonly HardwareSensorMonitor _hardwareSensorMonitor = new HardwareSensorMonitor();
        private HardwareSensorSnapshot _lastHardwareSensors;
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
                if (DateTime.Now - _lastHistoryWrite >= TimeSpan.FromSeconds(10))
                {
                    try { _historyStore.Add(new HistoryEntry { Timestamp = DateTime.Now, CpuPercent = snapshot.CpuUsage, RamPercent = snapshot.RamUsagePercent, DiskPercent = snapshot.DiskUsagePercent }); _lastHistoryWrite = DateTime.Now; }
                    catch (Exception) { }
                }
                GpuSnapshot gpu = _gpuMonitor.Read();
                _lastHardwareSensors = _hardwareSensorMonitor.Read();
                HardwareSensorSnapshot sensors = _lastHardwareSensors;
                string cpuTemperature = sensors.CpuTemperature.HasValue ? sensors.CpuTemperature.Value.ToString("0") + " °C" : "нет показаний";
                string gpuTemperature = sensors.GpuTemperature.HasValue ? sensors.GpuTemperature.Value.ToString("0") + " °C" : "нет показаний";
                string cpuClock = sensors.CpuClock.HasValue ? sensors.CpuClock.Value.ToString("0") + " МГц" : "нет данных";
                string gpuClock = sensors.GpuClock.HasValue ? sensors.GpuClock.Value.ToString("0") + " МГц" : "нет данных";
                SensorStatusText.Text = sensors.Status;
                GpuUsageValue.Text = sensors.GpuLoad.HasValue ? sensors.GpuLoad.Value.ToString("0") + "%" : (gpu.UtilizationPercent.HasValue ? gpu.UtilizationPercent.Value.ToString("0") + "%" : "Недоступно");
                GpuUsageDetail.Text = "Температура: " + gpuTemperature + " | Частота: " + gpuClock;
                CpuUsageDetail.Text = "Температура: " + cpuTemperature + " | Частота: " + cpuClock;
                CpuPageSensorDetail.Text = "Температура: " + cpuTemperature + " | Частота: " + cpuClock;
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
            var entries = _historyStore.ReadLatest(60);
            HistoryChart.Children.Clear();
            if (entries.Count == 0)
            {
                HistorySummary.Text = "История пока пуста.";
                HistoryStatistics.Text = "";
                return;
            }

            var chronological = new System.Collections.Generic.List<HistoryEntry>(entries);
            chronological.Reverse();
            HistorySummary.Text = "Показано точек: " + chronological.Count + " | Последнее измерение: " + entries[0].Timestamp.ToString("g");
            HistoryStatistics.Text = "CPU: " + FormatHistoryStats(chronological, 0) +
                "    RAM: " + FormatHistoryStats(chronological, 1) +
                "    Диск: " + FormatHistoryStats(chronological, 2);

            DrawHistoryLine(chronological, 0, Brushes.White);
            DrawHistoryLine(chronological, 1, Brushes.LightGray);
            DrawHistoryLine(chronological, 2, Brushes.Gray);
        }

        private static string FormatHistoryStats(System.Collections.Generic.IList<HistoryEntry> entries, int series)
        {
            double total = 0, min = double.MaxValue, max = double.MinValue;
            int count = 0;
            foreach (HistoryEntry entry in entries)
            {
                double? value = series == 0 ? entry.CpuPercent : (series == 1 ? entry.RamPercent : entry.DiskPercent);
                if (!value.HasValue) continue;
                total += value.Value;
                min = Math.Min(min, value.Value);
                max = Math.Max(max, value.Value);
                count++;
            }
            if (count == 0) return "недоступно";
            return "ср. " + (total / count).ToString("0") + "% (мин. " + min.ToString("0") + ", макс. " + max.ToString("0") + "%)";
        }

        private void DrawHistoryLine(System.Collections.Generic.IList<HistoryEntry> entries, int series, Brush brush)
        {
            var points = new PointCollection();
            int valid = 0;
            for (int i = 0; i < entries.Count; i++)
            {
                double? value = series == 0 ? entries[i].CpuPercent : (series == 1 ? entries[i].RamPercent : entries[i].DiskPercent);
                if (!value.HasValue) continue;
                double x = entries.Count <= 1 ? 0 : (HistoryChart.ActualWidth > 0 ? HistoryChart.ActualWidth : 700) * i / (entries.Count - 1);
                double y = 160 - Math.Max(0, Math.Min(100, value.Value)) * 1.5;
                points.Add(new Point(x, y));
                valid++;
            }
            if (valid < 2) return;
            HistoryChart.Children.Add(new Polyline { Points = points, Stroke = brush, StrokeThickness = 2, SnapsToDevicePixels = true });
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
            _hardwareSensorMonitor.Dispose();
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
            if (section == "Датчики" || section == "Видеокарта")
            {
                if (_lastHardwareSensors == null)
                    _lastHardwareSensors = _hardwareSensorMonitor.Read();

                AddText("Аппаратные датчики", 15, true);
                AddText(_lastHardwareSensors.Status, 11, false);
                AddText("Температуры, частоты, напряжения, нагрузки, обороты вентиляторов и доступные показатели памяти/накопителей считываются непосредственно с оборудования. Набор зависит от поддержки датчиков производителем.", 11, false);
                AddAction("Обновить датчики", (sender, args) =>
                {
                    UpdateMonitoring();
                    BuildDynamicPage(section);
                });

                int shownSensors = 0;
                foreach (HardwareSensorReading sensor in _lastHardwareSensors.Sensors)
                {
                    bool isGpu = sensor.HardwareType.IndexOf("Gpu", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (section == "Видеокарта" && !isGpu) continue;

                    string range = "";
                    if (sensor.Minimum.HasValue || sensor.Maximum.HasValue)
                        range = " (мин.: " + (sensor.Minimum.HasValue ? sensor.Minimum.Value.ToString("0.##") : "—") +
                            ", макс.: " + (sensor.Maximum.HasValue ? sensor.Maximum.Value.ToString("0.##") : "—") + ")";
                    AddText(sensor.HardwareName + " | " + sensor.Name + ": " + sensor.DisplayValue + range, 12, false);
                    shownSensors++;
                }

                if (shownSensors == 0)
                    AddText(section == "Видеокарта"
                        ? "Датчики видеокарты пока не предоставляют показания. Попробуйте запустить PCore от имени администратора и обновить драйвер видеокарты."
                        : "Датчики пока не предоставляют показания. Некоторые устройства требуют запуска от имени администратора или не поддерживаются прошивкой.", 12, false);
                return;
            }

            if (section == "Оптимизация")
            {
                AddText("Профиль слабого ПК уменьшает нагрузку именно от PCore: реже обновляет мониторинг и снижает приоритет процесса приложения. Он не отключает службы Windows, не завершает чужие процессы и не меняет системные настройки без вашего согласия.", 12, false);
                AddText("Рекомендуемый режим", 14, true);
                AddAction("Включить профиль слабого ПК", ApplyLowResourceProfile_Click);
                AddAction("Вернуть обычный режим PCore", RestoreNormalProfile_Click);
                AddText("Дополнительные настройки Windows", 14, true);
                AddText("Открывайте системные параметры Windows и самостоятельно выбирайте, что менять. Перед отключением автозагрузки убедитесь, что приложение вам не требуется.", 11, false);
                AddAction("Управление автозагрузкой Windows", OpenStartupSettings_Click);
                AddAction("Питание и режим энергосбережения", OpenPowerSettings_Click);
                AddAction("Игровой режим Windows", OpenGameModeSettings_Click);
                AddAction("Настройки графики Windows", OpenGraphicsSettings_Click);
                AddAction("Контроль памяти и хранилища", OpenStorageSettings_Click);
                AddText("Важно: PCore пока не выполняет автоматическую очистку реестра, удаление системных файлов, отключение служб или разгон. Такие действия могут навредить системе и не гарантируют ускорения.", 11, false);
                return;
            }
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

        private void ApplyLowResourceProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _settings.RefreshSeconds = 5;
                _settingsStore.Save(_settings);
                _monitorTimer.Interval = TimeSpan.FromSeconds(5);
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.BelowNormal;
                AddText("Профиль слабого ПК включён: мониторинг PCore обновляется раз в 5 секунд, приоритет приложения снижен. Изменения применены только к PCore.", 12, true);
            }
            catch (Exception ex)
            {
                AddText("Не удалось полностью применить профиль: " + ex.Message, 11, false);
            }
        }

        private void RestoreNormalProfile_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _settings.RefreshSeconds = 1;
                _settingsStore.Save(_settings);
                _monitorTimer.Interval = TimeSpan.FromSeconds(1);
                System.Diagnostics.Process.GetCurrentProcess().PriorityClass = System.Diagnostics.ProcessPriorityClass.Normal;
                AddText("Обычный режим PCore восстановлен: интервал мониторинга — 1 секунда, приоритет процесса — обычный.", 12, true);
            }
            catch (Exception ex)
            {
                AddText("Не удалось полностью восстановить обычный режим: " + ex.Message, 11, false);
            }
        }

        private void OpenWindowsSettings(string settingsUri)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = settingsUri,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                AddText("Не удалось открыть параметры Windows: " + ex.Message, 11, false);
            }
        }

        private void OpenStartupSettings_Click(object sender, RoutedEventArgs e) { OpenWindowsSettings("ms-settings:startupapps"); }
        private void OpenPowerSettings_Click(object sender, RoutedEventArgs e) { OpenWindowsSettings("ms-settings:powersleep"); }
        private void OpenGameModeSettings_Click(object sender, RoutedEventArgs e) { OpenWindowsSettings("ms-settings:gaming-gamemode"); }
        private void OpenGraphicsSettings_Click(object sender, RoutedEventArgs e) { OpenWindowsSettings("ms-settings:display-advancedgraphics"); }
        private void OpenStorageSettings_Click(object sender, RoutedEventArgs e) { OpenWindowsSettings("ms-settings:storagesense"); }

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

            PageSubtitle.Text = section == "Оптимизация" ? "Инструменты и безопасные настройки для маломощных компьютеров" : "Раздел PCore";
            PlaceholderContent.Visibility = Visibility.Visible;
            PlaceholderTitle.Text = section;
            PlaceholderDescription.Text = "Выберите действие ниже.";
            BuildDynamicPage(section);
        }
    }
}