using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace PCore.App
{
    public partial class MainWindow : Window
    {
        private readonly DispatcherTimer _monitorTimer;
        private SystemMonitor _systemMonitor;

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
        }

        private void UpdateMonitoring()
        {
            if (_systemMonitor == null) return;

            try
            {
                SystemMonitorSnapshot snapshot = _systemMonitor.Read();
                CpuUsageValue.Text = snapshot.CpuUsage.HasValue
                    ? snapshot.CpuUsage.Value.ToString("0") + "%"
                    : "Ожидание...";
                CpuUsageDetail.Text = snapshot.CpuUsage.HasValue
                    ? "Текущая загрузка процессора"
                    : "Сбор первого измерения";

                RamUsageValue.Text = snapshot.RamUsedGb.HasValue && snapshot.RamTotalGb.HasValue
                    ? snapshot.RamUsedGb.Value.ToString("0.0") + " / " + snapshot.RamTotalGb.Value.ToString("0.0") + " ГБ"
                    : "Недоступно";
                RamUsageDetail.Text = snapshot.RamUsagePercent.HasValue
                    ? "Используется " + snapshot.RamUsagePercent.Value.ToString("0") + "%"
                    : "Данные памяти недоступны";

                DiskUsageValue.Text = snapshot.DiskUsedGb.HasValue && snapshot.DiskTotalGb.HasValue
                    ? snapshot.DiskUsedGb.Value.ToString("0") + " / " + snapshot.DiskTotalGb.Value.ToString("0") + " ГБ"
                    : "Недоступно";
                DiskUsageDetail.Text = snapshot.DiskUsagePercent.HasValue
                    ? "Занято " + snapshot.DiskUsagePercent.Value.ToString("0") + "% на готовых дисках"
                    : "Нет доступных дисков";
            }
            catch (Exception)
            {
                CpuUsageValue.Text = "Недоступно";
                CpuUsageDetail.Text = "Ошибка чтения счётчика";
                RamUsageValue.Text = "Недоступно";
                RamUsageDetail.Text = "Ошибка чтения памяти";
                DiskUsageValue.Text = "Недоступно";
                DiskUsageDetail.Text = "Ошибка чтения дисков";
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            _monitorTimer.Stop();
            if (_systemMonitor != null)
                _systemMonitor.Dispose();
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

            if (section == "Обзор")
            {
                PageSubtitle.Text = "Состояние и основные показатели компьютера";
                DashboardContent.Visibility = Visibility.Visible;
                return;
            }

            if (section == "Оборудование")
            {
                PageSubtitle.Text = "Физические компоненты и сведения об операционной системе";
                HardwareContent.Visibility = Visibility.Visible;
                LoadHardwareInformation();
                return;
            }

            PageSubtitle.Text = "Раздел PCore";
            PlaceholderContent.Visibility = Visibility.Visible;
            PlaceholderTitle.Text = section;
            PlaceholderDescription.Text = "Этот раздел пока находится в разработке. Его функциональность будет добавлена поэтапно.";
        }

        private void RefreshHardware_Click(object sender, RoutedEventArgs e)
        {
            LoadHardwareInformation();
        }

        private void LoadHardwareInformation()
        {
            HardwareCpu.Text = "Загрузка...";
            HardwareGpu.Text = "Загрузка...";
            HardwareRam.Text = "Загрузка...";
            HardwareDisks.Text = "Загрузка...";
            HardwareOs.Text = "Загрузка...";

            try
            {
                HardwareInformation info = HardwareInformation.Read();
                HardwareCpu.Text = info.Processor;
                HardwareGpu.Text = info.Graphics;
                HardwareRam.Text = info.Memory;
                HardwareDisks.Text = info.Drives;
                HardwareOs.Text = info.OperatingSystem;
            }
            catch (Exception)
            {
                HardwareCpu.Text = "Недоступно";
                HardwareGpu.Text = "Недоступно";
                HardwareRam.Text = "Недоступно";
                HardwareDisks.Text = "Недоступно";
                HardwareOs.Text = "Не удалось получить сведения. Попробуйте обновить данные.";
            }
        }
    }
}