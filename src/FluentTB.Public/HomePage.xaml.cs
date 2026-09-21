using System.Windows;
using System.Windows.Controls;
using FluentTB.Release;
namespace FluentTB.Public;
public partial class HomePage : Page
{
    public HomePage()
    {
        InitializeComponent();
        DataContext = App.Settings;
        VersionTextBlock.Text = "v" + AppRelease.Version;
        UpdateStatusText.Text = App.German ? "Nach Updates suchen" : "Check for updates";
        LastCheckedText.Text = App.German ? "Noch nicht geprüft" : "Not checked yet";
        Loaded += (_, _) => TaskbarStatusText.SetResourceReference(TextBlock.TextProperty, FluentTB.Integration.TaskbarIntegration.Engine.activeSettings.TaskbarShapeEnabled ? "Enabled" : "Disabled");
        Loaded += (_, _) => LockStatusText.SetResourceReference(TextBlock.TextProperty, App.Settings.Enabled ? "Enabled" : "Disabled");
    }
    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        CheckButton.IsEnabled = false;
        UpdateStatusText.Text = App.German ? "Wird geprüft …" : "Checking …";
        try { UpdateStatusText.Text = await AppRelease.CheckAsync(App.German); LastCheckedText.Text = DateTime.Now.ToString("g"); }
        finally { CheckButton.IsEnabled = true; }
    }
    private void ViewUpdates_Click(object sender, RoutedEventArgs e) => AppRelease.OpenReleases();
    private void Taskbar_Click(object sender, RoutedEventArgs e) => ((SettingsWindow)Window.GetWindow(this)).Navigate(typeof(FluentFlyoutWPF.Pages.FluentTaskbarPage));
    private void LockKeys_Click(object sender, RoutedEventArgs e) => ((SettingsWindow)Window.GetWindow(this)).Navigate(typeof(LockPage));
}
