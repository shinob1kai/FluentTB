using System.Windows;
using System.Windows.Controls;
namespace FluentTB.Public;
public partial class LockPage : Page
{
    public LockPage() { InitializeComponent(); DataContext = App.Settings; SaveButton.Content = App.German ? "Speichern" : "Save"; }
    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try { App.Settings.Save(); StatusText.Text = App.German ? "Sperrtasten-Einstellungen gespeichert" : "Lock-key settings saved"; }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { StatusText.Text = ex.Message; }
    }
}
