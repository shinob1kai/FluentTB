using System;
using System.Runtime.InteropServices;

namespace FluentTB
{
    /// <summary>
    /// Thin wrapper around SHAppBarMessage for querying and setting taskbar appbar state.
    /// </summary>
    static class AppBars
    {
        [DllImport("shell32.dll")]
        private static extern IntPtr SHAppBarMessage(uint dwMessage, ref APPBARDATA pData);

        public enum AppBarMessages : uint
        {
            New              = 0x00,
            Remove           = 0x01,
            QueryPos         = 0x02,
            SetPos           = 0x03,
            GetState         = 0x04,
            GetTaskBarPos    = 0x05,
            Activate         = 0x06,
            GetAutoHideBar   = 0x07,
            SetAutoHideBar   = 0x08,
            WindowPosChanged = 0x09,
            SetState         = 0x0A
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct APPBARDATA
        {
            public uint   cbSize;
            public IntPtr hWnd;
            public uint   uCallbackMessage;
            public uint   uEdge;
            public LocalPInvoke.RECT rc;
            public int    lParam;
        }

        public enum AppBarStates
        {
            AutoHide    = 0x01,
            AlwaysOnTop = 0x02
        }

        /// <summary>Sets the appbar state (autohide / always-on-top).</summary>
        public static void SetAppbarState(IntPtr hwnd, AppBarStates option)
        {
            APPBARDATA d = Build(hwnd);
            d.lParam = (int)option;
            SHAppBarMessage((uint)AppBarMessages.SetState, ref d);
        }

        /// <summary>Gets the current appbar state.</summary>
        public static AppBarStates GetAppbarState(IntPtr hwnd)
        {
            APPBARDATA d = Build(hwnd);
            return (AppBarStates)SHAppBarMessage((uint)AppBarMessages.GetState, ref d);
        }

        /// <summary>Unregisters the taskbar as an appbar.</summary>
        public static void MakeAppbarSad(IntPtr hwnd)
        {
            APPBARDATA d = Build(hwnd);
            SHAppBarMessage((uint)AppBarMessages.Remove, ref d);
        }

        /// <summary>Sets the appbar position rectangle.</summary>
        public static void SetAppbarRect(IntPtr hwnd, LocalPInvoke.RECT rc)
        {
            APPBARDATA d = Build(hwnd);
            d.rc    = rc;
            d.uEdge = 0x3; // ABE_BOTTOM
            SHAppBarMessage((uint)AppBarMessages.SetPos, ref d);
        }

        private static APPBARDATA Build(IntPtr hwnd) => new APPBARDATA
        {
            cbSize = (uint)Marshal.SizeOf(typeof(APPBARDATA)),
            hWnd   = hwnd
        };
    }
}
