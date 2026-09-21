// Adapted from FluentFlyout. Copyright (c) 2024-2026 The FluentFlyout Authors.
// SPDX-License-Identifier: GPL-3.0-or-later
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
namespace FluentTB.Public;
public partial class LockWindow : Window
{
    private readonly DispatcherTimer timer = new();
    public LockWindow()
    {
        InitializeComponent();
        timer.Tick += (_, _) => { timer.Stop(); Hide(); };
        SourceInitialized += (_, _) => { var hwnd = new WindowInteropHelper(this).Handle; SetWindowLongPtr(hwnd, -20, GetWindowLongPtr(hwnd, -20) | 0x08000080); };
    }
    public void ShowStatus(string key, bool active)
    {
        if (!App.Settings.Enabled) return;
        var foreground = GetForegroundWindow();
        var focused = Forms.Screen.FromHandle(foreground);
        // Match FluentFlyout: suppress only exclusive Direct3D fullscreen, not a maximized desktop app.
        if (SHQueryUserNotificationState(out int notificationState) == 0 && notificationState == 3) return;
        LockTextBlock.Text = key == "Insert" ? FindResource("LockWindow_InsertPressed").ToString() : $"{FindResource("LockWindow_" + key)} {FindResource(active ? "LockWindow_LockOn" : "LockWindow_LockOff")}";
        LockTextBlock.FontWeight = App.Settings.Bold ? FontWeights.Medium : FontWeights.Normal;
        FlowDirection = System.Globalization.CultureInfo.CurrentUICulture.TextInfo.IsRightToLeft ? FlowDirection.RightToLeft : FlowDirection.LeftToRight;
        LockTextBlock.Measure(new Size(double.PositiveInfinity, 50));
        Width = Math.Max(160, LockTextBlock.DesiredSize.Width + 68);
        active = FluentTB.Input.LockKeyNotification.IndicatorActive(key, active);
        var duration = TimeSpan.FromMilliseconds(App.Settings.Animated ? 140 : 0);
        LockIndicatorRectangle.BeginAnimation(OpacityProperty, new DoubleAnimation(active || key == "Insert" ? 1 : .2, duration));
        LockIndicatorRectangle.BeginAnimation(WidthProperty, new DoubleAnimation(active ? 60 : 36, duration));
        ShackleRotation.BeginAnimation(RotateTransform.AngleProperty, new DoubleAnimation(active ? 0 : 25, duration));
        var screen = App.Settings.Monitor switch { 0 => Forms.Screen.PrimaryScreen!, 2 => Forms.Screen.FromPoint(Forms.Cursor.Position), _ => focused };
        var hwnd = new WindowInteropHelper(this).EnsureHandle();
        // Move to the target monitor before reading DPI; coordinates passed to Win32 are physical pixels.
        SetWindowPos(hwnd, -1, screen.WorkingArea.Left, screen.WorkingArea.Top, 0, 0, 0x11);
        Show();
        var scale = GetDpiForWindow(hwnd) / 96.0;
        int width = (int)Math.Ceiling(Width * scale), height = (int)Math.Ceiling(Height * scale);
        SetWindowPos(hwnd, -1, screen.WorkingArea.Left + (screen.WorkingArea.Width - width) / 2, screen.WorkingArea.Bottom - height - (int)(20 * scale), width, height, 0x10);
        timer.Stop(); timer.Interval = TimeSpan.FromMilliseconds(double.IsFinite(App.Settings.Duration) ? Math.Clamp(App.Settings.Duration, 250, 15000) : 2000); timer.Start();
    }
    protected override void OnClosed(EventArgs e) { timer.Stop(); base.OnClosed(e); }
    [DllImport("user32.dll")] private static extern nint GetForegroundWindow();
    [DllImport("shell32.dll")] private static extern int SHQueryUserNotificationState(out int state);
    [DllImport("user32.dll")] private static extern uint GetDpiForWindow(nint hwnd);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(nint hwnd, nint after, int x, int y, int width, int height, uint flags);
    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")] private static extern nint GetWindowLongPtr(nint hwnd, int index);
    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")] private static extern nint SetWindowLongPtr(nint hwnd, int index, nint value);
}
