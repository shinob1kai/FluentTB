using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;

namespace FluentTB
{
    /// <summary>
    /// Enumerates connected display monitors using EnumDisplayMonitors / GetMonitorInfo.
    /// </summary>
    internal static class MonitorStuff
    {
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
            EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lplmi);

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public uint               cbSize;
            public NativeMethods.RECT rcMonitor;
            public NativeMethods.RECT rcWork;
            public uint               dwFlags;
        }

        private delegate bool EnumMonitorsDelegate(
            IntPtr hMonitor, IntPtr hdcMonitor,
            ref NativeMethods.RECT lprcMonitor, IntPtr dwData);

        // -------------------------------------------------------------------------

        public class DisplayInfo
        {
            public string              Availability { get; set; } = string.Empty;
            public string              ScreenHeight { get; set; } = string.Empty;
            public string              ScreenWidth  { get; set; } = string.Empty;
            public NativeMethods.RECT  MonitorArea  { get; set; }
            public NativeMethods.RECT  WorkArea     { get; set; }
            public IntPtr              Handle       { get; set; }
            public int                 Top          { get; set; }
            public int                 Left         { get; set; }
        }

        public class DisplayInfoCollection : List<DisplayInfo> { }

        public static DisplayInfoCollection GetDisplays()
        {
            var col = new DisplayInfoCollection();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (IntPtr hMonitor, IntPtr hdcMonitor, ref NativeMethods.RECT lprcMonitor, IntPtr dwData) =>
                {
                    var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)) };
                    if (GetMonitorInfo(hMonitor, ref mi))
                    {
                        col.Add(new DisplayInfo
                        {
                            ScreenWidth  = (mi.rcMonitor.Right  - mi.rcMonitor.Left).ToString(),
                            ScreenHeight = (mi.rcMonitor.Bottom - mi.rcMonitor.Top).ToString(),
                            MonitorArea  = mi.rcMonitor,
                            WorkArea     = mi.rcWork,
                            Availability = mi.dwFlags.ToString(),
                            Handle       = hMonitor,
                            Top          = mi.rcMonitor.Top,
                            Left         = mi.rcMonitor.Left,
                        });
                    }
                    return true;
                }, IntPtr.Zero);

            return col;
        }
    }
}
