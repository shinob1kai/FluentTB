using System;

namespace FluentTB
{
    public class Types
    {
        public class Taskbar
        {
            public IntPtr TaskbarHwnd   { get; set; } // Handle to the taskbar window
            public IntPtr TrayHwnd      { get; set; } // Handle to the tray notification area
            public IntPtr AppListHwnd   { get; set; } // Handle to the open/pinned-apps list
            public LocalPInvoke.RECT TaskbarRect  { get; set; } // Bounding box of the taskbar
            public LocalPInvoke.RECT TrayRect     { get; set; } // Bounding box of the tray (dynamic)
            public LocalPInvoke.RECT AppListRect  { get; set; } // Bounding box of the app list (dynamic)
            public IntPtr RecoveryHrgn  { get; set; } // Recovery region; defaults to IntPtr.Zero
            public double ScaleFactor   { get; set; } // DPI scale factor of the taskbar's monitor
            public string TaskbarRes    { get; set; } // Taskbar resolution as a display string
            public bool   Ignored       { get; set; } // True when the taskbar should be skipped this tick
            public bool   TrayHidden    { get; set; } // True when FTB has hidden the tray on this taskbar
            public int    AppListWidth  { get; set; } // Cached width of the app list
            public TaskbarEffect TaskbarEffectWindow { get; set; }
            public EffectiveRegion EffectiveRegion { get; set; }
            public IntPtr ClockHwnd     { get; set; }
            public IntPtr WidgetsHwnd   { get; set; }
            public LocalPInvoke.RECT ClockRect    { get; set; }
            public LocalPInvoke.RECT WidgetsRect  { get; set; }
        }

        public class Settings
        {
            public int  Version           { get; set; }
            public int  CornerRadius      { get; set; }
            public int  MarginBasic       { get; set; }  // -384 = advanced/independent mode
            public int  MarginBottom      { get; set; }
            public int  MarginLeft        { get; set; }
            public int  MarginRight       { get; set; }
            public int  MarginTop         { get; set; }
            public bool IsDynamic         { get; set; }
            public bool IsCentred         { get; set; }
            public bool IsWindows11       { get; set; }
            public bool ShowTray          { get; set; }
            public bool CompositionCompat { get; set; }
            public bool IsNotFirstLaunch  { get; set; }
            public bool FillOnMaximise    { get; set; }
            public bool FillOnTaskSwitch  { get; set; }
            public bool ShowTrayOnHover   { get; set; }
            public bool ShowTaskbarShadow { get; set; }
            public int  AutoHideMode      { get; set; }  // 0 = AlwaysShow, 1 = AlwaysHide
            public bool ShowWidgets       { get; set; }
            public bool ShowClock         { get; set; }
            public bool ShowSegmentsOnHover { get; set; }

            /// <summary>
            /// Erstellt eine flache Wertkopie dieser Settings-Instanz.
            /// Wird vom Background-Thread genutzt (Background.cs) um eine
            /// thread-sichere Arbeitskopie zu erstellen, ohne die vom UI-Thread
            /// gehaltene activeSettings-Instanz zu mutieren.
            /// Alle Felder sind Value-Types (int, bool) — eine flache Kopie
            /// ist hier vollständig äquivalent zu einer tiefen Kopie.
            /// </summary>
            public Settings Clone()
            {
                return new Settings
                {
                    Version           = this.Version,
                    CornerRadius      = this.CornerRadius,
                    MarginBasic       = this.MarginBasic,
                    MarginBottom      = this.MarginBottom,
                    MarginLeft        = this.MarginLeft,
                    MarginRight       = this.MarginRight,
                    MarginTop         = this.MarginTop,
                    IsDynamic         = this.IsDynamic,
                    IsCentred         = this.IsCentred,
                    IsWindows11       = this.IsWindows11,
                    ShowTray          = this.ShowTray,
                    CompositionCompat = this.CompositionCompat,
                    IsNotFirstLaunch  = this.IsNotFirstLaunch,
                    FillOnMaximise    = this.FillOnMaximise,
                    FillOnTaskSwitch  = this.FillOnTaskSwitch,
                    ShowTrayOnHover   = this.ShowTrayOnHover,
                    ShowTaskbarShadow = this.ShowTaskbarShadow,
                    AutoHideMode      = this.AutoHideMode,
                    ShowWidgets       = this.ShowWidgets,
                    ShowClock         = this.ShowClock,
                    ShowSegmentsOnHover = this.ShowSegmentsOnHover,
                };
            }
        }

        public class EffectiveRegion
        {
            public int CornerRadius { get; set; }
            public int Top          { get; set; }
            public int Left         { get; set; }
            public int Width        { get; set; }
            public int Height       { get; set; }
        }

        public enum TrayMode
        {
            Show     = 0,
            Hide     = 1,
            AutoHide = 2,
        }

        public enum CompositionMode
        {
            None        = 0,
            TranslucentTB = 1,
            Legacy      = 2,
        }

        public enum KeyModifier
        {
            None    = 0,
            Alt     = 1,
            Control = 2,
            Shift   = 4,
            WinKey  = 8
        }
    }
}
