using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace FluentTB
{
    /// <summary>
    /// "Help and About" window for FluentTB.
    /// </summary>
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void okButton_Click(object sender, RoutedEventArgs e) => Close();

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // ShellExecute the URI so the default browser opens it
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
            {
                UseShellExecute = true
            });
            e.Handled = true;
        }

        private void configButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(
                ((MainWindow)Application.Current.MainWindow).configPath)
            {
                UseShellExecute = true
            });
        }

        private void logButton_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo(
                ((MainWindow)Application.Current.MainWindow).logPath)
            {
                UseShellExecute = true
            });
        }
    }
}
