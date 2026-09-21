using System.Globalization;
using System.Windows;
using System.Windows.Controls;
namespace FluentTB.Public;
public sealed class PreferencesPage : Page
{
    public PreferencesPage()
    {
        var panel = new StackPanel { Margin = new Thickness(16) };
        var heading = new TextBlock { FontSize = 26, Margin = new Thickness(0, 0, 0, 24) }; heading.SetResourceReference(TextBlock.TextProperty, "SystemSettingsTitle"); panel.Children.Add(heading);
        var picker = new ComboBox { MaxWidth = 380, HorizontalAlignment = HorizontalAlignment.Left };
        var codes = new[] { "system", "en-US", "ar", "ca", "zh-CN", "zh-TW", "hr", "cs", "nl", "fi", "fr", "de", "he", "hi", "hu", "id", "it", "ja", "ko", "pl", "pt-BR", "ru", "sk", "es", "ta", "th", "tr", "uk", "vi" };
        foreach (var code in codes) picker.Items.Add(new ComboBoxItem { Content = code == "system" ? "System" : CultureInfo.GetCultureInfo(code).NativeName, Tag = code });
        picker.SelectedIndex = Math.Max(0, Array.IndexOf(codes, App.Settings.Language));
        var status = new TextBlock { TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 16, 0, 0) };
        picker.SelectionChanged += (_, _) =>
        {
            App.Settings.Language = (string)((ComboBoxItem)picker.SelectedItem).Tag;
            try { App.Settings.Save(); App.ApplyLanguage(); status.Text = App.German ? "Sprache gespeichert. Für alle Texte FluentTB neu starten." : "Language saved. Restart FluentTB to update all text."; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { status.Text = ex.Message; }
        };
        panel.Children.Add(picker); panel.Children.Add(status); Content = panel;
    }
}
public sealed class AboutPage : Page
{
    public AboutPage()
    {
        var panel = new StackPanel { Margin = new Thickness(16) };
        panel.Children.Add(new TextBlock { Text = "FluentTB", FontSize = 30, FontWeight = FontWeights.SemiBold });
        panel.Children.Add(new TextBlock { Text = "Shinob1Kai · v" + Release.AppRelease.Version, Margin = new Thickness(0, 12, 0, 24) });
        panel.Children.Add(new TextBlock { Text = App.German ? "Entwicklung von FluentTB: Shinob1Kai" : "FluentTB development: Shinob1Kai", FontSize = 18 });
        panel.Children.Add(new TextBlock { Text = "Third-party components\n\nRoundedTB · torchgm · MIT\nFluentFlyout lock-key visuals and dispatch · Hugo Li / The FluentFlyout Authors · GPL-3.0-or-later\n\nLICENSE, licenses/ and THIRD_PARTY_NOTICES.md are included with this application.", TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 24, 0, 0), Opacity = .7 });
        Content = panel;
    }
}
