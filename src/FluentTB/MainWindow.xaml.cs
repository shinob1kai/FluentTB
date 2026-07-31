using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Reflection;
using ModernWpf;
using System.Windows.Threading;
using System.Windows.Interop;
using DesktopBridge;
using System.Threading.Tasks;
// using Windows.ApplicationModel; // UWP only - conditionally used via reflection
using System.Diagnostics;
using Microsoft.Win32;
using System.Text;
using System.Threading;
using System.Windows.Media;

namespace FluentTB
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml â€” FluentTB
    ///
    /// Based on RoundedTB by torchgm.
    /// Bugfixes applied:
    ///   â€¢ TTB compatibility: Unchecked handler + reliable mutex detection
    ///   â€¢ Dynamic mode: event-driven dirty flag instead of polling-only
    ///   â€¢ Fill on maximise: shell hook (WH_SHELL) replaces slow window enumeration
    ///   â€¢ ApplyButton: cancels background worker synchronously without DoEvents spin
    /// </summary>
    public partial class MainWindow : Window
    {
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Constants & version
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        /// <summary>
        /// App version token.
        /// 1 = FluentTB 1.0
        /// </summary>
        public const int AppVersion = 1;

        // Shell hook message IDs (registered at runtime)
        private int _shellHookMsg;


        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Public state
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public bool isWindows11;
        public List<Types.Taskbar> taskbarDetails = new List<Types.Taskbar>();
        public readonly object TaskbarStateLock = new object();
        public bool shouldReallyDieNoReally = false;

        // FluentTB config & log paths - stored in %LOCALAPPDATA%\FluentTB\
        public string configPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FluentTB",
            "fluent-tb.json");
        public string logPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "FluentTB",
            "fluent-tb.log");

        public Types.Settings activeSettings = new Types.Settings();

        /// <summary>
        /// Staging copy â€” alle UI-Ã„nderungen (Slider, Checkboxen) landen hier.
        /// Erst ApplyButton_Click Ã¼bertrÃ¤gt pendingSettings â†’ activeSettings und
        /// aktualisiert die Taskleiste. Wird beim Laden aus activeSettings befÃ¼llt.
        /// </summary>
        public Types.Settings pendingSettings = new Types.Settings();
        public BackgroundWorker taskbarThread = new BackgroundWorker();
        public IntPtr hwndDesktopButton = IntPtr.Zero;
        public bool isCentred = false;
        public bool isAlreadyRunning = false;
        public Background background;
        public Interaction interaction;
        private HwndSource _hwndSource;

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Dirty flag: set by the shell hook when a window maximise/restore
        //  event is detected. The background worker reads & clears this flag.
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public volatile bool maximisedStateDirty = false;

        // AutoHide manager (ported from old FluentTB code)
        private readonly AutoHideManager _autoHideMgr = new AutoHideManager();


        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Constructor
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public MainWindow()
        {
            InitializeComponent();

            // â”€â”€ OS detection â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            int buildNumber = Convert.ToInt32(registryKey?.GetValue("CurrentBuild")?.ToString() ?? "0");
            isWindows11 = buildNumber >= 22000;

            if (!isWindows11)
            {
                MessageBox.Show(
                    "FluentTB supports Windows 11 only.",
                    "FluentTB",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                shouldReallyDieNoReally = true;
                Application.Current.Shutdown();
                return;
            }

            // â”€â”€ Core helpers â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            background   = new Background();
            interaction  = new Interaction();

            // â”€â”€ Single-instance guard â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            Process[] matchingProcesses =
                Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName);
            if (matchingProcesses.Length > 1)
            {
                // Signal existing instance to show its settings window
                foreach (IntPtr hwnd in Interaction.GetTopLevelWindows())
                {
                    var cls   = new StringBuilder(1024);
                    var title = new StringBuilder(1024);
                    try
                    {
                        LocalPInvoke.GetClassName(hwnd, cls, 1024);
                        LocalPInvoke.GetWindowText(hwnd, title, 1024);
                        if (cls.ToString().Contains("HwndWrapper[FluentTB.exe") &&
                            title.ToString() == "FluentTB")
                        {
                            LocalPInvoke.SetWindowText(hwnd, "FluentTB_SettingsRequest");
                        }
                    }
                    catch { }
                }
                shouldReallyDieNoReally = true;
                isAlreadyRunning = true;
                Close();
                return;
            }

            TrayIconCheck();


            // â”€â”€ UWP packaging paths â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            if (IsRunningAsUWP())
            {
// #pragma warning disable CS4014
                // StartupInit(true);  // UWP only - disabled
#pragma warning restore CS4014
                configPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FluentTB",
                    "fluent-tb.json");
                logPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FluentTB",
                    "fluent-tb.log");
            }

            // â”€â”€ Startup shortcut check (non-UWP) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            if (!IsRunningAsUWP() &&
                System.IO.File.Exists(Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                    "FluentTB.lnk")))
            {
                StartupCheckBox.IsChecked = true;
            }

            // â”€â”€ Background worker setup â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            taskbarThread.WorkerSupportsCancellation = true;
            taskbarThread.WorkerReportsProgress = true;
            taskbarThread.DoWork += background.DoWork;
            taskbarThread.RunWorkerCompleted += TaskbarThread_RunWorkerCompleted;

            // â”€â”€ Load & apply settings â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            interaction.FileSystem();
            interaction.AddLog(IsRunningAsUWP()
                ? "FluentTB started in UWP mode!"
                : "FluentTB started!");

            activeSettings = interaction.ReadJSON();

            // Guard: ReadJSON returns null if the file is missing or corrupt
            if (activeSettings == null)
                activeSettings = BuildDefaultSettings();

            activeSettings.IsWindows11 = isWindows11;

            // Version migration: force first-launch screen on version bump
            if (activeSettings.Version != AppVersion && AppVersion != -1)
            {
                activeSettings.IsNotFirstLaunch = false;
            }
            activeSettings.Version = AppVersion;

            // Initialisiere pendingSettings als Arbeitskopie der geladenen Einstellungen
            pendingSettings = CopySettings(activeSettings);

            LogSettings();


            // â”€â”€ Populate UI from loaded settings â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            if (activeSettings.MarginBasic == -384)
            {
                // Advanced / independent margins mode
                marginInput.Text = "Advanced";
                marginSlider.IsEnabled = false;
                marginInput.IsEnabled = false;
                mTopInput.IsEnabled = mLeftInput.IsEnabled =
                    mBottomInput.IsEnabled = mRightInput.IsEnabled = true;
                mTopInput.Text    = activeSettings.MarginTop.ToString();
                mLeftInput.Text   = activeSettings.MarginLeft.ToString();
                mBottomInput.Text = activeSettings.MarginBottom.ToString();
                mRightInput.Text  = activeSettings.MarginRight.ToString();
            }
            else
            {
                marginInput.Text = activeSettings.MarginBasic.ToString();
                marginSlider.IsEnabled = marginInput.IsEnabled = true;
                mTopInput.IsEnabled = mLeftInput.IsEnabled =
                    mBottomInput.IsEnabled = mRightInput.IsEnabled = false;
            }

            // Detect taskbar alignment from registry
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    if (key != null)
                    {
                        isCentred = (int)(key.GetValue("TaskbarAl") ?? 0) == 1;
                        interaction.AddLog($"Taskbar centred: {isCentred}");
                    }
                }
            }
            catch (Exception ex) { interaction.AddLog(ex.Message); }

            // Bind checkboxes
            dynamicCheckBox.IsChecked        = activeSettings.IsDynamic;
            centredCheckBox.IsChecked        = activeSettings.IsCentred;
            showTrayCheckBox.IsChecked       = activeSettings.ShowTray;
            fillMaximisedCheckBox.IsChecked  = activeSettings.FillOnMaximise;
            fillAltTabCheckBox.IsChecked     = activeSettings.FillOnTaskSwitch;
            showTrayOnHoverCheckBox.IsChecked = activeSettings.ShowTrayOnHover;
            compositionFixCheckBox.IsChecked = activeSettings.CompositionCompat;
            // taskbarShadowCheckBox.IsChecked  = activeSettings.ShowTaskbarShadow;  // UI element not present
            cornerRadiusInput.Text           = activeSettings.CornerRadius.ToString();

            // Disable alt-tab checkbox when fill-on-maximise is off
            if (!activeSettings.FillOnMaximise)
            {
                activeSettings.FillOnTaskSwitch = false;
                fillAltTabCheckBox.IsEnabled = false;
            }

            // Initial taskbar scan & apply
            taskbarDetails = Taskbar.GenerateTaskbarInfo();
            if (marginInput.Text != null && cornerRadiusInput.Text != null)
            {
                ApplyButton_Click(null, null);
            }

            // First launch: show About window
            if (activeSettings.IsNotFirstLaunch != true)
            {
                activeSettings.IsNotFirstLaunch = true;
                var aw = new AboutWindow();
                aw.expander0.IsExpanded = true;
                aw.ShowDialog();
                try { Visibility = Visibility.Visible; }
                catch (InvalidOperationException) { }
                ShowMenuItem.Header = "Hide FluentTB";
            }
        }


        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Window source initialised: register hotkeys & shell hook
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr handle = new WindowInteropHelper(this).Handle;
            _hwndSource = HwndSource.FromHwnd(handle);
            _hwndSource.AddHook(WndProc);

            // Win+F2 â€” toggle system tray visibility
            LocalPInvoke.RegisterHotKey(handle, 9000, (int)Types.KeyModifier.WinKey, 0x71);

            // Register for shell notifications (maximise / restore events).
            _shellHookMsg = LocalPInvoke.RegisterWindowMessage("SHELLHOOK");
            LocalPInvoke.RegisterShellHookWindow(handle);

            // Subscribe to Windows theme changes (Light â†” Dark)
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            ApplyDwmDarkMode(handle);
            ApplyWindowTheme();

            Visibility = Visibility.Hidden;
            Opacity = 1;
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Window procedure: handles hotkeys, shell events, and the
        //  single-instance "show settings" message
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private IntPtr WndProc(IntPtr hwnd, int msg,
                               IntPtr wParam, IntPtr lParam,
                               ref bool handled)
        {
            const int WM_HOTKEY = 0x0312;

            // â”€â”€ Hotkeys â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            if (msg == WM_HOTKEY && wParam.ToInt32() == 9000)
            {
                int vkey = ((int)lParam >> 16) & 0xFFFF;
                if (vkey == 0x71) // F2
                {
                    showTrayCheckBox.IsChecked = !(showTrayCheckBox.IsChecked == true);
                    ApplyButton_Click(null, null);
                }
                handled = true;
            }

            // â”€â”€ Shell hook â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            // HSHELL_WINDOWCREATED/DESTROYED keep Dynamic mode in sync when
            // a non-pinned app appears or disappears from the taskbar.
            if (msg == _shellHookMsg)
            {
                int code = wParam.ToInt32() & 0x7FFF; // mask out HSHELL_HIGHBIT
                if (code == 1 || code == 2 || code == 3 || code == 4)
                {
                    lock (TaskbarStateLock)
                    {
                        maximisedStateDirty = true;
                    }
                }
            }

            // â”€â”€ Delegate the Interaction hotkey hook (TTB / misc) â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
            return interaction.HwndHook(hwnd, msg, wParam, lParam, ref handled);
        }


        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Tray icon â€” switch icon based on current app theme
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public TypedEventHandler<ThemeManager, object> TrayIconCheck()
        {
            Uri resLight = new Uri("pack://application:,,,/res/TrayLight.ico");
            Uri resDark  = new Uri("pack://application:,,,/res/TrayDark.ico");

            TrayIcon.Icon = ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Light
                ? new System.Drawing.Icon(Application.GetResourceStream(resLight).Stream)
                : new System.Drawing.Icon(Application.GetResourceStream(resDark).Stream);

            return null;
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Apply button â€” der EINZIGE Ort der activeSettings schreibt und die
        //  Taskleiste aktualisiert. Liest aus pendingSettings (Arbeitskopie),
        //  Ã¼bertrÃ¤gt sie nach activeSettings, dann ForceRedraw.
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public void ApplyButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryParseInputs(out int mt, out int ml, out int mb, out int mr, out int roundFactor))
                return;

            // 1. Arbeitskopie aus UI-Zustand zusammenbauen
            pendingSettings.CornerRadius      = roundFactor;
            pendingSettings.MarginTop         = mt;
            pendingSettings.MarginLeft        = ml;
            pendingSettings.MarginBottom      = mb;
            pendingSettings.MarginRight       = mr;
            // FIX Bug 3 (MarginBasic-Modus): Modus aus Datenmodell lesen, nicht aus
            // UI-Zustand. pendingSettings.MarginBasic wurde bereits in
            // advancedMarginsButton_Click auf -384 gesetzt (Advanced) bzw. auf den
            // numerischen Wert (Basic). marginInput.IsEnabled ist ein reines
            // UI-Feedback-Flag und kann beim App-Start noch nicht den richtigen Zustand
            // widerspiegeln. Das Datenmodell ist die einzige zuverlÃ¤ssige Quelle.
            if (pendingSettings.MarginBasic != -384)
                pendingSettings.MarginBasic = mt; // Basic-Modus: Basic = Top = alle Seiten
            // Im Advanced-Modus bleibt MarginBasic = -384 â€” keine Ã„nderung nÃ¶tig.
            pendingSettings.IsDynamic         = dynamicCheckBox.IsChecked == true;
            pendingSettings.IsCentred         = Taskbar.CheckIfCentred();
            pendingSettings.ShowTray          = showTrayCheckBox.IsChecked == true;
            pendingSettings.CompositionCompat = compositionFixCheckBox.IsChecked == true;
            pendingSettings.FillOnMaximise    = fillMaximisedCheckBox.IsChecked == true;
            pendingSettings.FillOnTaskSwitch  = fillAltTabCheckBox.IsChecked == true;
            pendingSettings.ShowTrayOnHover   = showTrayOnHoverCheckBox.IsChecked == true;
            // pendingSettings.ShowTaskbarShadow = taskbarShadowCheckBox.IsChecked == true;
            pendingSettings.IsWindows11       = isWindows11;
            lock (TaskbarStateLock)
            {
                pendingSettings.Version          = activeSettings.Version;
                pendingSettings.IsNotFirstLaunch = activeSettings.IsNotFirstLaunch;
            }

            // 2. Arbeitskopie â†’ activeSettings Ã¼bertragen (atomisch)
            Types.Settings appliedSettings;
            List<Types.Taskbar> snapshot;
            lock (TaskbarStateLock)
            {
                activeSettings = CopySettings(pendingSettings);
                appliedSettings = CopySettings(activeSettings);
                snapshot = taskbarDetails != null
                    ? new List<Types.Taskbar>(taskbarDetails)
                    : new List<Types.Taskbar>();
            }

            // 3. Taskleiste sofort aktualisieren (einzige Stelle die das tut)
            // forceAccentReset=true keeps the apply path explicit and separate
            // from the lightweight background updates.
            //
            // FIX Bug 3 (Thread-Safety): taskbarDetails ist ein Feld das der
            // BG-Worker atomar ersetzen kann (mw.taskbarDetails = taskbars).
            // Wir lesen die Referenz einmalig in eine lokale Variable â€” so arbeiten
            // wir auf demselben Listenobjekt auch wenn der Worker die Referenz
            // zwischenzeitlich tauscht. Eine weitere Absicherung mit Interlocked ist
            // nicht nÃ¶tig, da der Worker ausschlieÃŸlich die Referenz ersetzt
            // (keine In-Place-Mutation der Liste selbst nach dem Swap).
            try
            {
                foreach (Types.Taskbar taskbar in snapshot)
                {
                    int gap = taskbar.TrayRect.Left - taskbar.AppListRect.Right;
                    bool isFull = !appliedSettings.IsDynamic ||
                                  (gap <= taskbar.ScaleFactor * 25 && gap > 0 &&
                                   taskbar.TrayRect.Left != 0);
                    if (isFull)
                        Taskbar.UpdateSimpleTaskbar(taskbar, appliedSettings, forceAccentReset: true);
                    else
                        Taskbar.UpdateDynamicTaskbar(taskbar, appliedSettings, forceAccentReset: true);
                }

                SyncTaskbarShadows(snapshot, appliedSettings);
            }
            catch (InvalidOperationException ex)
            {
                interaction.AddLog(ex.Message);
            }

            // 4. Background worker neu starten mit den aktuellen Werten
            RestartBackgroundWorker(mt, ml, mb, mr, roundFactor);

            interaction.WriteJSON();
            TrayIconCheck();
        }


        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Helper: parse margin & corner-radius inputs
        //
        //  FIX Bug 4 â€” NullReferenceException beim App-Start:
        //  Der Modus (Basic vs. Advanced) wird aus dem Datenmodell gelesen
        //  (pendingSettings.MarginBasic == -384), nicht aus dem UI-Zustand
        //  (marginInput.IsEnabled). marginInput.IsEnabled ist beim initialen
        //  ApplyButton_Click im Konstruktor noch nicht zuverlÃ¤ssig gesetzt,
        //  da InitializeComponent() die Bindings noch nicht vollstÃ¤ndig
        //  aufgelÃ¶st hat. Das Datenmodell ist die einzige verlÃ¤ssliche Quelle.
        //
        //  ZusÃ¤tzlich: alle Text-Felder werden vor dem Parse auf null/leer
        //  geprÃ¼ft, da TextBox.Text beim App-Start noch null sein kann.
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private bool TryParseInputs(out int mt, out int ml,
                                    out int mb, out int mr,
                                    out int roundFactor)
        {
            mt = ml = mb = mr = roundFactor = 0;

            // cornerRadiusInput muss immer parseabel sein
            if (!int.TryParse(cornerRadiusInput?.Text, out roundFactor))
                return false;

            // Modus aus Datenmodell: -384 = Advanced (unabhÃ¤ngige RÃ¤nder)
            bool isAdvancedMode = pendingSettings.MarginBasic == -384;

            if (!isAdvancedMode)
            {
                // Basic mode: gleicher Rand auf allen Seiten
                // Fallback: wenn marginInput noch nicht bereit ist, 0 annehmen
                string basicText = marginInput?.Text;
                if (string.IsNullOrEmpty(basicText) || basicText == "Advanced")
                    basicText = "0";
                if (!int.TryParse(basicText, out int basic))
                    return false;
                mt = ml = mb = mr = basic;
            }
            else
            {
                // Advanced / independent margins
                // Fallback auf "0" wenn ein Feld noch nicht initialisiert ist
                string topText    = mTopInput?.Text    ?? "0";
                string leftText   = mLeftInput?.Text   ?? "0";
                string bottomText = mBottomInput?.Text ?? "0";
                string rightText  = mRightInput?.Text  ?? "0";

                if (!int.TryParse(topText,    out mt) ||
                    !int.TryParse(leftText,   out ml) ||
                    !int.TryParse(bottomText, out mb) ||
                    !int.TryParse(rightText,  out mr))
                    return false;
            }
            return true;
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Background-worker restart â€” never blocks the UI thread
        //
        //  If the worker is idle â†’ start immediately.
        //  If the worker is busy â†’ CancelAsync and queue the restart via
        //  RunWorkerCompleted, which fires on the UI thread once the worker
        //  actually finishes.  This avoids Thread.Sleep on the UI thread which
        //  would freeze WPF and cause RunWorkerAsync to throw
        //  InvalidOperationException when IsBusy is still true.
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private bool _workerRestartPending = false;
        private (int mt, int ml, int mb, int mr, int cr) _pendingArgs;

        private void RestartBackgroundWorker(int mt, int ml, int mb, int mr, int cr)
        {
            if (!taskbarThread.IsBusy)
            {
                taskbarThread.RunWorkerAsync((mt, ml, mb, mr, cr));
            }
            else
            {
                // Store the desired args and request cancellation.
                // RunWorkerCompleted will pick them up and restart.
                _pendingArgs          = (mt, ml, mb, mr, cr);
                _workerRestartPending = true;
                taskbarThread.CancelAsync();
            }
        }

        private void TaskbarThread_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (_workerRestartPending)
            {
                _workerRestartPending = false;
                var a = _pendingArgs;
                taskbarThread.RunWorkerAsync((a.mt, a.ml, a.mb, a.mr, a.cr));
            }
        }


        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Window lifecycle
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (!shouldReallyDieNoReally)
            {
                // Hide to tray instead of closing
                e.Cancel = true;
                Visibility = Visibility.Hidden;
                ShowMenuItem.Header = "Show FluentTB";
                return;
            }

            // Real shutdown
            try { taskbarThread.CancelAsync(); }
            catch (Exception ex) { interaction.AddLog(ex.Message); }

            // Don't spin-wait here â€” the worker will finish on its own.
            // We already called CancelAsync; give it one brief chance then continue.
            int waited = 0;
            while (taskbarThread.IsBusy && waited < 500)
            {
                System.Windows.Forms.Application.DoEvents();
                System.Threading.Thread.Sleep(25);
                waited += 25;
            }

            // Unregister shell hook & hotkey
            if (_hwndSource != null)
            {
                IntPtr handle = new WindowInteropHelper(this).Handle;
                LocalPInvoke.DeregisterShellHookWindow(handle);
                LocalPInvoke.UnregisterHotKey(handle, 9000);
            }

            // Stop auto-hide manager (restores taskbar visibility)
            _autoHideMgr.Stop();
            CloseTaskbarShadows();

            // Unsubscribe theme change events
            SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;

            interaction.AddLog("Exiting FluentTB.");

            if (!isAlreadyRunning)
                interaction.WriteJSON();
        }


        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Tray context-menu handlers
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void CloseMenuItem_Click(object sender, RoutedEventArgs e)
        {
            // Close all secondary windows first
            for (int i = App.Current.Windows.Count - 1; i >= 0; i--)
                App.Current.Windows[i].Close();

            shouldReallyDieNoReally = true;

            try
            {
                List<Types.Taskbar> snapshot;
                Types.Settings settings;
                lock (TaskbarStateLock)
                {
                    snapshot = new List<Types.Taskbar>(taskbarDetails);
                    settings = CopySettings(activeSettings);
                }

                foreach (var tb in snapshot)
                    Taskbar.ResetTaskbar(tb, settings);

                SyncTaskbarShadows(snapshot, new Types.Settings { ShowTaskbarShadow = false });
            }
            catch (InvalidOperationException ex)
            {
                interaction.AddLog($"Taskbar structure changed on exit: {ex.Message}");
            }

            Close();
        }

        public void ShowMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!IsVisible)
            {
                Visibility = Visibility.Visible;
                ShowMenuItem.Header = "Hide FluentTB";
            }
            else
            {
                for (int i = App.Current.Windows.Count - 1; i >= 0; i--)
                    App.Current.Windows[i].Close();
                Visibility = Visibility.Hidden;
                ShowMenuItem.Header = "Show FluentTB";
            }
        }

        private async void ContextMenu_MouseEnter(object sender,
            System.Windows.Input.MouseEventArgs e)
        {
            // UWP startup init disabled
            // if (IsRunningAsUWP())
            //     await StartupInit(false);
        }


        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Startup handling
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private async void Startup_Clicked(object sender, RoutedEventArgs e)
        {
            // UWP startup methods disabled
            // if (IsRunningAsUWP())
            // {
            //     await StartupToggle();
            //     await StartupInit(false);
            // }
            // else
            if (!IsRunningAsUWP())
            {
                string lnk = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                    "FluentTB.lnk");

                if (System.IO.File.Exists(lnk))
                    System.IO.File.Delete(lnk);
                else
                    EnableStartup();
            }
        }

        public void EnableStartup()
        {
            try
            {
                string folder = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
                if (!Directory.Exists(folder))
                    Directory.CreateDirectory(folder);

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                dynamic shell = Activator.CreateInstance(shellType);
                string lnkPath = Path.Combine(folder, "FluentTB.lnk");
                dynamic shortcut = shell.CreateShortcut(lnkPath);
                shortcut.TargetPath   = Environment.GetCommandLineArgs()[0];
                shortcut.IconLocation = Environment.GetCommandLineArgs()[0];
                shortcut.Arguments    = "";
                shortcut.Description  = "Start FluentTB";
                shortcut.Save();
                Marshal.ReleaseComObject(shortcut);
                Marshal.ReleaseComObject(shell);
            }
            catch (Exception ex)
            {
                interaction.AddLog($"EnableStartup failed: {ex.Message}");
            }
        }

        /*
        // UWP-specific startup methods - disabled for now
        private async Task StartupToggle()
        {
            StartupTask task = await StartupTask.GetAsync("FTB");
            switch (task.State)
            {
                case StartupTaskState.Disabled:
                    await task.RequestEnableAsync();
                    StartupCheckBox.IsEnabled = true;
                    break;
                case StartupTaskState.Enabled:
                    task.Disable();
                    StartupCheckBox.IsEnabled = true;
                    break;
                default:
                    StartupCheckBox.IsEnabled = false;
                    break;
            }
        }

        private async Task StartupInit(bool clean)
        {
            StartupTask task = await StartupTask.GetAsync("FTB");
            switch (task.State)
            {
                case StartupTaskState.Disabled:
                    StartupCheckBox.IsChecked = false;
                    StartupCheckBox.IsEnabled = true;
                    StartupCheckBox.Content   = "Run at startup";
                    if (clean) { Visibility = Visibility.Visible; ShowMenuItem.Header = "Hide FluentTB"; }
                    break;
                case StartupTaskState.Enabled:
                    StartupCheckBox.IsChecked = true;
                    StartupCheckBox.IsEnabled = true;
                    StartupCheckBox.Content   = "Run at startup";
                    if (clean) { Visibility = Visibility.Hidden; ShowMenuItem.Header = "Show FluentTB"; }
                    break;
                case StartupTaskState.EnabledByPolicy:
                    StartupCheckBox.IsChecked = true;
                    StartupCheckBox.IsEnabled = false;
                    StartupCheckBox.Content   = "Startup mandatory";
                    if (clean) { Visibility = Visibility.Hidden; ShowMenuItem.Header = "Show FluentTB"; }
                    break;
                default:
                    StartupCheckBox.IsChecked = false;
                    StartupCheckBox.IsEnabled = false;
                    StartupCheckBox.Content   = "Startup unavailable";
                    if (clean) { Visibility = Visibility.Visible; ShowMenuItem.Header = "Hide FluentTB"; }
                    break;
            }
        }
        */


        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Checkbox event handlers
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        // Dynamic mode
        private void dynamicCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            centredCheckBox.IsEnabled       = true;
            showTrayOnHoverCheckBox.IsEnabled = true;
            showTrayOnHoverCheckBox.IsChecked = false;
            showTrayCheckBox.IsEnabled       = true;
            showTrayCheckBox.IsChecked       = true;
            mLeftLabel.Content  = "Outer Margin";
            mRightLabel.Content = "Inner Margin";
        }

        private void dynamicCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            centredCheckBox.IsEnabled        = false;
            centredCheckBox.IsChecked        = false;
            showTrayOnHoverCheckBox.IsEnabled = false;
            showTrayOnHoverCheckBox.IsChecked = false;
            showTrayCheckBox.IsEnabled        = false;
            showTrayCheckBox.IsChecked        = false;
            mLeftLabel.Content  = "Left Margin";
            mRightLabel.Content = "Right Margin";
        }

        // TranslucentTB compatibility â€” nur UI-Feedback, kein sofortiges Apply
        private void compositionFixCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            // Kein sofortiges Apply â€” erst beim Apply-Button wird pendingSettings
            // nach activeSettings Ã¼bertragen und TTB-Refresh ausgelÃ¶st.
        }

        private void compositionFixCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            // Ebenso â€” kein sofortiges Apply.
        }

        // Fill when maximised â€” nur abhÃ¤ngige UI-Elemente steuern
        private void fillMaximisedCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            fillAltTabCheckBox.IsEnabled = true;
        }

        private void fillMaximisedCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            fillAltTabCheckBox.IsChecked = false;
            fillAltTabCheckBox.IsEnabled = false;
        }

        // Show tray on hover
        private void showTrayOnHoverCheckBox_Checked(object sender, RoutedEventArgs e)
        {
            showTrayCheckBox.IsEnabled = false;
        }

        private void showTrayOnHoverCheckBox_Unchecked(object sender, RoutedEventArgs e)
        {
            showTrayCheckBox.IsEnabled = true;
        }


        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Slider event handlers
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void marginSlider_ValueChanged(object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            if (marginInput != null)
                marginInput.Text = Math.Round(marginSlider.Value).ToString();
        }

        private void marginSlider_DragCompleted(object sender,
            System.Windows.Controls.Primitives.DragCompletedEventArgs e)
        {
            // Kein automatisches Apply â€” Nutzer muss explizit Apply drÃ¼cken.
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
            // Kein automatisches Apply â€” Nutzer muss explizit Apply drÃ¼cken.
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Advanced panel toggle
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void advancedButton_Click(object sender, RoutedEventArgs e)
        {
            if (Width < 300)
            {
                Width = 393;
                AdvancedGrid.Visibility        = Visibility.Visible;
                advancedMarginsButton.Visibility = Visibility.Visible;
            }
            else
            {
                Width = 169;
                AdvancedGrid.Visibility        = Visibility.Collapsed;
                advancedMarginsButton.Visibility = Visibility.Hidden;
            }
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Advanced margins toggle (... button)
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void advancedMarginsButton_Click(object sender, RoutedEventArgs e)
        {
            bool enabling = !mTopInput.IsEnabled;
            mTopInput.IsEnabled    = enabling;
            mLeftInput.IsEnabled   = enabling;
            mBottomInput.IsEnabled = enabling;
            mRightInput.IsEnabled  = enabling;
            marginSlider.IsEnabled = !enabling;
            marginInput.IsEnabled  = !enabling;

            if (enabling)
            {
                marginInput.Text      = "Advanced";
                // Zeige aktuell gepufferte Werte â€” nicht die bereits committed activeSettings
                mTopInput.Text    = pendingSettings.MarginTop.ToString();
                mLeftInput.Text   = pendingSettings.MarginLeft.ToString();
                mBottomInput.Text = pendingSettings.MarginBottom.ToString();
                mRightInput.Text  = pendingSettings.MarginRight.ToString();
                pendingSettings.MarginBasic = -384;
            }
            else
            {
                int m = pendingSettings.MarginTop; // Fallback auf letzten Top-Wert
                pendingSettings.MarginBasic = m;
                marginInput.Text            = m.ToString();
                marginSlider.Value          = m;
            }
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  About window
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            var aw = new AboutWindow();
            aw.ShowDialog();
        }



        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Debug menu item (hidden, developer use only)
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private void DebugMenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (taskbarDetails.Count == 0) return;

            IntPtr hwndNext = LocalPInvoke.FindWindowExA(
                taskbarDetails[0].TaskbarHwnd, IntPtr.Zero, "Start", null);

            var children = new List<IntPtr> { hwndNext };
            while (true)
            {
                hwndNext = LocalPInvoke.FindWindowExA(
                    taskbarDetails[0].TaskbarHwnd, hwndNext, null, null);
                if (hwndNext == IntPtr.Zero || children.Contains(hwndNext)) break;
                children.Add(hwndNext);
            }

            foreach (IntPtr h in children)
            {
                LocalPInvoke.GetWindowRect(h, out LocalPInvoke.RECT r);
                LocalPInvoke.MoveWindow(h,
                    r.Left + 50, r.Top,
                    r.Right - r.Left,
                    r.Bottom - r.Top,
                    true);
            }
        }


        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  UWP helpers
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        public bool IsRunningAsUWP()
        {
            try
            {
                return new Helpers().IsRunningAsUwp();
            }
            catch
            {
                return false;
            }
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Windows theme detection & application
        //  (ported from old FluentTB code â€” ApplyWindowTheme, context menu theming)
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        /// <summary>Reads HKCU Themes\Personalize\AppsUseLightTheme. True = Light.</summary>
        public static bool ReadSystemUsesLightTheme()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key?.GetValue("AppsUseLightTheme") is int val)
                        return val != 0;
                }
            }
            catch { }
            return true;
        }

        /// <summary>
        /// Applies Dark/Light palette brushes to the window's ResourceDictionary.
        /// Called on first load and whenever SystemEvents.UserPreferenceChanged fires.
        /// </summary>
        public void ApplyWindowTheme()
        {
            bool isLight = ReadSystemUsesLightTheme();

            var winBg   = new SolidColorBrush(isLight
                ? System.Windows.Media.Color.FromRgb(0xF3, 0xF3, 0xF3)
                : System.Windows.Media.Color.FromRgb(0x20, 0x20, 0x20));
            var winFg   = new SolidColorBrush(isLight
                ? System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x1A)
                : System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
            var ctrlBg  = new SolidColorBrush(isLight
                ? System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF)
                : System.Windows.Media.Color.FromRgb(0x2D, 0x2D, 0x2D));
            var borderC = new SolidColorBrush(isLight
                ? System.Windows.Media.Color.FromRgb(0xCC, 0xCC, 0xCC)
                : System.Windows.Media.Color.FromRgb(0x3A, 0x3A, 0x3A));

            Resources["FtbWindowBackground"]  = winBg;
            Resources["FtbWindowForeground"]  = winFg;
            Resources["FtbControlBackground"] = ctrlBg;
            Resources["FtbBorderBrush"]       = borderC;
            Background = winBg;

            ApplyContextMenuTheme();
            interaction?.AddLog($"Theme applied: {(isLight ? "Light" : "Dark")}");
        }

        /// <summary>
        /// Pushes the current theme colours directly into the tray ContextMenu
        /// (which lives in a separate Popup and doesn't inherit Window.Resources).
        /// </summary>
        private void ApplyContextMenuTheme()
        {
            bool isLight = ReadSystemUsesLightTheme();

            var bg     = new SolidColorBrush(isLight
                ? System.Windows.Media.Color.FromRgb(0xF3, 0xF3, 0xF3)
                : System.Windows.Media.Color.FromRgb(0x20, 0x20, 0x20));
            var fg     = new SolidColorBrush(isLight
                ? System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x1A)
                : System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
            var border = new SolidColorBrush(isLight
                ? System.Windows.Media.Color.FromRgb(0xCC, 0xCC, 0xCC)
                : System.Windows.Media.Color.FromRgb(0x3A, 0x3A, 0x3A));

            var menu = TrayIcon?.ContextMenu;
            if (menu == null) return;

            menu.Background  = bg;
            menu.Foreground  = fg;
            menu.BorderBrush = border;

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
            }
        }

        /// <summary>
        /// Applies DWM dark-mode title bar to the given HWND.
        /// Only has an effect on Windows 11 (build â‰¥ 21996).
        /// </summary>
        public static void ApplyDwmDarkMode(IntPtr hwnd)
        {
            bool isDark = !ReadSystemUsesLightTheme();
            int  value  = isDark ? 1 : 0;
            LocalPInvoke.DwmSetWindowAttribute(
                hwnd,
                (LocalPInvoke.DWMWINDOWATTRIBUTE)LocalPInvoke.DWMWA_USE_IMMERSIVE_DARK_MODE,
                ref value,
                sizeof(int));
        }

        // Subscribed in OnSourceInitialized, unsubscribed in OnClosing
        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    TrayIconCheck();
                    ApplyWindowTheme();
                }));
            }
        }
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Private utility methods
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private Types.Settings BuildDefaultSettings()
        {
            return new Types.Settings
            {
                CornerRadius      = 7,
                MarginBasic       = 3,
                MarginBottom      = 0,
                MarginTop         = 0,
                MarginLeft        = 0,
                MarginRight       = 0,
                IsDynamic         = false,
                IsCentred         = false,
                IsWindows11       = true,
                ShowTray          = false,
                CompositionCompat = false,
                IsNotFirstLaunch  = false,
                FillOnMaximise    = true,
                FillOnTaskSwitch  = true,
                ShowTrayOnHover   = false,
                ShowTaskbarShadow = false
            };
        }

        /// <summary>
        /// Erstellt eine flache Kopie von <paramref name="src"/>.
        /// Wird genutzt um pendingSettings â†” activeSettings atomar auszutauschen.
        /// </summary>
        private static Types.Settings CopySettings(Types.Settings src)
        {
            return new Types.Settings
            {
                Version           = src.Version,
                CornerRadius      = src.CornerRadius,
                MarginBasic       = src.MarginBasic,
                MarginBottom      = src.MarginBottom,
                MarginLeft        = src.MarginLeft,
                MarginRight       = src.MarginRight,
                MarginTop         = src.MarginTop,
                IsDynamic         = src.IsDynamic,
                IsCentred         = src.IsCentred,
                IsWindows11       = src.IsWindows11,
                ShowTray          = src.ShowTray,
                CompositionCompat = src.CompositionCompat,
                IsNotFirstLaunch  = src.IsNotFirstLaunch,
                FillOnMaximise    = src.FillOnMaximise,
                FillOnTaskSwitch  = src.FillOnTaskSwitch,
                ShowTrayOnHover   = src.ShowTrayOnHover,
                // ShowTaskbarShadow = src.ShowTaskbarShadow,
                AutoHideMode      = src.AutoHideMode,
            };
        }

        private void LogSettings()
        {
            interaction.AddLog("Settings loaded:");
            interaction.AddLog(
                $"\nCornerRadius: {activeSettings.CornerRadius}\n" +
                $"MarginBasic: {activeSettings.MarginBasic}\n" +
                $"MarginTop: {activeSettings.MarginTop}\n" +
                $"MarginBottom: {activeSettings.MarginBottom}\n" +
                $"MarginLeft: {activeSettings.MarginLeft}\n" +
                $"MarginRight: {activeSettings.MarginRight}\n" +
                $"IsDynamic: {activeSettings.IsDynamic}\n" +
                $"IsCentred: {activeSettings.IsCentred}\n" +
                $"ShowTray: {activeSettings.ShowTray}\n" +
                $"CompositionCompat: {activeSettings.CompositionCompat}\n" +
                $"FillOnMaximise: {activeSettings.FillOnMaximise}\n" +
                $"FillOnTaskSwitch: {activeSettings.FillOnTaskSwitch}\n" +
                $"ShowTrayOnHover: {activeSettings.ShowTrayOnHover}\n" +
                $"ShowTaskbarShadow: {activeSettings.ShowTaskbarShadow}\n" +
                $"IsNotFirstLaunch: {activeSettings.IsNotFirstLaunch}\n"
            );
        }

        public void SyncTaskbarShadows(List<Types.Taskbar> taskbars, Types.Settings settings)
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new System.Action(() => SyncTaskbarShadows(taskbars, settings)));
                return;
            }

            if (taskbars == null)
                return;

            foreach (Types.Taskbar taskbar in taskbars)
            {
                if (!settings.ShowTaskbarShadow || taskbar.Ignored || taskbar.EffectiveRegion == null)
                {
                    taskbar.TaskbarEffectWindow?.Close();
                    taskbar.TaskbarEffectWindow = null;
                    continue;
                }

                if (taskbar.TaskbarEffectWindow == null)
                    taskbar.TaskbarEffectWindow = new TaskbarEffect();

                taskbar.TaskbarEffectWindow.UpdateShadow(taskbar.TaskbarRect, taskbar.EffectiveRegion);
            }
        }

        public void CloseTaskbarShadows()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.BeginInvoke(new System.Action(CloseTaskbarShadows));
                return;
            }

            foreach (Types.Taskbar taskbar in taskbarDetails)
            {
                taskbar.TaskbarEffectWindow?.Close();
                taskbar.TaskbarEffectWindow = null;
            }
        }

        private void splitHelpButton_Click(object sender, RoutedEventArgs e)
        {
            Infobox ib = new Infobox();
            ib.Title = "FluentTB - Split mode";
            ib.titleBlock.Text = "How to use Split Mode";
            ib.bodyBlock.Text = "Split mode allows dynamic taskbar resizing.";
            ib.ShowDialog();
        }
        
        private async void checkUpdateButton_Click(object sender, RoutedEventArgs e)
        {
            checkUpdateButton.IsEnabled = false;
            checkUpdateButton.Content = "Checking...";
            
            try
            {
                var update = await UpdateManager.CheckForUpdatesAsync();
                
                if (update == null)
                {
                    MessageBox.Show(
                        "Could not check for updates.\nPlease check your internet connection.",
                        "Update Check Failed",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning
                    );
                }
                else if (UpdateManager.IsNewerVersion(update.TagName))
                {
                    var result = MessageBox.Show(
                        $"A new version is available!\n\n" +
                        $"Current: v{UpdateManager.GetCurrentVersion()}\n" +
                        $"Latest: {update.TagName}\n\n" +
                        $"Would you like to download and install it?",
                        "Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information
                    );
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        await UpdateManager.DownloadAndInstallUpdate(update);
                    }
                }
                else
                {
                    MessageBox.Show(
                        $"You are running the latest version!\n\n" +
                        $"Current version: v{UpdateManager.GetCurrentVersion()}",
                        "No Updates Available",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information
                    );
                }
                
                UpdateManager.SaveLastUpdateCheck();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Update check error: {ex.Message}");
                MessageBox.Show(
                    $"Update check failed:\n{ex.Message}",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
            finally
            {
                checkUpdateButton.Content = "Updates";
                checkUpdateButton.IsEnabled = true;
            }
        }

    }
}
