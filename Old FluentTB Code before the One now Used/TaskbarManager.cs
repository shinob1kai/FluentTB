using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows;

namespace FluentTB
{
    /// <summary>
    /// Core taskbar geometry engine.
    ///
    /// Bugfix summary embedded in this file:
    ///
    ///   Pillar B-1 (Dynamic Icon-Clipping):
    ///     All bounding-box arithmetic is performed in floating-point before casting to int,
    ///     and a dynamic safety padding of Convert.ToInt32(24.0f * scaleFactor) is added to
    ///     the AppList right edge so icon containers are never clipped.
    ///
    ///   Pillar B-2 (DWM Ghost Border):
    ///     After SetWindowRgn is called, StripBorderAndGhost() strips WS_BORDER | WS_THICKFRAME
    ///     via SetWindowLong and sets DWMWA_BORDER_COLOR to DWMWA_COLOR_NONE so Windows 11
    ///     does not draw a thin ghost outline around the clipped region.
    ///
    ///   Pillar B-3 (Clean Exit):
    ///     ResetTaskbar() calls SetWindowRgn(hwnd, IntPtr.Zero, true) and immediately follows
    ///     with SetWindowPos(..., SWP_FRAMECHANGED | SWP_NOACTIVATE | SWP_NOMOVE | SWP_NOSIZE
    ///     | SWP_NOZORDER) to force a full DWM cache flush, preventing frozen grey/blue artifacts.
    /// </summary>
    internal static class TaskbarManager
    {
        // -------------------------------------------------------------------------
        // Taskbar discovery
        // -------------------------------------------------------------------------

        /// <summary>
        /// Enumerates all present taskbars (primary + secondary) and returns their
        /// handles, rects, and DPI scale factors.
        /// </summary>
        public static List<Types.Taskbar> GenerateTaskbarInfo()
        {
            var result = new List<Types.Taskbar>();

            // --- Primary taskbar ---
            IntPtr hwndMain = NativeMethods.FindWindowExA(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null);
            if (hwndMain == IntPtr.Zero) return result; // Explorer not ready yet

            NativeMethods.GetWindowRect(hwndMain, out NativeMethods.RECT rectMain);
            IntPtr hwndTray    = NativeMethods.FindWindowExA(hwndMain, IntPtr.Zero, "TrayNotifyWnd", null);
            NativeMethods.GetWindowRect(hwndTray, out NativeMethods.RECT rectTray);
            IntPtr hwndRebar   = NativeMethods.FindWindowExA(hwndMain, IntPtr.Zero, "ReBarWindow32", null);
            IntPtr hwndAppList = hwndRebar != IntPtr.Zero
                ? NativeMethods.FindWindowExA(hwndRebar, IntPtr.Zero, "MSTaskSwWClass", null)
                : IntPtr.Zero;
            NativeMethods.GetWindowRect(hwndAppList, out NativeMethods.RECT rectAppList);

            result.Add(new Types.Taskbar
            {
                TaskbarHwnd  = hwndMain,
                TrayHwnd     = hwndTray,
                AppListHwnd  = hwndAppList,
                TaskbarRect  = rectMain,
                TrayRect     = rectTray,
                AppListRect  = rectAppList,
                RecoveryHrgn = IntPtr.Zero,
                ScaleFactor  = (double)NativeMethods.GetDpiForWindow(hwndMain) / 96.0,
                TaskbarRes   = $"{rectMain.Right - rectMain.Left} x {rectMain.Bottom - rectMain.Top}",
                Ignored      = false,
            });

            // --- Secondary taskbars (multi-monitor) ---
            IntPtr hwndPrevious = IntPtr.Zero;
            while (true)
            {
                IntPtr hwndSec = NativeMethods.FindWindowExA(
                    IntPtr.Zero, hwndPrevious, "Shell_SecondaryTrayWnd", null);
                hwndPrevious = hwndSec;
                if (hwndSec == IntPtr.Zero) break;

                NativeMethods.GetWindowRect(hwndSec, out NativeMethods.RECT rectSec);

                IntPtr hwndSecTray    = NativeMethods.FindWindowExA(hwndSec, IntPtr.Zero, "TrayNotifyWnd", null);
                NativeMethods.GetWindowRect(hwndSecTray, out NativeMethods.RECT rectSecTray);

                IntPtr hwndWorkerW    = NativeMethods.FindWindowExA(hwndSec, IntPtr.Zero, "WorkerW", null);
                IntPtr hwndSecAppList = hwndWorkerW != IntPtr.Zero
                    ? NativeMethods.FindWindowExA(hwndWorkerW, IntPtr.Zero, "MSTaskListWClass", null)
                    : IntPtr.Zero;
                NativeMethods.GetWindowRect(hwndSecAppList, out NativeMethods.RECT rectSecAppList);

                result.Add(new Types.Taskbar
                {
                    TaskbarHwnd  = hwndSec,
                    TrayHwnd     = hwndSecTray,
                    AppListHwnd  = hwndSecAppList,
                    TaskbarRect  = rectSec,
                    TrayRect     = rectSecTray,
                    AppListRect  = rectSecAppList,
                    RecoveryHrgn = IntPtr.Zero,
                    ScaleFactor  = (double)NativeMethods.GetDpiForWindow(hwndSec) / 96.0,
                    TaskbarRes   = $"{rectSec.Right - rectSec.Left} x {rectSec.Bottom - rectSec.Top}",
                    Ignored      = false,
                });
            }

            return result;
        }

        // -------------------------------------------------------------------------
        // Quick rect refresh (called in the background loop)
        // -------------------------------------------------------------------------

        public static Types.Taskbar GetQuickTaskbarRects(
            IntPtr taskbarHwnd, IntPtr trayHwnd, IntPtr appListHwnd)
        {
            NativeMethods.GetWindowRect(taskbarHwnd, out NativeMethods.RECT tbRect);
            NativeMethods.GetWindowRect(trayHwnd,    out NativeMethods.RECT trayRect);
            NativeMethods.GetWindowRect(appListHwnd, out NativeMethods.RECT alRect);
            return new Types.Taskbar
            {
                TaskbarHwnd = taskbarHwnd,
                TrayHwnd    = trayHwnd,
                AppListHwnd = appListHwnd,
                TaskbarRect = tbRect,
                TrayRect    = trayRect,
                AppListRect = alRect,
            };
        }

        // -------------------------------------------------------------------------
        // Pillar B-3: Clean Reset / Exit
        // -------------------------------------------------------------------------

        /// <summary>
        /// Removes the custom window region and forces a full DWM cache flush.
        /// Must be called on exit and when the taskbar should be "filled".
        /// </summary>
        public static void ResetTaskbar(Types.Taskbar taskbar, Types.Settings settings)
        {
            IntPtr hwnd = taskbar.TaskbarHwnd;

            // Step 1 — remove the clipping region
            NativeMethods.SetWindowRgn(hwnd, IntPtr.Zero, true);

            // Step 2 — force DWM to flush its cached non-client geometry (Pillar B-3)
            NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                NativeMethods.SetWindowPosFlags.FrameChanged      |
                NativeMethods.SetWindowPosFlags.DoNotActivate     |
                NativeMethods.SetWindowPosFlags.IgnoreMove        |
                NativeMethods.SetWindowPosFlags.IgnoreResize      |
                NativeMethods.SetWindowPosFlags.IgnoreZOrder);

            if (settings.CompositionCompat)
                Interaction.UpdateTranslucentTB(hwnd);
        }

        // -------------------------------------------------------------------------
        // Pillar B-2: Strip DWM ghost border
        // -------------------------------------------------------------------------

        /// <summary>
        /// Strips WS_BORDER and WS_THICKFRAME from the taskbar window and sets
        /// DWMWA_BORDER_COLOR to transparent so Windows 11 does not draw a ghost
        /// outline around the clipped region.
        /// </summary>
        private static void StripBorderAndGhost(IntPtr hwnd)
        {
            // Strip border styles
            uint style = NativeMethods.GetWindowLong(hwnd, NativeMethods.GWL_STYLE);
            style &= ~(NativeMethods.WS_BORDER | NativeMethods.WS_THICKFRAME);
            NativeMethods.SetWindowLong(hwnd, NativeMethods.GWL_STYLE, style);

            // Tell DWM not to draw any border colour (transparent / colour-none sentinel)
            uint colourNone = NativeMethods.DWMWA_COLOR_NONE;
            NativeMethods.DwmSetWindowAttribute(
                hwnd,
                NativeMethods.DWMWINDOWATTRIBUTE.DWMWA_BORDER_COLOR,
                ref colourNone,
                sizeof(uint));
        }

        // -------------------------------------------------------------------------
        // Bug 2 fix: Force DWM + desktop repaint after every region change
        // -------------------------------------------------------------------------

        /// <summary>
        /// After SetWindowRgn Windows 10 often "forgets" to repaint the desktop
        /// behind the newly transparent corners. This forces it by:
        ///   1. SWP_FRAMECHANGED  — tells DWM the non-client geometry changed.
        ///   2. RedrawWindow      — invalidates + force-updates the taskbar and children.
        /// This replicates what the Win-key press (WM_THEMECHANGED broadcast) triggers.
        /// </summary>
        private static void ForceRedrawAfterRegion(IntPtr hwnd)
        {
            NativeMethods.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                NativeMethods.SetWindowPosFlags.FrameChanged  |
                NativeMethods.SetWindowPosFlags.DoNotActivate |
                NativeMethods.SetWindowPosFlags.IgnoreMove    |
                NativeMethods.SetWindowPosFlags.IgnoreResize  |
                NativeMethods.SetWindowPosFlags.IgnoreZOrder);

            NativeMethods.RedrawWindow(hwnd, IntPtr.Zero, IntPtr.Zero,
                NativeMethods.RedrawWindowFlags.Invalidate  |
                NativeMethods.RedrawWindowFlags.UpdateNow   |
                NativeMethods.RedrawWindowFlags.AllChildren |
                NativeMethods.RedrawWindowFlags.Frame);
        }

        // -------------------------------------------------------------------------
        // Simple (non-dynamic) taskbar update
        // -------------------------------------------------------------------------

        /// <summary>
        /// Applies a static rounded-rectangle region to the taskbar.
        /// </summary>
        public static bool UpdateSimpleTaskbar(Types.Taskbar taskbar, Types.Settings settings)
        {
            try
            {
                // Snapshot to avoid mutating the shared ActiveSettings from a background thread
                Types.Settings s = SnapshotSettings(settings);
                double sf = taskbar.ScaleFactor;

                var region = new Types.EffectiveRegion
                {
                    CornerRadius = Convert.ToInt32(s.CornerRadius * sf),
                    Top          = Convert.ToInt32(s.MarginTop    * sf),
                    Left         = Convert.ToInt32(s.MarginLeft   * sf),
                    // Pillar B-1: floating-point arithmetic before int cast
                    Width  = Convert.ToInt32((taskbar.TaskbarRect.Right  - taskbar.TaskbarRect.Left)
                                             - (s.MarginRight * sf)) + 1,
                    Height = Convert.ToInt32((taskbar.TaskbarRect.Bottom - taskbar.TaskbarRect.Top)
                                             - (s.MarginBottom * sf)) + 1,
                };

                IntPtr hrgn = NativeMethods.CreateRoundRectRgn(
                    region.Left, region.Top, region.Width, region.Height,
                    region.CornerRadius, region.CornerRadius);

                NativeMethods.SetWindowRgn(taskbar.TaskbarHwnd, hrgn, true);

                // Pillar B-2: strip ghost border
                StripBorderAndGhost(taskbar.TaskbarHwnd);

                // Bug 2 fix: force DWM cache flush + desktop repaint
                ForceRedrawAfterRegion(taskbar.TaskbarHwnd);

                if (s.CompositionCompat)
                    Interaction.UpdateTranslucentTB(taskbar.TaskbarHwnd);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateSimpleTaskbar failed: {ex.Message}");
                return false;
            }
        }

        // -------------------------------------------------------------------------
        // Dynamic taskbar update (shrinks to fit open apps)
        // -------------------------------------------------------------------------

        /// <summary>
        /// Applies a dynamic rounded-rectangle region that clips the taskbar to the
        /// width of active/pinned icons, with an optional separate tray area.
        ///
        /// Pillar B-1 fix: all pixel math is float-first; a 24 logical-pixel safety
        /// pad (scaled) is added to the AppList right edge to prevent icon clipping
        /// on Windows 11 centred taskbars.
        /// </summary>
        public static bool UpdateDynamicTaskbar(Types.Taskbar taskbar, Types.Settings settings)
        {
            try
            {
                // Snapshot to avoid mutating the shared ActiveSettings from the background thread
                Types.Settings s = SnapshotSettings(settings);
                double sf = taskbar.ScaleFactor;

                // ── All geometry in double; one Convert.ToInt32() per coordinate at the end ──
                //
                // Safety padding: 48 logical pixels × DPI scale, kept as float then widened.
                // 48 lp gives the rounded corner enough room at all standard DPI settings.
                double iconSafetyPad = (double)(48.0f * (float)sf);

                double cornerRadius = s.CornerRadius * sf;
                double marginTop    = s.MarginTop    * sf;
                double marginLeft   = s.MarginLeft   * sf;
                double marginRight  = s.MarginRight  * sf;
                double marginBottom = s.MarginBottom * sf;

                // Taskbar dimensions in physical pixels (already integers, no loss)
                double tbWidth  = taskbar.TaskbarRect.Right  - taskbar.TaskbarRect.Left;
                double tbHeight = taskbar.TaskbarRect.Bottom - taskbar.TaskbarRect.Top;

                // ── Convert TrayNotifyWnd rect to window-relative coordinates ──────────────
                // GetWindowRect returns screen-absolute positions. Subtract the taskbar
                // origin to get the position within the taskbar window's client/region space.
                // If TrayHwnd is zero the rect will be all-zeros, giving trayRelLeft = 0
                // which we guard against below.
                double trayRelLeft  = taskbar.TrayRect.Left   - taskbar.TaskbarRect.Left;
                double trayRelRight = taskbar.TrayRect.Right  - taskbar.TaskbarRect.Left;

                // ── centredDistanceFromEdge ───────────────────────────────────────────────
                // How many physical pixels to trim from the right edge of the app-icon pill.
                // Kept entirely in double to preserve fractional DPI components.
                double centredDist = (taskbar.TaskbarRect.Right - taskbar.AppListRect.Right)
                                     - (2.0 * sf);
                if (!s.IsWindows11)
                    centredDist -= 20.0 * sf; // Win10: subtract drag-handle space

                // ── Guard: never let the app-list pill clip into the tray area ─────────────
                // On secondary monitors (Shell_SecondaryTrayWnd) the tray contains the clock.
                // If trimming centredDist would push mainRight past the tray's left edge,
                // clamp it so the region always includes the full tray area.
                // trayRelLeft == 0 means no valid tray was found; skip the guard in that case.
                double clampedMainRight;
                if (trayRelLeft > 1.0)
                {
                    // The right edge of the app-list pill must not overlap the tray left edge.
                    // Add iconSafetyPad but never exceed the tray boundary.
                    double unclamped = (tbWidth - marginRight) - centredDist + iconSafetyPad;
                    clampedMainRight = Math.Min(unclamped, trayRelLeft - marginLeft);
                    // If the clamped value would be smaller than the safety minimum
                    // (e.g. only one app open on a wide monitor), keep the unclamped value —
                    // the user chose Dynamic Mode knowingly and the tray has ShowTray toggle.
                    if (clampedMainRight < marginLeft + iconSafetyPad)
                        clampedMainRight = unclamped;
                }
                else
                {
                    // No valid tray rect — use unclamped value
                    clampedMainRight = (tbWidth - marginRight) - centredDist + iconSafetyPad;
                }

                // ── Build main (app-list) region ─────────────────────────────────────────
                double mainTop    = marginTop;
                double mainBottom = tbHeight - marginBottom;
                double mainLeft, mainRight;

                if (s.IsCentred)
                {
                    mainLeft  = centredDist + (marginRight - 1.0);
                    mainRight = clampedMainRight;
                }
                else
                {
                    mainLeft  = marginLeft;
                    mainRight = clampedMainRight;
                }

                IntPtr mainRegion = NativeMethods.CreateRoundRectRgn(
                    Convert.ToInt32(mainLeft),
                    Convert.ToInt32(mainTop),
                    Convert.ToInt32(mainRight),
                    Convert.ToInt32(mainBottom),
                    Convert.ToInt32(cornerRadius),
                    Convert.ToInt32(cornerRadius));

                IntPtr finalRegion = mainRegion;

                // ── Tray pill (optional, window-relative coordinates) ─────────────────────
                if (s.ShowTray && taskbar.TrayHwnd != IntPtr.Zero && trayRelLeft > 1.0)
                {
                    // trayRelLeft / trayRelRight are already window-relative.
                    // Add a 1×sf inset on the left so the rounded corner sits just inside.
                    double trayPillLeft  = trayRelLeft  - sf;
                    double trayPillTop   = marginTop;
                    double trayPillRight = trayRelRight + marginRight;   // include right margin
                    double trayPillBot   = tbHeight - marginBottom;

                    // Clamp to taskbar width
                    trayPillRight = Math.Min(trayPillRight, tbWidth - marginRight);

                    IntPtr trayHrgn = NativeMethods.CreateRoundRectRgn(
                        Convert.ToInt32(trayPillLeft),
                        Convert.ToInt32(trayPillTop),
                        Convert.ToInt32(trayPillRight),
                        Convert.ToInt32(trayPillBot),
                        Convert.ToInt32(cornerRadius),
                        Convert.ToInt32(cornerRadius));

                    IntPtr combined = NativeMethods.CreateRectRgn(0, 0, 0, 0);
                    NativeMethods.CombineRgn(combined, trayHrgn, mainRegion, 2 /* RGN_OR */);
                    NativeMethods.DeleteObject(trayHrgn);
                    NativeMethods.DeleteObject(mainRegion);
                    finalRegion = combined;
                }

                NativeMethods.SetWindowRgn(taskbar.TaskbarHwnd, finalRegion, true);

                // Pillar B-2: strip ghost border
                StripBorderAndGhost(taskbar.TaskbarHwnd);

                // Force DWM cache flush + desktop repaint
                ForceRedrawAfterRegion(taskbar.TaskbarHwnd);

                if (s.CompositionCompat)
                    Interaction.UpdateTranslucentTB(taskbar.TaskbarHwnd);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateDynamicTaskbar failed: {ex.Message}");
                return false;
            }
        }

        // -------------------------------------------------------------------------
        // Fill / maximised-window detection
        // -------------------------------------------------------------------------

        public static bool TaskbarShouldBeFilled(IntPtr taskbarHwnd, Types.Settings settings)
        {
            if (!settings.FillOnMaximise) return false;

            // Windows 11: check for Task Switcher / Alt+Tab overlay
            if (settings.FillOnTaskSwitch)
            {
                IntPtr topHwnd = NativeMethods.WindowFromPoint(new NativeMethods.POINT { x = 0, y = 0 });
                var sb = new StringBuilder(1024);
                NativeMethods.GetClassName(topHwnd, sb, 1024);
                if (sb.ToString() == "XamlExplorerHostIslandWindow") return true;
            }

            // Check for a maximised window on the same monitor
            foreach (IntPtr wHwnd in Interaction.GetTopLevelWindows())
            {
                if (!NativeMethods.IsWindowVisible(wHwnd)) continue;
                if (NativeMethods.MonitorFromWindow(taskbarHwnd, 2) !=
                    NativeMethods.MonitorFromWindow(wHwnd, 2)) continue;

                NativeMethods.DwmGetWindowAttribute(wHwnd,
                    NativeMethods.DWMWINDOWATTRIBUTE.Cloaked, out bool isCloaked, 4);
                if (isCloaked) continue;

                var placement = NativeMethods.WINDOWPLACEMENT.Default;
                NativeMethods.GetWindowPlacement(wHwnd, ref placement);
                if (placement.ShowCmd == NativeMethods.ShowWindowCommands.ShowMaximized)
                    return true;
            }
            return false;
        }

        // -------------------------------------------------------------------------
        // Change detection
        // -------------------------------------------------------------------------

        public static bool TaskbarRefreshRequired(
            Types.Taskbar current, Types.Taskbar latest, bool isDynamic)
        {
            bool tbChanged  = !RectsEqual(current.TaskbarRect,  latest.TaskbarRect);
            bool alChanged  = !RectsEqual(current.AppListRect,  latest.AppListRect);
            bool tryChanged = !RectsEqual(current.TrayRect,     latest.TrayRect);

            return isDynamic
                ? tbChanged || alChanged || tryChanged
                : tbChanged;
        }

        public static bool TaskbarCountOrHandleChanged(int knownCount, IntPtr mainHandle)
        {
            var current = new List<IntPtr>();
            IntPtr hwndPrev = IntPtr.Zero;

            IntPtr hwndMain = NativeMethods.FindWindowExA(IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null);
            if (hwndMain == IntPtr.Zero)   return false;
            if (hwndMain != mainHandle)    return true;
            current.Add(hwndMain);

            while (true)
            {
                IntPtr hwndSec = NativeMethods.FindWindowExA(
                    IntPtr.Zero, hwndPrev, "Shell_SecondaryTrayWnd", null);
                hwndPrev = hwndSec;
                if (hwndSec == IntPtr.Zero) break;
                current.Add(hwndSec);
            }

            return current.Count != knownCount;
        }

        public static bool CheckDynamicUpdateIsValid(Types.Taskbar current, Types.Taskbar latest)
        {
            if (current == null || latest == null)                   return false;
            if (current.TaskbarHwnd != latest.TaskbarHwnd)           return false;

            int newW     = latest.AppListRect.Right  - latest.AppListRect.Left;
            int tbWidth  = latest.TaskbarRect.Right  - latest.TaskbarRect.Left;

            if (latest.AppListRect.Right >= latest.TrayRect.Left && latest.TrayRect.Left != 0)
                return false;
            if (newW == latest.TrayRect.Left && latest.TrayRect.Left != 0)
                return false;
            if (newW <= 20 * current.ScaleFactor && newW != 0)
                return false;
            if (newW >= tbWidth && newW != 0)
                return false;

            return true;
        }

        // -------------------------------------------------------------------------
        // Registry: centred taskbar detection
        // -------------------------------------------------------------------------

        public static bool CheckIfCentred()
        {
            try
            {
                using RegistryKey? key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced");
                if (key?.GetValue("TaskbarAl") is int val)
                    return val == 1;
            }
            catch { }
            return false;
        }

        // -------------------------------------------------------------------------
        // Helpers
        // -------------------------------------------------------------------------

        private static bool RectsEqual(NativeMethods.RECT a, NativeMethods.RECT b)
            => a.Left == b.Left && a.Top == b.Top && a.Right == b.Right && a.Bottom == b.Bottom;

        /// <summary>
        /// When MarginBasic is not in "advanced" mode (-384), copies it to all four per-side
        /// margin fields on the provided Settings instance. The caller is responsible for
        /// passing either the live settings or a snapshot; this method mutates what it receives.
        /// </summary>
        private static void ExpandBasicMargins(Types.Settings s)
        {
            if (s.MarginBasic != -384)
            {
                s.MarginLeft   = s.MarginBasic;
                s.MarginTop    = s.MarginBasic;
                s.MarginRight  = s.MarginBasic;
                s.MarginBottom = s.MarginBasic;
            }
        }

        /// <summary>
        /// Returns a shallow copy of the given Settings with MarginBasic already
        /// expanded to all four per-side fields. Calling this instead of
        /// ExpandBasicMargins directly avoids mutating the shared ActiveSettings
        /// object from the background thread.
        /// </summary>
        private static Types.Settings SnapshotSettings(Types.Settings s)
        {
            var snap = new Types.Settings
            {
                Version          = s.Version,
                CornerRadius     = s.CornerRadius,
                MarginBasic      = s.MarginBasic,
                MarginBottom     = s.MarginBottom,
                MarginLeft       = s.MarginLeft,
                MarginRight      = s.MarginRight,
                MarginTop        = s.MarginTop,
                IsDynamic        = s.IsDynamic,
                IsCentred        = s.IsCentred,
                IsWindows11      = s.IsWindows11,
                ShowTray         = s.ShowTray,
                CompositionCompat = s.CompositionCompat,
                IsNotFirstLaunch  = s.IsNotFirstLaunch,
                FillOnMaximise    = s.FillOnMaximise,
                FillOnTaskSwitch  = s.FillOnTaskSwitch,
                ShowTrayOnHover   = s.ShowTrayOnHover,
                AutoHideMode      = s.AutoHideMode,
            };
            ExpandBasicMargins(snap);
            return snap;
        }
    }
}
