using System;
using System.Runtime.InteropServices;

namespace FluentTB
{
    /// <summary>
    /// Thin wrapper around SHAppBarMessage for taskbar AppBar state management.
    /// </summary>
    internal static class AppBars
    {
        public static void SetAppbarState(IntPtr hwnd, AppBarStates option)
        {
            var data = new NativeMethods.APPBARDATA
            {
                cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.APPBARDATA)),
                hWnd   = hwnd,
                lParam = (int)option
            };
            NativeMethods.SHAppBarMessage(NativeMethods.ABM.SetState, ref data);
        }

        public static AppBarStates GetAppbarState(IntPtr hwnd)
        {
            var data = new NativeMethods.APPBARDATA
            {
                cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.APPBARDATA)),
                hWnd   = hwnd
            };
            return (AppBarStates)NativeMethods.SHAppBarMessage(NativeMethods.ABM.GetState, ref data);
        }

        public static void MakeAppbarSad(IntPtr hwnd)
        {
            var data = new NativeMethods.APPBARDATA
            {
                cbSize = (uint)Marshal.SizeOf(typeof(NativeMethods.APPBARDATA)),
                hWnd   = hwnd
            };
            NativeMethods.SHAppBarMessage(NativeMethods.ABM.Remove, ref data);
        }

        public enum AppBarStates
        {
            AutoHide    = 0x01,
            AlwaysOnTop = 0x02,
        }
    }
}
