using Microsoft.Win32;
using System;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace FluentTB
{
    public partial class Infobox : Window
    {
        public Infobox()
        {
            InitializeComponent();

            // Apply theme colours before first render
            ApplyTheme();

            // React to live theme changes while the window is open
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
            Closed += (_, _) => SystemEvents.UserPreferenceChanged -= OnUserPreferenceChanged;

            // Apply DWM dark title bar once the HWND is available
            SourceInitialized += (_, _) =>
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                MainWindow.ApplyDwmDarkMode(hwnd);
            };
        }

        // ── Theme helpers ──────────────────────────────────────────────────────

        private void ApplyTheme()
        {
            bool isLight = MainWindow.ReadSystemUsesLightTheme();

            Color winBg   = isLight ? Color.FromRgb(0xF3, 0xF3, 0xF3)
                                    : Color.FromRgb(0x20, 0x20, 0x20);
            Color winFg   = isLight ? Color.FromRgb(0x1A, 0x1A, 0x1A)
                                    : Color.FromRgb(0xFF, 0xFF, 0xFF);
            Color ctrlBg  = isLight ? Color.FromRgb(0xFF, 0xFF, 0xFF)
                                    : Color.FromRgb(0x2D, 0x2D, 0x2D);
            Color borderC = isLight ? Color.FromRgb(0xCC, 0xCC, 0xCC)
                                    : Color.FromRgb(0x3A, 0x3A, 0x3A);

            Resources["FtbWindowBackground"]  = new SolidColorBrush(winBg);
            Resources["FtbWindowForeground"]  = new SolidColorBrush(winFg);
            Resources["FtbControlBackground"] = new SolidColorBrush(ctrlBg);
            Resources["FtbBorderBrush"]       = new SolidColorBrush(borderC);

            Background = new SolidColorBrush(winBg);

            // If the HWND already exists (live theme switch), update title bar too
            if (IsInitialized)
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                    MainWindow.ApplyDwmDarkMode(hwnd);
            }
        }

        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category != UserPreferenceCategory.General) return;
            Dispatcher.BeginInvoke(ApplyTheme);
        }

        // ── Button handler ─────────────────────────────────────────────────────

        private void okButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}
