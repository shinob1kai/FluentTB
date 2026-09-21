using System.ComponentModel;
using System.Windows;
namespace FluentTB.Public;
public partial class SettingsWindow : Wpf.Ui.Controls.FluentWindow
{
    public SettingsWindow() { InitializeComponent(); }
    private void OnLoadedWindow(object sender, RoutedEventArgs e) { Navigation.Navigate(typeof(HomePage)); }
    private void OnClosingWindow(object? sender, CancelEventArgs e) { e.Cancel = true; Hide(); }
    internal void Navigate(Type page) => Navigation.Navigate(page);
}
