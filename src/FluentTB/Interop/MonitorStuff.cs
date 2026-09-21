using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Windows;

namespace FluentTB
{
    static class MonitorStuff
    {
        [DllImport("user32.dll")]
        private static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip,
            EnumMonitorsDelegate lpfnEnum, IntPtr dwData);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lplmi);

        [StructLayout(LayoutKind.Sequential)]
        public struct MONITORINFO
        {
            public uint            cbSize;
            public LocalPInvoke.RECT rcMonitor;
            public LocalPInvoke.RECT rcWork;
            public uint            dwFlags;
        }

        private delegate bool EnumMonitorsDelegate(IntPtr hMonitor, IntPtr hdcMonitor,
            ref LocalPInvoke.RECT lprcMonitor, IntPtr dwData);

        // ── Public surface ────────────────────────────────────────────────────

        /// <summary>Returns a snapshot of all connected display monitors.</summary>
        public static DisplayInfoCollection GetDisplays()
        {
            var col = new DisplayInfoCollection();

            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                (IntPtr hMon, IntPtr hdcMon, ref LocalPInvoke.RECT lprc, IntPtr dw) =>
                {
                    var mi = new MONITORINFO { cbSize = (uint)Marshal.SizeOf(typeof(MONITORINFO)) };
                    if (GetMonitorInfo(hMon, ref mi))
                    {
                        col.Add(new DisplayInfo
                        {
                            Handle      = hMon,
                            MonitorArea = mi.rcMonitor,
                            WorkArea    = mi.rcWork,
                            ScreenWidth  = (mi.rcMonitor.Right  - mi.rcMonitor.Left).ToString(),
                            ScreenHeight = (mi.rcMonitor.Bottom - mi.rcMonitor.Top).ToString(),
                            Availability = mi.dwFlags.ToString(),
                            Top          = mi.rcMonitor.Top,
                            Left         = mi.rcMonitor.Left
                        });
                    }
                    return true;
                },
                IntPtr.Zero);

            return col;
        }

        // ── Data types ────────────────────────────────────────────────────────

        public class DisplayInfoCollection : List<DisplayInfo> { }

        public class DisplayInfo
        {
            public string           Availability { get; set; }
            public string           ScreenHeight { get; set; }
            public string           ScreenWidth  { get; set; }
            public LocalPInvoke.RECT MonitorArea { get; set; }
            public LocalPInvoke.RECT WorkArea    { get; set; }
            public IntPtr           Handle       { get; set; }
            public int              Top          { get; set; }
            public int              Left         { get; set; }
        }
    }
}
