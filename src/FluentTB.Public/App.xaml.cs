using System.Globalization;
using System.Windows;
using FluentTB.Integration;
using Forms = System.Windows.Forms;
namespace FluentTB.Public;
public partial class App : Application
{
    internal static PublicSettings Settings { get; } = PublicSettings.Load();
    private Mutex? singleton;
    private Forms.NotifyIcon? tray;
    private System.Drawing.Icon? trayImage;
    private KeyboardHook? keyboard;
    private SettingsWindow? settingsWindow;
    private LockWindow? flyout;
    private EventWaitHandle? openSettings;
    private RegisteredWaitHandle? openWait;
    internal static bool German => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName == "de";
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        // Shared with the private edition: never run two taskbar owners simultaneously.
        singleton = new Mutex(true, "FluentTB", out bool created);
        if (!created)
        {
            using var signal = new EventWaitHandle(false, EventResetMode.AutoReset, "FluentTB_OpenSettings");
            signal.Set(); Shutdown(); return;
        }
        ApplyLanguage();
        TaskbarIntegration.Start();
        tray = new Forms.NotifyIcon { Text = "FluentTB" };
        UpdateTrayIcon();
        tray.Visible = true;
        Microsoft.Win32.SystemEvents.UserPreferenceChanged += ThemeChanged;
        tray.ContextMenuStrip = new Forms.ContextMenuStrip();
        tray.ContextMenuStrip.Items.Add(German ? "Einstellungen" : "Settings", null, (_, _) => OpenSettings());
        tray.ContextMenuStrip.Items.Add(German ? "Beenden" : "Exit", null, (_, _) => Shutdown());
        tray.DoubleClick += (_, _) => OpenSettings();
        keyboard = new KeyboardHook((key, active) => { flyout ??= new LockWindow(); flyout.ShowStatus(key, active); });
        openSettings = new EventWaitHandle(false, EventResetMode.AutoReset, "FluentTB_OpenSettings");
        openWait = ThreadPool.RegisterWaitForSingleObject(openSettings, (_, _) => Dispatcher.BeginInvoke(OpenSettings), null, Timeout.Infinite, false);
        // Start silently; tray actions or the explicit settings signal open the window.
    }
    internal static void ApplyLanguage()
    {
        var culture = Settings.Language == "system" ? CultureInfo.InstalledUICulture : CultureInfo.GetCultureInfo(Settings.Language);
        CultureInfo.CurrentUICulture = culture;
        var code = culture.Name;
        var dictionaries = Current.Resources.MergedDictionaries;
        while (dictionaries.Count > 3) dictionaries.RemoveAt(3);
        Current.Resources["FtbOtherPages"] = "";
        foreach (var candidate in new[] { code, culture.TwoLetterISOLanguageName }.Distinct())
        {
            if (candidate == "en-US") break;
            try { dictionaries.Add(new ResourceDictionary { Source = new Uri($"Resources/Localization/Dictionary-{candidate}.xaml", UriKind.Relative) }); break; }
            catch (IOException) { }
        }
    }
    internal void OpenSettings()
    {
        settingsWindow ??= new SettingsWindow();
        settingsWindow.Show(); settingsWindow.WindowState = WindowState.Normal; settingsWindow.Activate();
    }
    private void ThemeChanged(object sender, Microsoft.Win32.UserPreferenceChangedEventArgs e) => Dispatcher.BeginInvoke(UpdateTrayIcon);
    private void UpdateTrayIcon()
    {
        if (tray == null) return;
        using var personalization = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
        bool light = personalization?.GetValue("SystemUsesLightTheme") is int value && value == 1;
        string asset = light ? "TrayLight" : "TrayDark";
        using var stream = GetResourceStream(new Uri($"pack://application:,,,/FluentTB.Taskbar;component/res/{asset}.ico")).Stream;
        using var source = new System.Drawing.Icon(stream);
        var previous = trayImage;
        trayImage = (System.Drawing.Icon)source.Clone();
        tray.Icon = trayImage;
        previous?.Dispose();
    }
    protected override void OnExit(ExitEventArgs e)
    {
        openWait?.Unregister(null); openSettings?.Dispose();
        Microsoft.Win32.SystemEvents.UserPreferenceChanged -= ThemeChanged;
        keyboard?.Dispose(); flyout?.Close(); tray?.Dispose(); tray = null; trayImage?.Dispose();
        TaskbarIntegration.Stop(); singleton?.Dispose(); base.OnExit(e);
    }
}
