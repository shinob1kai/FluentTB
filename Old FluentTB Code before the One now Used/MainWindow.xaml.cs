using Microsoft.Win32;
using iNKORE.UI.WPF.Modern;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace FluentTB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    ///
    /// FluentTB — a fork/revival of RoundedTB 3.1
    /// with critical runtime bugfixes applied (see TaskbarManager.cs).
    /// </summary>
    public partial class MainWindow : Window
    {
        // -------------------------------------------------------------------------
        // Public state (accessed by BackgroundEngine)
        // -------------------------------------------------------------------------

        public bool             IsWindows11    { get; private set; }
        public Types.Settings   ActiveSettings  { get; set; }  = new();
        public Interaction      Interaction     { get; private set; } = null!;

        // TaskbarDetails is written from the background thread and read from the UI thread.
        // All accesses go through this lock to prevent torn reads/writes.
        private readonly object _taskbarLock = new();
        private List<Types.Taskbar> _taskbarDetails = new();
        public List<Types.Taskbar> TaskbarDetails
        {
            get { lock (_taskbarLock) return _taskbarDetails; }
            set { lock (_taskbarLock) _taskbarDetails = value; }
        }

        public string ConfigPath { get; private set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ftb.json");
        public string LogPath { get; private set; } =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ftb.log");

        // -------------------------------------------------------------------------
        // Private state
        // -------------------------------------------------------------------------

        private bool              _shouldReallyDie;
        private bool              _isAlreadyRunning;
        private BackgroundEngine  _bgEngine       = null!;
        private HwndSource?       _hwndSource;
        private AutoHideManager   _autoHideMgr    = new();

        // The background loop runs on a plain Task/Thread, not a BackgroundWorker,
        // so it never touches the UI thread and there is no need for DoEvents().
        private System.Threading.CancellationTokenSource _bgCts = new();

        private const int AppVersion = 20263; // 2026.3.0.0

        // -------------------------------------------------------------------------
        // Constructor
        // -------------------------------------------------------------------------

        public MainWindow()
        {
            InitializeComponent();

            // Detect Windows 11
            IsWindows11 = DetectWindows11();

            // Create helper instances (they reference Application.Current.MainWindow)
            _bgEngine   = new BackgroundEngine();
            Interaction = new Interaction();

            // Check for duplicate instance
            Process[] peers = Process.GetProcessesByName(
                Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]));
            if (peers.Length > 1)
            {
                foreach (IntPtr hwnd in Interaction.GetTopLevelWindows())
                {
                    var cls   = new StringBuilder(1024);
                    var title = new StringBuilder(1024);
                    try
                    {
                        NativeMethods.GetClassName(hwnd, cls, 1024);
                        NativeMethods.GetWindowText(hwnd, title, 1024);
                        if (cls.ToString().Contains("HwndWrapper[FluentTB.exe")
                            && title.ToString() == "FluentTB")
                        {
                            NativeMethods.SetWindowText(hwnd, "FluentTB_SettingsRequest");
                        }
                    }
                    catch { }
                }
                _shouldReallyDie = true;
                _isAlreadyRunning = true;
                Close();
                return;
            }

            TrayIconCheck();

            // Startup shortcut check
            string startupLink = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup), "FluentTB.lnk");
            if (System.IO.File.Exists(startupLink))
            {
                StartupCheckBox.IsChecked = true;
                ShowMenuItem.Header = "Show FluentTB";
            }

            // (BackgroundWorker entfernt — Loop läuft via Task.Run in BackgroundEngine)
            Interaction.FileSystem();
            Interaction.AddLog("FluentTB started.");
            ActiveSettings = Interaction.ReadJSON();

            // Sync IsWindows11 flag
            ActiveSettings.IsWindows11 = IsWindows11;

            if (!IsWindows11)
            {
                ActiveSettings.IsCentred     = false;
                dynamicCheckBox.Content      = "Split mode";
                fillAltTabCheckBox.Content   = "[Unavailable]";
                fillAltTabCheckBox.IsEnabled = false;
                fillAltTabCheckBox.Visibility = Visibility.Collapsed;
            }

            if (ActiveSettings.Version != AppVersion && ActiveSettings.Version != -1)
                ActiveSettings.IsNotFirstLaunch = false;
            ActiveSettings.Version = AppVersion;

            Interaction.AddLog(JsonConvert.SerializeObject(ActiveSettings, Formatting.Indented));

            // Apply the correct Dark/Light theme before the first render
            ApplyWindowTheme();

            // Populate UI from settings
            PopulateUI();

            // Everything that touches Win32 taskbar handles is deferred to the Loaded
            // event so WPF gets one clean render pass first. This is what prevents the
            // white/frozen window — the constructor returns and WPF renders the frame,
            // then OnLoaded runs on the still-UI thread but after painting is done.
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded; // one-shot

            // Read centred state from registry
            bool isCentred = TaskbarManager.CheckIfCentred();
            Interaction.AddLog($"Taskbar centred: {isCentred}");

            // Build taskbar info and apply initial geometry
            TaskbarDetails = TaskbarManager.GenerateTaskbarInfo();
            if (!string.IsNullOrEmpty(marginInput.Text) && !string.IsNullOrEmpty(cornerRadiusInput.Text))
                ApplyButton_Click(null, null);

            if (!ActiveSettings.FillOnMaximise)
            {
                ActiveSettings.FillOnTaskSwitch = false;
                fillAltTabCheckBox.IsEnabled    = false;
            }

            // Show split mode help button for Win10
            splitHelpButton.Visibility = (!IsWindows11 && ActiveSettings.IsDynamic)
                ? Visibility.Visible : Visibility.Hidden;

            // First launch: show About
            if (!ActiveSettings.IsNotFirstLaunch)
            {
                ActiveSettings.IsNotFirstLaunch = true;
                var aw = new AboutWindow();
                aw.expander0.IsExpanded = true;
                aw.ShowDialog();
                try { Visibility = Visibility.Visible; } catch { }
                ShowMenuItem.Header = "Hide FluentTB";
            }
        }

        // -------------------------------------------------------------------------
        // Tray icon
        // -------------------------------------------------------------------------

        public void TrayIconCheck()
        {
            // Dark mode  → WHITE icon (visible on dark taskbar)
            // Light mode → DARK/BLACK icon (visible on light taskbar)
            bool isLight = ReadSystemUsesLightTheme();
            Uri  uri     = isLight
                ? new("pack://application:,,,/res/TrayDark.ico")   // dark/black icon on light bg
                : new("pack://application:,,,/res/TrayLight.ico");  // white icon on dark bg

            // Fallback chain: themed → FluentTB.ico
            Uri fallback = new("pack://application:,,,/res/FluentTB.ico");
            var stream   = Application.GetResourceStream(uri)?.Stream
                        ?? Application.GetResourceStream(fallback)?.Stream;

            if (stream != null)
                TrayIcon.Icon = new System.Drawing.Icon(stream);

            // Also re-theme the context menu colours
            ApplyContextMenuTheme();
        }

        /// <summary>
        /// Applies Dark/Light colours directly to the tray ContextMenu's resource dictionary
        /// so the popup matches the current Windows theme.
        /// </summary>
        private void ApplyContextMenuTheme()
        {
            bool isLight = ReadSystemUsesLightTheme();

            var bg     = new SolidColorBrush(isLight ? Color.FromRgb(0xF3, 0xF3, 0xF3) : Color.FromRgb(0x20, 0x20, 0x20));
            var fg     = new SolidColorBrush(isLight ? Color.FromRgb(0x1A, 0x1A, 0x1A) : Color.FromRgb(0xFF, 0xFF, 0xFF));
            var border = new SolidColorBrush(isLight ? Color.FromRgb(0xCC, 0xCC, 0xCC) : Color.FromRgb(0x3A, 0x3A, 0x3A));
            var ctrl   = new SolidColorBrush(isLight ? Color.FromRgb(0xFF, 0xFF, 0xFF) : Color.FromRgb(0x2D, 0x2D, 0x2D));

            var menu = TrayIcon.ContextMenu;
            if (menu == null) return;

            // The ContextMenu lives in a separate Popup window and does NOT inherit
            // Window.Resources. We push the palette into its own ResourceDictionary
            // AND set the dependency properties directly so all bindings resolve.
            menu.Resources["FtbWindowBackground"]  = bg;
            menu.Resources["FtbWindowForeground"]  = fg;
            menu.Resources["FtbBorderBrush"]       = border;
            menu.Resources["FtbControlBackground"] = ctrl;

            menu.Background  = bg;
            menu.Foreground  = fg;
            menu.BorderBrush = border;

            // Walk all items and set Foreground explicitly — DynamicResource doesn't
            // cross Popup boundaries, so we must push the colour manually each time.
            foreach (var item in menu.Items)
            {
                if (item is System.Windows.Controls.MenuItem mi)
                {
                    mi.Foreground = fg;
                    mi.Background = bg;
                }
                else if (item is System.Windows.Controls.CheckBox cb)
                {
                    cb.Foreground = fg;
                }
                else if (item is System.Windows.Controls.Border wrapperBorder)
                {
                    // StartupCheckBox is wrapped in a Border for padding alignment
                    if (wrapperBorder.Child is System.Windows.Controls.CheckBox cbInner)
                        cbInner.Foreground = fg;
                }
            }
        }

        // -------------------------------------------------------------------------
        // Window theme (Dark / Light from Windows registry)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Tells DWM to render the title bar in dark or light mode.
        /// Must be called after the HWND exists (OnSourceInitialized or later).
        /// </summary>
        public static void ApplyDwmDarkMode(IntPtr hwnd)
        {
            bool isDark = !ReadSystemUsesLightTheme();
            int  value  = isDark ? 1 : 0;
            NativeMethods.DwmSetWindowAttribute(
                hwnd,
                NativeMethods.DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref value,
                sizeof(int));
        }

        /// <summary>
        /// Reads HKCU\...\Themes\Personalize\AppsUseLightTheme.
        /// Returns true = Light, false = Dark.  Defaults to Light on any error.
        /// </summary>
        public static bool ReadSystemUsesLightTheme()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key?.GetValue("AppsUseLightTheme") is int val)
                    return val != 0; // 0 = Dark, 1 = Light
            }
            catch { }
            return true; // safe default
        }

        /// <summary>
        /// Applies Dark or Light theme brushes to the window's own resource dictionary
        /// so that Background, Foreground, TextBox fill and CheckBox text all update.
        /// Called once on Loaded and again whenever Windows theme changes.
        /// </summary>
        public void ApplyWindowTheme()
        {
            bool isLight = ReadSystemUsesLightTheme();

            // --- Palette ---
            // Dark:  anthracite window, slightly lighter controls, light borders, white text
            // Light: near-white window, light grey controls, medium border, near-black text
            Color winBg   = isLight ? Color.FromRgb(0xF3, 0xF3, 0xF3)   // #F3F3F3
                                    : Color.FromRgb(0x20, 0x20, 0x20);   // #202020
            Color winFg   = isLight ? Color.FromRgb(0x1A, 0x1A, 0x1A)   // #1A1A1A
                                    : Color.FromRgb(0xFF, 0xFF, 0xFF);   // #FFFFFF
            Color ctrlBg  = isLight ? Color.FromRgb(0xFF, 0xFF, 0xFF)   // #FFFFFF
                                    : Color.FromRgb(0x2D, 0x2D, 0x2D);   // #2D2D2D
            Color borderC = isLight ? Color.FromRgb(0xCC, 0xCC, 0xCC)   // #CCCCCC
                                    : Color.FromRgb(0x3A, 0x3A, 0x3A);   // #3A3A3A

            // Update the brushes in our local resource dictionary.
            // DynamicResource bindings in XAML react to these changes automatically.
            Resources["FtbWindowBackground"] = new SolidColorBrush(winBg);
            Resources["FtbWindowForeground"] = new SolidColorBrush(winFg);
            Resources["FtbControlBackground"] = new SolidColorBrush(ctrlBg);
            Resources["FtbBorderBrush"]       = new SolidColorBrush(borderC);

            // Set Window.Background directly so the chrome area also updates.
            Background = new SolidColorBrush(winBg);

            // NOTE: We deliberately do NOT call ThemeManager.Current.ApplicationTheme here.
            // Changing that property causes the iNKORE library to reload its ResourceDictionaries
            // app-wide, which overwrites every implicit Style (Button, CheckBox, TextBox) and
            // destroys the ControlTemplates defined in MainWindow.xaml — breaking the layout.
            // All colours are fully handled by the four FtbXxx DynamicResource brushes above.

            Interaction?.AddLog($"Theme applied: {(isLight ? "Light" : "Dark")}");
        }

        // -------------------------------------------------------------------------
        // Apply
        // -------------------------------------------------------------------------

        public void ApplyButton_Click(object? sender, RoutedEventArgs? e)
        {
            if (!int.TryParse(cornerRadiusInput.Text, out int roundFactor))
                return;
            if (ActiveSettings.MarginBasic != -384
                && !int.TryParse(marginInput.Text, out _))
                return;

            // Read margins
            int mt = 0, ml = 0, mb = 0, mr = 0;

            if (marginInput.IsEnabled)
            {
                if (!int.TryParse(marginInput.Text, out int mBasic)) return;
                mt = ml = mb = mr = mBasic;
                ActiveSettings.MarginBasic = mBasic;
            }
            else
            {
                if (!int.TryParse(mTopInput.Text,    out mt)) return;
                if (!int.TryParse(mLeftInput.Text,   out ml)) return;
                if (!int.TryParse(mBottomInput.Text, out mb)) return;
                if (!int.TryParse(mRightInput.Text,  out mr)) return;
                ActiveSettings.MarginBasic = -384;
            }

            ActiveSettings.CornerRadius     = roundFactor;
            ActiveSettings.MarginTop        = mt;
            ActiveSettings.MarginLeft       = ml;
            ActiveSettings.MarginBottom     = mb;
            ActiveSettings.MarginRight      = mr;
            ActiveSettings.IsDynamic        = dynamicCheckBox.IsChecked        == true;
            ActiveSettings.IsCentred        = TaskbarManager.CheckIfCentred();
            ActiveSettings.ShowTray         = showTrayCheckBox.IsChecked       == true;
            ActiveSettings.CompositionCompat = compositionFixCheckBox.IsChecked == true;
            ActiveSettings.FillOnMaximise   = fillMaximisedCheckBox.IsChecked  == true;
            ActiveSettings.FillOnTaskSwitch = fillAltTabCheckBox.IsChecked     == true;
            ActiveSettings.ShowTrayOnHover  = showTrayOnHoverCheckBox.IsChecked == true;

            try
            {
                foreach (var tb in TaskbarDetails)
                {
                    int gap = tb.TrayRect.Left - tb.AppListRect.Right;
                    bool isFull = !ActiveSettings.IsDynamic
                        || (gap <= tb.ScaleFactor * 25 && gap > 0 && tb.TrayRect.Left != 0);

                    if (isFull)
                        TaskbarManager.UpdateSimpleTaskbar(tb, ActiveSettings);
                    else
                        TaskbarManager.UpdateDynamicTaskbar(tb, ActiveSettings);
                }
            }
            catch (InvalidOperationException ex)
            {
                Interaction.AddLog(ex.Message);
            }

            // Start the background monitor loop (off the UI thread)
            RestartLoop();

            Interaction.WriteJSON();
            TrayIconCheck();
        }

        /// <summary>
        /// Cancels any running background loop and starts a fresh one via Task.Run.
        /// Completely off the UI thread — no DoEvents, no blocking.
        /// </summary>
        private void RestartLoop()
        {
            _bgCts.Cancel();
            _bgCts.Dispose();
            _bgCts = new System.Threading.CancellationTokenSource();
            Task.Run(() => _bgEngine.RunLoop(_bgCts.Token));
        }

        // -------------------------------------------------------------------------
        // Closing
        // -------------------------------------------------------------------------

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!_shouldReallyDie)
            {
                e.Cancel = true;
                Visibility = Visibility.Hidden;
                ShowMenuItem.Header = "Show FluentTB";
                return;
            }

            // Graceful exit: cancel the background loop and wait briefly for it to stop
            _bgCts.Cancel();
            System.Threading.Thread.Sleep(200); // give the 100ms loop one clean exit tick

            // Stop auto-hide monitor (restores taskbar visibility)
            _autoHideMgr.Stop();

            // Stop listening for theme changes
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;

            // Pillar B-3: reset all taskbars on exit
            try
            {
                foreach (var tb in TaskbarDetails)
                    TaskbarManager.ResetTaskbar(tb, ActiveSettings);
            }
            catch (InvalidOperationException ex)
            {
                Interaction.AddLog($"Reset on exit error: {ex.Message}");
            }

            Interaction.AddLog("FluentTB exiting.");
            if (!_isAlreadyRunning)
                Interaction.WriteJSON();
        }

        // -------------------------------------------------------------------------
        // Tray menu handlers
        // -------------------------------------------------------------------------

        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Close all child windows first
            for (int i = Application.Current.Windows.Count - 1; i >= 0; i--)
                Application.Current.Windows[i].Close();

            _shouldReallyDie = true;
            Close();
        }

        public void ShowMenuItem_Click(object? sender, RoutedEventArgs? e)
        {
            if (!IsVisible)
            {
                Visibility = Visibility.Visible;
                ShowMenuItem.Header = "Hide FluentTB";
            }
            else
            {
                for (int i = Application.Current.Windows.Count - 1; i >= 0; i--)
                    Application.Current.Windows[i].Close();
                Visibility = Visibility.Hidden;
                ShowMenuItem.Header = "Show FluentTB";
            }
        }

        private void ContextMenu_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Reserved for future UWP startup init; noop in Win32 mode.
        }

        // ── Custom title bar close button ──────────────────────────────────────
        private void TitleBar_Close_Click(object sender, RoutedEventArgs e)
        {
            // Same behaviour as the system close button — hide, not kill
            Visibility = Visibility.Hidden;
            ShowMenuItem.Header = "Show FluentTB";
        }

        // -------------------------------------------------------------------------
        // Startup
        // -------------------------------------------------------------------------

        private void Startup_Clicked(object sender, RoutedEventArgs e)
        {
            string startupLink = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.Startup), "FluentTB.lnk");

            if (System.IO.File.Exists(startupLink))
                System.IO.File.Delete(startupLink);
            else
                EnableStartup();
        }

        private void EnableStartup()
        {
            try
            {
                string shortcutFolder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (!Directory.Exists(shortcutFolder))
                    Directory.CreateDirectory(shortcutFolder);

                // Use WScript.Shell via late-binding COM so we don't need a .NET Framework
                // COMReference (which is unsupported in .NET 8 SDK builds).
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return;
                dynamic shell    = Activator.CreateInstance(shellType)!;
                string  linkPath = Path.Combine(shortcutFolder, "FluentTB.lnk");
                dynamic shortcut = shell.CreateShortcut(linkPath);
                shortcut.TargetPath   = Environment.GetCommandLineArgs()[0];
                shortcut.IconLocation = Environment.GetCommandLineArgs()[0];
                shortcut.Arguments    = "";
                shortcut.Description  = "Start FluentTB";
                shortcut.Save();
            }
            catch (Exception ex)
            {
                Interaction.AddLog($"EnableStartup failed: {ex.Message}");
            }
        }

        // -------------------------------------------------------------------------
        // Slider / input event handlers
        // -------------------------------------------------------------------------

        private void marginSlider_ValueChanged(object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (marginInput != null)
                marginInput.Text = Math.Round(marginSlider.Value).ToString();
        }

        private void marginSlider_DragCompleted(object sender,
            System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            // Intentionally a no-op — Apply is the only way to commit changes.
        }

        private void cornerRadiusSlider_ValueChanged(object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (cornerRadiusInput != null)
                cornerRadiusInput.Text = Math.Round(cornerRadiusSlider.Value).ToString();
        }

        private void cornerRadiusSlider_DragCompleted(object sender,
            System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            // Intentionally a no-op — Apply is the only way to commit changes.
        }

        // -------------------------------------------------------------------------
        // Advanced panel toggle
        // -------------------------------------------------------------------------

        private void advancedButton_Click(object sender, RoutedEventArgs e)
        {
            if (AdvancedGrid.Visibility != Visibility.Visible)
            {
                AdvancedGrid.Visibility          = Visibility.Visible;
                advancedMarginsButton.Visibility = Visibility.Visible;
                Width = 445;
            }
            else
            {
                AdvancedGrid.Visibility          = Visibility.Collapsed;
                advancedMarginsButton.Visibility = Visibility.Hidden;
                Width = 169;
            }
        }

        private void advancedMarginsButton_Click(object sender, RoutedEventArgs e)
        {
            if (marginInput.IsEnabled)
            {
                marginInput.Text           = "Advanced";
                ActiveSettings.MarginBasic = -384;
                marginSlider.Value         = 0;
                marginSlider.IsEnabled     = false;
                marginInput.IsEnabled      = false;
                mTopInput.IsEnabled = mLeftInput.IsEnabled =
                    mBottomInput.IsEnabled = mRightInput.IsEnabled = true;
            }
            else
            {
                marginInput.Text           = "0";
                ActiveSettings.MarginBasic = 0;
                marginSlider.IsEnabled     = true;
                marginInput.IsEnabled      = true;
                mTopInput.IsEnabled = mLeftInput.IsEnabled =
                    mBottomInput.IsEnabled = mRightInput.IsEnabled = false;
            }
        }

        // -------------------------------------------------------------------------
        // Dynamic / split mode checkbox
        // -------------------------------------------------------------------------

        private void dynamicCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            showTrayOnHoverCheckBox.IsEnabled = true;
            showTrayOnHoverCheckBox.IsChecked = false;
            showTrayCheckBox.IsEnabled        = true;
            showTrayCheckBox.IsChecked        = true;
            mLeftLabel.Content  = "Outer Margin";
            mRightLabel.Content = "Inner Margin";

            if (!IsWindows11)
            {
                splitHelpButton.Visibility = Visibility.Visible;
                if (Opacity > 0.5)
                    splitHelpButton_Click(null!, null!);
            }
        }

        private void dynamicCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            mLeftLabel.Content  = "Left Margin";
            mRightLabel.Content = "Right Margin";
            showTrayOnHoverCheckBox.IsEnabled = false;
            showTrayOnHoverCheckBox.IsChecked = false;
            showTrayCheckBox.IsEnabled        = false;
            showTrayCheckBox.IsChecked        = false;
            if (!IsWindows11)
                splitHelpButton.Visibility = Visibility.Hidden;
        }

        private void splitHelpButton_Click(object sender, RoutedEventArgs e)
        {
            var ib = new Infobox
            {
                Title = "FluentTB — Split mode",
                Height = 480,
            };
            ib.titleBlock.Text = "How to use Split Mode";
            ib.bodyBlock.Text  =
                "Split mode has a couple of limitations and requires a small amount of setup.\n\n" +
                "Limitations:\n" +
                "1) Split mode doesn't resize automatically.\n" +
                "2) Toolbars are not compatible with split mode (disable all but one).\n" +
                "3) Split mode only works on horizontal taskbars.\n\n" +
                "Setup:\n" +
                "1) Right-click the taskbar and disable \"Lock the taskbar\".\n" +
                "2) Right-click again and turn off any existing toolbars.\n" +
                "3) Right-click a third time, select Toolbars > Desktop.\n" +
                "4) Use the || handle to resize the taskbar as you please.";
            ib.ShowDialog();
        }

        // -------------------------------------------------------------------------
        // Tray / hover checkboxes
        // -------------------------------------------------------------------------

        private void showTrayOnHoverCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            showTrayCheckBox.IsEnabled = false;
            showTrayCheckBox.IsChecked = false;
        }

        private void showTrayOnHoverCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            showTrayCheckBox.IsEnabled = true;
            showTrayCheckBox.IsChecked = true;
        }

        // -------------------------------------------------------------------------
        // Fill on maximise checkboxes
        // -------------------------------------------------------------------------

        private void fillMaximisedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (IsWindows11)
                fillAltTabCheckBox.IsEnabled = true;
        }

        private void fillMaximisedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            fillAltTabCheckBox.IsEnabled = false;
            fillAltTabCheckBox.IsChecked = false;
        }

        // -------------------------------------------------------------------------
        // TranslucentTB compat info dialog
        // -------------------------------------------------------------------------

        private void compositionFixCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (Opacity <= 0.01) return;

            var ib = new Infobox { Height = 450 };
            ib.Title = "FluentTB — TranslucentTB compatibility";
            ib.titleBlock.Text = "Compatibility with TranslucentTB";
            ib.bodyBlock.Text =
                "\nTranslucentTB is a utility that lets you customise the taskbar's opacity, " +
                "blur and colour. Enable this option to let FluentTB and TranslucentTB coexist.\n\n" +
                "After every geometry update, FluentTB sends TTB_ForceRefreshTaskbar to " +
                "TranslucentTB's worker window so its visual effects are re-applied correctly.\n\n" +
                "You may see brief flickering when the taskbar region changes. This is a " +
                "Windows limitation, not a bug in either application.";
            ib.ShowDialog();
        }

        // -------------------------------------------------------------------------
        // About
        // -------------------------------------------------------------------------

        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            var aw = new AboutWindow();
            aw.ShowDialog();
        }

        // -------------------------------------------------------------------------
        // OnSourceInitialized — register hotkey + WndProc hook
        // -------------------------------------------------------------------------

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr handle = new WindowInteropHelper(this).Handle;
            _hwndSource = HwndSource.FromHwnd(handle);
            _hwndSource?.AddHook(Interaction.HwndHook);

            // Win+F2 (modifier=0x8, vk=0x71)
            bool registered = NativeMethods.RegisterHotKey(handle, 9000, 0x8, 0x71);
            Debug.WriteLine($"Hotkey registered: {registered}");

            // Apply dark/light title bar via DWM
            ApplyDwmDarkMode(handle);

            // Listen for Windows theme changes so the window recolours live
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;

            Visibility = Visibility.Hidden;
            Opacity    = 1;
        }

        /// <summary>
        /// Fired by Windows when the user changes a system preference — including
        /// switching between Dark and Light mode. Marshals back to the UI thread
        /// so WPF resource mutations are safe.
        /// </summary>
        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            // Category.General covers colour/theme changes; ignore all others.
            if (e.Category != UserPreferenceCategory.General) return;

            // UserPreferenceChanged fires on a background thread — dispatch to UI.
            Dispatcher.BeginInvoke(() =>
            {
                ApplyWindowTheme();
                ApplyDwmDarkMode(new WindowInteropHelper(this).Handle);
                TrayIconCheck(); // keep tray icon in sync with new theme
            });
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static bool DetectWindows11()
        {
            try
            {
                using RegistryKey? key = Registry.LocalMachine.OpenSubKey(
                    @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
                if (key?.GetValue("CurrentBuild") is string build
                    && int.TryParse(build, out int buildNum))
                    return buildNum >= 21996;
            }
            catch { }
            return Environment.OSVersion.Version.Build >= 21996;
        }

        private void PopulateUI()
        {
            if (ActiveSettings.MarginBasic == -384)
            {
                marginInput.Text           = "Advanced";
                marginSlider.IsEnabled     = false;
                marginInput.IsEnabled      = false;
                mTopInput.IsEnabled = mLeftInput.IsEnabled =
                    mBottomInput.IsEnabled = mRightInput.IsEnabled = true;
                mTopInput.Text    = ActiveSettings.MarginTop.ToString();
                mLeftInput.Text   = ActiveSettings.MarginLeft.ToString();
                mBottomInput.Text = ActiveSettings.MarginBottom.ToString();
                mRightInput.Text  = ActiveSettings.MarginRight.ToString();
            }
            else
            {
                marginInput.Text       = ActiveSettings.MarginBasic.ToString();
                marginSlider.IsEnabled = true;
                marginInput.IsEnabled  = true;
                mTopInput.IsEnabled = mLeftInput.IsEnabled =
                    mBottomInput.IsEnabled = mRightInput.IsEnabled = false;
            }

            cornerRadiusInput.Text = ActiveSettings.CornerRadius.ToString();

            dynamicCheckBox.IsChecked         = ActiveSettings.IsDynamic;
            showTrayCheckBox.IsChecked         = ActiveSettings.ShowTray;
            fillMaximisedCheckBox.IsChecked    = ActiveSettings.FillOnMaximise;
            fillAltTabCheckBox.IsChecked        = ActiveSettings.FillOnTaskSwitch;
            showTrayOnHoverCheckBox.IsChecked   = ActiveSettings.ShowTrayOnHover;
            compositionFixCheckBox.IsChecked    = ActiveSettings.CompositionCompat;

            if (!ActiveSettings.FillOnMaximise)
                fillAltTabCheckBox.IsEnabled = false;
        }

    }
}
