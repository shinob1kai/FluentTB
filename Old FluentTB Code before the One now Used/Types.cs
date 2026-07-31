using System;

namespace FluentTB
{
    /// <summary>
    /// Shared data structures for FluentTB.
    /// </summary>
    public static class Types
    {
        /// <summary>Per-taskbar runtime state.</summary>
        public class Taskbar
        {
            public IntPtr TaskbarHwnd { get; set; }   // Handle to Shell_TrayWnd / Shell_SecondaryTrayWnd
            public IntPtr TrayHwnd    { get; set; }   // Handle to TrayNotifyWnd (clock/notification area)
            public IntPtr AppListHwnd { get; set; }   // Handle to MSTaskSwWClass / MSTaskListWClass

            public NativeMethods.RECT TaskbarRect  { get; set; }
            public NativeMethods.RECT TrayRect     { get; set; }
            public NativeMethods.RECT AppListRect  { get; set; }

            public IntPtr RecoveryHrgn { get; set; }   // Saved region for clean reset
            public double ScaleFactor  { get; set; }   // DPI/96.0
            public string TaskbarRes   { get; set; } = string.Empty;
            public bool   Ignored      { get; set; }   // Skip this taskbar this iteration
            public bool   TrayHidden   { get; set; }
            public int    AppListWidth { get; set; }
        }

        /// <summary>User configuration — serialised to %LocalAppData%\ftb.json.</summary>
        public class Settings
        {
            public int  Version        { get; set; }
            public int  CornerRadius   { get; set; }
            public int  MarginBasic    { get; set; }   // -384 = advanced (per-side) mode
            public int  MarginBottom   { get; set; }
            public int  MarginLeft     { get; set; }
            public int  MarginRight    { get; set; }
            public int  MarginTop      { get; set; }
            public bool IsDynamic      { get; set; }
            public bool IsCentred      { get; set; }
            public bool IsWindows11    { get; set; }
            public bool ShowTray       { get; set; }
            public bool CompositionCompat  { get; set; }
            public bool IsNotFirstLaunch   { get; set; }
            public bool FillOnMaximise     { get; set; }
            public bool FillOnTaskSwitch   { get; set; }
            public bool ShowTrayOnHover    { get; set; }
            public int  AutoHideMode       { get; set; }  // 0 = AlwaysShow, 1 = AlwaysHide
        }

        /// <summary>Computed region parameters after scaling.</summary>
        public class EffectiveRegion
        {
            public int CornerRadius { get; set; }
            public int Top    { get; set; }
            public int Left   { get; set; }
            public int Width  { get; set; }
            public int Height { get; set; }
        }

        public enum TrayMode
        {
            Show     = 0,
            Hide     = 1,
            AutoHide = 2,
        }

        public enum CompositionMode
        {
            None         = 0,
            TranslucentTB = 1,
            Legacy        = 2,
        }

        public enum KeyModifier
        {
            None    = 0,
            Alt     = 1,
            Control = 2,
            Shift   = 4,
            WinKey  = 8,
        }
    }
}
