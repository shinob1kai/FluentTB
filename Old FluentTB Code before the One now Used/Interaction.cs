using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;

namespace FluentTB
{
    /// <summary>
    /// Utility methods: settings I/O, TranslucentTB interop, hotkey hook, window enumeration.
    /// </summary>
    public class Interaction
    {
        private readonly MainWindow _mw;

        public Interaction()
        {
            _mw = (MainWindow)Application.Current.MainWindow;
        }

        // -------------------------------------------------------------------------
        // Settings persistence
        // -------------------------------------------------------------------------

        public Types.Settings ReadJSON()
        {
            string json = File.ReadAllText(_mw.ConfigPath);
            return JsonConvert.DeserializeObject<Types.Settings>(json)
                   ?? CreateDefaultSettings();
        }

        public void WriteJSON()
        {
            File.WriteAllText(_mw.ConfigPath,
                JsonConvert.SerializeObject(_mw.ActiveSettings, Formatting.Indented));
        }

        public void FileSystem()
        {
            // Always recreate the log file fresh at startup
            File.WriteAllText(_mw.LogPath, string.Empty);

            if (!File.Exists(_mw.ConfigPath) ||
                string.IsNullOrWhiteSpace(File.ReadAllText(_mw.ConfigPath)))
            {
                _mw.ActiveSettings = CreateDefaultSettings();
                WriteJSON();
            }
        }

        private Types.Settings CreateDefaultSettings()
        {
            return _mw.IsWindows11
                ? new Types.Settings
                {
                    CornerRadius      = 7,
                    MarginBasic       = 3,
                    IsDynamic         = false,
                    IsCentred         = false,
                    IsWindows11       = true,
                    ShowTray          = false,
                    CompositionCompat = false,
                    IsNotFirstLaunch  = false,
                    FillOnMaximise    = true,
                    FillOnTaskSwitch  = true,
                    ShowTrayOnHover   = false,
                }
                : new Types.Settings
                {
                    CornerRadius      = 16,
                    MarginBasic       = 2,
                    IsDynamic         = false,
                    IsCentred         = false,
                    IsWindows11       = false,
                    ShowTray          = false,
                    CompositionCompat = false,
                    IsNotFirstLaunch  = false,
                    FillOnMaximise    = true,
                    FillOnTaskSwitch  = false,
                    ShowTrayOnHover   = false,
                };
        }

        // -------------------------------------------------------------------------
        // Logging
        // -------------------------------------------------------------------------

        public void AddLog(string message)
        {
            try
            {
                File.AppendAllText(_mw.LogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}\n");
            }
            catch { /* never crash on logging */ }
        }

        // -------------------------------------------------------------------------
        // OS detection
        // -------------------------------------------------------------------------

        public static bool IsWindows11()
        {
            return Environment.OSVersion.Version.Build >= 21996;
        }

        // -------------------------------------------------------------------------
        // TranslucentTB interop
        // -------------------------------------------------------------------------

        /// <summary>Detect TranslucentTB via its named mutex.</summary>
        public static bool IsTranslucentTBRunning()
        {
            try
            {
                if (Mutex.TryOpenExisting("344635E9-9AE4-4E60-B128-D53E25AB70A7", out Mutex? mutex))
                {
                    mutex?.Dispose();
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ask TranslucentTB to force-refresh its effects on the given taskbar handle.
        /// TranslucentTB listens for TTB_ForceRefreshTaskbar on its TTB_WorkerWindow.
        /// </summary>
        public static IntPtr UpdateTranslucentTB(IntPtr taskbarHwnd)
        {
            IntPtr worker = NativeMethods.FindWindow("TTB_WorkerWindow", "TTB_WorkerWindow");
            int msg = NativeMethods.RegisterWindowMessage("TTB_ForceRefreshTaskbar");
            return NativeMethods.SendMessage(worker, msg, 0, taskbarHwnd);
        }

        /// <summary>Legacy WM_DWMCOMPOSITIONCHANGED nudge for non-TranslucentTB scenarios.</summary>
        public static void UpdateLegacyTB(IntPtr taskbarHwnd)
        {
            NativeMethods.SendMessage(taskbarHwnd, NativeMethods.WM_DWMCOMPOSITIONCHANGED, 1, IntPtr.Zero);
        }

        // -------------------------------------------------------------------------
        // Work area
        // -------------------------------------------------------------------------

        public static bool SetWorkspace(NativeMethods.RECT rect)
        {
            bool result = NativeMethods.SystemParametersInfo(
                NativeMethods.SPI_SETWORKAREA, 0, ref rect, NativeMethods.SPIF_change);
            if (!result)
                Debug.WriteLine("SetWorkspace failed: " + Marshal.GetLastWin32Error());
            return result;
        }

        public static bool IsAutoHideEnabled()
        {
            return Math.Abs(SystemParameters.PrimaryScreenHeight - SystemParameters.WorkArea.Height) > 0;
        }

        // -------------------------------------------------------------------------
        // Window enumeration
        // -------------------------------------------------------------------------

        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        public static List<IntPtr> GetTopLevelWindows()
        {
            var handles = new List<IntPtr>();
            GCHandle gcHandle = GCHandle.Alloc(handles);
            try
            {
                NativeMethods.EnumWindows(new EnumWindowsProc(EnumWindow), GCHandle.ToIntPtr(gcHandle));
            }
            finally
            {
                if (gcHandle.IsAllocated) gcHandle.Free();
            }
            return handles;
        }

        private static bool EnumWindow(IntPtr hwnd, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            if (gch.Target is List<IntPtr> list)
                list.Add(hwnd);
            return true;
        }

        // -------------------------------------------------------------------------
        // HwndHook — global hotkey (Win+F2 toggles ShowTray)
        // -------------------------------------------------------------------------

        public IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeMethods.WM_HOTKEY)
            {
                if (wParam.ToInt32() == 9000)
                {
                    int vkey = ((int)lParam >> 16) & 0xFFFF;
                    if (vkey == 0x71) // F2
                    {
                        _mw.Dispatcher.Invoke(() =>
                        {
                            _mw.showTrayCheckBox.IsChecked = !(_mw.showTrayCheckBox.IsChecked == true);
                            _mw.ApplyButton_Click(null, null);
                        });
                        handled = true;
                    }
                }
            }
            return IntPtr.Zero;
        }

        // -------------------------------------------------------------------------
        // Taskbar position helper (via SHAppBarMessage)
        // -------------------------------------------------------------------------

        public sealed class TaskbarInfo
        {
            public System.Drawing.Rectangle Bounds { get; private set; }
            public TaskbarPosition Position { get; private set; }
            public bool AlwaysOnTop { get; private set; }
            public bool AutoHide    { get; private set; }

            public TaskbarInfo(IntPtr taskbarHandle)
            {
                var data = new NativeMethods.APPBARDATA();
                data.cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.APPBARDATA));
                data.hWnd   = taskbarHandle;
                NativeMethods.SHAppBarMessage(NativeMethods.ABM.GetTaskbarPos, ref data);
                Position = (TaskbarPosition)data.uEdge;
                Bounds   = System.Drawing.Rectangle.FromLTRB(
                    data.rc.Left, data.rc.Top, data.rc.Right, data.rc.Bottom);

                data.cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.APPBARDATA));
                IntPtr result = NativeMethods.SHAppBarMessage(NativeMethods.ABM.GetState, ref data);
                int state = result.ToInt32();
                AlwaysOnTop = (state & NativeMethods.ABS.AlwaysOnTop) != 0;
                AutoHide    = (state & NativeMethods.ABS.Autohide)    != 0;
            }
        }

        public enum TaskbarPosition { Unknown = -1, Left, Top, Right, Bottom }

        // -------------------------------------------------------------------------
        // Monitor intersection
        // -------------------------------------------------------------------------

        public bool IsTaskbarVisibleOnMonitor(NativeMethods.RECT tbRectP, NativeMethods.RECT monitorRectP)
        {
            var tbRect      = new Rectangle(tbRectP.Left + 3, tbRectP.Top + 3,
                                            tbRectP.Right  - tbRectP.Left  - 3,
                                            tbRectP.Bottom - tbRectP.Top   - 3);
            var monitorRect = new Rectangle(monitorRectP.Left, monitorRectP.Top,
                                            monitorRectP.Right  - monitorRectP.Left,
                                            monitorRectP.Bottom - monitorRectP.Top);
            return tbRect.IntersectsWith(monitorRect);
        }
    }
}
