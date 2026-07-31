using System;
using System.Runtime.InteropServices;
using System.Text;

namespace FluentTB
{
    /// <summary>
    /// All Win32 / DWM / Shell P/Invoke declarations for FluentTB.
    ///
    /// Pillar C — TranslucentTB Compatibility:
    ///   All structs use the default CLR SequentialLayout with no explicit Pack value.
    ///   This guarantees the correct 8-byte natural alignment on x64, preventing handle
    ///   mismatches that would cause TranslucentTB to lose its hooks.
    /// </summary>
    public static class NativeMethods
    {
        // -------------------------------------------------------------------------
        // Structs
        // -------------------------------------------------------------------------

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int x;
            public int y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct APPBARDATA
        {
            public uint   cbSize;
            public IntPtr hWnd;
            public uint   uCallbackMessage;
            public ABE    uEdge;
            public RECT   rc;
            public int    lParam;
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential)]
        public struct WINDOWPLACEMENT
        {
            public int              Length;
            public int              Flags;
            public ShowWindowCommands ShowCmd;
            public POINT            MinPosition;
            public POINT            MaxPosition;
            public RECT             NormalPosition;

            public static WINDOWPLACEMENT Default
            {
                get
                {
                    var result = new WINDOWPLACEMENT();
                    result.Length = Marshal.SizeOf(result);
                    return result;
                }
            }
        }

        // -------------------------------------------------------------------------
        // Enumerations
        // -------------------------------------------------------------------------

        public enum DWMWINDOWATTRIBUTE : uint
        {
            NCRenderingEnabled        = 1,
            NCRenderingPolicy         = 2,
            TransitionsForceDisabled  = 3,
            AllowNCPaint              = 4,
            CaptionButtonBounds       = 5,
            NonClientRtlLayout        = 6,
            ForceIconicRepresentation = 7,
            Flip3DPolicy              = 8,
            ExtendedFrameBounds       = 9,
            HasIconicBitmap           = 10,
            DisallowPeek              = 11,
            ExcludedFromPeek          = 12,
            Cloak                     = 13,
            Cloaked                   = 14,
            FreezeRepresentation      = 15,
            // Win11 22000+
            DWMWA_BORDER_COLOR        = 34,
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
        }

        public enum ABM : uint
        {
            New                = 0x00000000,
            Remove             = 0x00000001,
            QueryPos           = 0x00000002,
            SetPos             = 0x00000003,
            GetState           = 0x00000004,
            GetTaskbarPos      = 0x00000005,
            Activate           = 0x00000006,
            GetAutoHideBar     = 0x00000007,
            SetAutoHideBar     = 0x00000008,
            WindowPosChanged   = 0x00000009,
            SetState           = 0x0000000A,
        }

        public enum ABE : uint
        {
            Left   = 0,
            Top    = 1,
            Right  = 2,
            Bottom = 3,
        }

        public static class ABS
        {
            public const int Autohide    = 0x0000001;
            public const int AlwaysOnTop = 0x0000002;
        }

        public enum ShowWindowCommands
        {
            Hide            = 0,
            Normal          = 1,
            ShowMinimized   = 2,
            Maximize        = 3,
            ShowMaximized   = 3,
            ShowNoActivate  = 4,
            Show            = 5,
            Minimize        = 6,
            ShowMinNoActive = 7,
            ShowNA          = 8,
            Restore         = 9,
            ShowDefault     = 10,
            ForceMinimize   = 11,
        }

        [Flags]
        public enum SetWindowPosFlags : uint
        {
            IgnoreResize          = 0x0001,
            IgnoreMove            = 0x0002,
            IgnoreZOrder          = 0x0004,
            DoNotRedraw           = 0x0008,
            DoNotActivate         = 0x0010,
            FrameChanged          = 0x0020,   // SWP_FRAMECHANGED / SWP_DRAWFRAME
            ShowWindow            = 0x0040,
            HideWindow            = 0x0080,
            DoNotCopyBits         = 0x0100,
            DoNotChangeOwnerZOrder = 0x0200,
            DoNotSendChangingEvent = 0x0400,
            DeferErase            = 0x2000,
            AsynchronousWindowPosition = 0x4000,
        }

        [Flags]
        public enum RedrawWindowFlags : uint
        {
            Invalidate    = 0x1,
            InternalPaint = 0x2,
            Erase         = 0x4,
            Validate      = 0x8,
            NoInternalPaint = 0x10,
            NoErase       = 0x20,
            NoChildren    = 0x40,
            AllChildren   = 0x80,
            UpdateNow     = 0x100,
            EraseNow      = 0x200,
            Frame         = 0x400,
            NoFrame       = 0x800,
        }

        // GWL indices for GetWindowLong / SetWindowLong
        public const int GWL_STYLE   = -16;
        public const int GWL_EXSTYLE = -20;

        // Window styles (subset)
        public const uint WS_BORDER      = 0x00800000;
        public const uint WS_THICKFRAME  = 0x00040000;

        // SetWindowPos "insert after" sentinels
        public static readonly IntPtr HWND_TOPMOST   = new IntPtr(-1);
        public static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);

        // DWM transparent border colour sentinel (COLORREF 0xFFFFFFFE = "no border")
        public const uint DWMWA_COLOR_NONE = 0xFFFFFFFE;

        // Misc Win32 constants
        public const int WM_HOTKEY                = 0x0312;
        public const int WM_DWMCOMPOSITIONCHANGED = 0x031E;

        public const int SPIF_UPDATEINIFILE    = 1;
        public const int SPIF_SENDWININICHANGE = 2;
        public const int SPIF_change           = SPIF_UPDATEINIFILE | SPIF_SENDWININICHANGE;
        public const int SPI_SETWORKAREA       = 47;
        public const int SPI_GETWORKAREA       = 48;

        public const int SW_HIDE         = 0;
        public const int SW_SHOWNORMAL   = 1;
        public const int SW_SHOWMINIMIZED = 2;
        public const int SW_SHOWMAXIMIZED = 3;
        public const int SW_SHOWNOACTIVATE = 4;
        public const int SW_RESTORE       = 9;

        // -------------------------------------------------------------------------
        // User32
        // -------------------------------------------------------------------------

        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, SetWindowPosFlags uFlags);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("user32.dll")]
        public static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool RedrawWindow(IntPtr hWnd, IntPtr lprcUpdate, IntPtr hrgnUpdate, RedrawWindowFlags flags);

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, int Msg, int wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool PtInRect(ref RECT lprc, POINT pt);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern int GetWindowRgn(IntPtr hWnd, IntPtr hRgn);

        [DllImport("user32.dll")]
        public static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("user32.dll")]
        public static extern IntPtr FindWindowExA(IntPtr hWndParent, IntPtr hWndChildAfter, string? lpszClass, string? lpszWindow);

        [DllImport("user32.dll")]
        public static extern int GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [DllImport("user32.dll")]
        public static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("user32.dll")]
        public static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowRect(HandleRef hWnd, out RECT lpRect);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern int RegisterWindowMessage(string lpString);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetCursorPos(out POINT lpPoint);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool SystemParametersInfo(int uiAction, int uiParam, ref RECT pvParam, int fWinIni);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool IsWindowVisible(IntPtr hWnd);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool EnumWindows(Interaction.EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool GetWindowPlacement(IntPtr hWnd, ref WINDOWPLACEMENT lpwndpl);

        [DllImport("user32.dll", SetLastError = true, EntryPoint = "GetClassNameW", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern IntPtr WindowFromPoint(POINT p);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

        [DllImport("user32.dll")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        public static extern bool ShowWindowAsync(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        public static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        public static extern bool SetWindowText(IntPtr hwnd, string lpString);

        /// <summary>Read a window's Win32 style flags.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint GetWindowLong(IntPtr hWnd, int nIndex);

        /// <summary>Modify a window's Win32 style flags.</summary>
        [DllImport("user32.dll", SetLastError = true)]
        public static extern uint SetWindowLong(IntPtr hWnd, int nIndex, uint dwNewLong);

        // -------------------------------------------------------------------------
        // GDI32
        // -------------------------------------------------------------------------

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRoundRectRgn(int x1, int y1, int x2, int y2, int w, int h);

        [DllImport("gdi32.dll")]
        public static extern IntPtr CreateRectRgn(int x1, int y1, int x2, int y2);

        [DllImport("gdi32.dll")]
        public static extern int CombineRgn(IntPtr hrgnDest, IntPtr hrgnSrc1, IntPtr hrgnSrc2, int fnCombineMode);

        [DllImport("gdi32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool DeleteObject(IntPtr hObject);

        // -------------------------------------------------------------------------
        // DWM
        // -------------------------------------------------------------------------

        [DllImport("dwmapi.dll")]
        public static extern int DwmGetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, out bool pvAttribute, int cbAttribute);

        /// <summary>Overload for setting a COLORREF (uint) attribute such as DWMWA_BORDER_COLOR.</summary>
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref uint pvAttribute, int cbAttribute);

        /// <summary>Overload for setting an int/bool attribute such as DWMWA_USE_IMMERSIVE_DARK_MODE.</summary>
        [DllImport("dwmapi.dll")]
        public static extern int DwmSetWindowAttribute(IntPtr hwnd, DWMWINDOWATTRIBUTE dwAttribute, ref int pvAttribute, int cbAttribute);

        /// <summary>Flush the DWM thumbnail / composition cache for a window.</summary>
        [DllImport("dwmapi.dll")]
        public static extern int DwmFlush();

        // -------------------------------------------------------------------------
        // Shell32
        // -------------------------------------------------------------------------

        [DllImport("shell32.dll", SetLastError = true)]
        public static extern IntPtr SHAppBarMessage(ABM dwMessage, [In] ref APPBARDATA pData);
    }
}
