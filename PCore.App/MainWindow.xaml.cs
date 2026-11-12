using System;
using System.Windows;
using System.Windows.Controls;

namespace PCore.App
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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