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
    public class Interaction
    {
        public MainWindow mw;

        public Interaction()
        {
            mw = (MainWindow)Application.Current.MainWindow;
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Settings persistence
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads and deserialises settings from disk.
        /// Returns null if the file is missing, empty, or corrupt.
        /// </summary>
        public Types.Settings ReadJSON()
        {
            try
            {
                string text = File.ReadAllText(mw.configPath);
                if (string.IsNullOrWhiteSpace(text)) return null;
                return JsonConvert.DeserializeObject<Types.Settings>(text);
            }
            catch (Exception ex)
            {
                AddLog($"ReadJSON failed: {ex.Message}");
                return null;
            }
        }

        public void WriteJSON()
        {
            try
            {
                File.WriteAllText(mw.configPath,
                    JsonConvert.SerializeObject(mw.activeSettings, Formatting.Indented));
            }
            catch (Exception ex)
            {
                AddLog($"WriteJSON failed: {ex.Message}");
            }
        }

        /// <summary>
        /// Ensures config and log files exist on disk.
        /// Writes default settings when the config is absent or empty.
        /// </summary>
        public void FileSystem()
        {
            // Always (re-)create the log so old sessions don't accumulate
            File.Create(mw.logPath).Close();

            if (!File.Exists(mw.configPath) ||
                string.IsNullOrWhiteSpace(File.ReadAllText(mw.configPath)))
            {
                mw.activeSettings = new Types.Settings
                {
                    CornerRadius = 7,
                    MarginBasic = 3,
                    IsWindows11 = true,
                    FillOnMaximise = true,
                    FillOnTaskSwitch = true
                };
                WriteJSON();
            }
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Logging
        // ──────────────────────────────────────────────────────────────────────

        public void AddLog(string message)
        {
            try
            {
                File.AppendAllText(mw.logPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\n");
            }
            catch { /* never let logging crash the app */ }
        }

        // ──────────────────────────────────────────────────────────────────────
        //  TranslucentTB detection & signalling
        // ──────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Detects whether TranslucentTB is running by checking its named mutex.
        /// The mutex GUID matches TranslucentTB 2021.5+.
        /// </summary>
        public static bool IsTranslucentTBRunning()
        {
            try
            {
                bool found = Mutex.TryOpenExisting(
                    "344635E9-9AE4-4E60-B128-D53E25AB70A7",
                    out Mutex m);
                m?.Dispose();
                return found;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Requests TranslucentTB to force-refresh the given taskbar.
        /// Call this after every SetWindowRgn when CompositionCompat is on.
        /// </summary>
        public static IntPtr UpdateTranslucentTB(IntPtr taskbarHwnd)
        {
            return LocalPInvoke.SendMessage(
                LocalPInvoke.FindWindow("TTB_WorkerWindow", "TTB_WorkerWindow"),
                LocalPInvoke.RegisterWindowMessage("TTB_ForceRefreshTaskbar"),
                0,
                taskbarHwnd);
        }

        /// <summary>
        /// Legacy composition refresh via WM_DWMCOMPOSITIONCHANGED.
        /// Used as a fallback when TTB is not running.
        /// </summary>
        public static void UpdateLegacyTB(IntPtr taskbarHwnd)
        {
            const int WM_DWMCOMPOSITIONCHANGED = 789;
            LocalPInvoke.SendMessage(taskbarHwnd, WM_DWMCOMPOSITIONCHANGED, 1, IntPtr.Zero);
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Workspace area
        // ──────────────────────────────────────────────────────────────────────

        public static bool SetWorkspace(LocalPInvoke.RECT rect)
        {
            bool ok = LocalPInvoke.SystemParametersInfo(
                LocalPInvoke.SPI_SETWORKAREA, 0, ref rect, LocalPInvoke.SPIF_change);
            if (!ok)
                Debug.WriteLine($"SetWorkspace error: {Marshal.GetLastWin32Error()}");
            return ok;
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Window enumeration helpers
        // ──────────────────────────────────────────────────────────────────────

        public delegate bool EnumWindowsProc(IntPtr hwnd, IntPtr lParam);

        public static List<IntPtr> GetTopLevelWindows()
        {
            var list = new List<IntPtr>();
            GCHandle handle = GCHandle.Alloc(list);
            try
            {
                LocalPInvoke.EnumWindows(new EnumWindowsProc(EnumWindow),
                    GCHandle.ToIntPtr(handle));
            }
            finally
            {
                if (handle.IsAllocated) handle.Free();
            }
            return list;
        }

        private static bool EnumWindow(IntPtr hwnd, IntPtr pointer)
        {
            var list = GCHandle.FromIntPtr(pointer).Target as List<IntPtr>
                ?? throw new InvalidCastException("GCHandle target is not List<IntPtr>");
            list.Add(hwnd);
            return true;
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Misc helpers
        // ──────────────────────────────────────────────────────────────────────

        public static bool IsAutoHideEnabled()
            => Math.Abs(SystemParameters.PrimaryScreenHeight -
                        SystemParameters.WorkArea.Height) > 0;

        public bool IsTaskbarVisibleOnMonitor(
            LocalPInvoke.RECT tbRectP, LocalPInvoke.RECT monitorRectP)
        {
            var tbRect  = new Rectangle(tbRectP.Left + 3, tbRectP.Top + 3,
                                        tbRectP.Right  - tbRectP.Left - 3,
                                        tbRectP.Bottom - tbRectP.Top  - 3);
            var monRect = new Rectangle(monitorRectP.Left, monitorRectP.Top,
                                        monitorRectP.Right  - monitorRectP.Left,
                                        monitorRectP.Bottom - monitorRectP.Top);
            return tbRect.IntersectsWith(monRect);
        }

        // ──────────────────────────────────────────────────────────────────────
        //  HwndHook — handles the Win+F2 hotkey forwarded from MainWindow
        // ──────────────────────────────────────────────────────────────────────

        public IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam,
                               ref bool handled)
        {
            // All hotkey logic is now handled in MainWindow.WndProc.
            // This method is kept for backwards compatibility with any code that
            // still calls it via AddHook, but does nothing on its own.
            return IntPtr.Zero;
        }

        // ──────────────────────────────────────────────────────────────────────
        //  Taskbar helper class (used by Background for appbar position queries)
        // ──────────────────────────────────────────────────────────────────────

        public enum TaskbarPosition { Unknown = -1, Left, Top, Right, Bottom }

        public sealed class TaskbarInfo
        {
            public Rectangle      Bounds      { get; }
            public TaskbarPosition Position   { get; }
            public System.Drawing.Point Location => Bounds.Location;
            public System.Drawing.Size  Size     => Bounds.Size;
            public bool AlwaysOnTop { get; }
            public bool AutoHide    { get; }

            public TaskbarInfo(IntPtr taskbarHandle)
            {
                var data = new LocalPInvoke.APPBARDATA
                {
                    cbSize = (uint)Marshal.SizeOf(typeof(LocalPInvoke.APPBARDATA)),
                    hWnd   = taskbarHandle
                };

                IntPtr result = LocalPInvoke.SHAppBarMessage(
                    LocalPInvoke.ABM.GetTaskbarPos, ref data);
                Position = (TaskbarPosition)data.uEdge;
                Bounds   = Rectangle.FromLTRB(
                    data.rc.Left, data.rc.Top, data.rc.Right, data.rc.Bottom);

                data.cbSize = (uint)Marshal.SizeOf(typeof(LocalPInvoke.APPBARDATA));
                result      = LocalPInvoke.SHAppBarMessage(
                    LocalPInvoke.ABM.GetState, ref data);
                int state   = result.ToInt32();
                AlwaysOnTop = (state & LocalPInvoke.ABS.AlwaysOnTop) != 0;
                AutoHide    = (state & LocalPInvoke.ABS.Autohide)    != 0;
            }
        }
    }
}
