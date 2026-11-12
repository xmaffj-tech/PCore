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
            if (button == null)
                return;

            string section = button.Tag as string;
            if (string.IsNullOrWhiteSpace(section))
                return;

            PageTitle.Text = section;

            if (section == "Обзор")
            {
                PageSubtitle.Text = "Состояние и основные показатели компьютера";
                DashboardContent.Visibility = Visibility.Visible;
                PlaceholderContent.Visibility = Visibility.Collapsed;
                return;
            }

            PageSubtitle.Text = "Раздел PCore";
            DashboardContent.Visibility = Visibility.Collapsed;
            PlaceholderContent.Visibility = Visibility.Visible;
            PlaceholderTitle.Text = section;
            PlaceholderDescription.Text = "Этот раздел пока находится в разработке. Его функциональность будет добавлена поэтапно.";
        }
    }
}
