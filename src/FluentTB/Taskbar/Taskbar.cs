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
    /// Bugfixes integrated from TaskbarManager.cs (old FluentTB code):
    ///
    ///   Pillar B-1 (Dynamic Icon-Clipping):
    ///     All bounding-box arithmetic uses double before a final Convert.ToInt32(),
    ///     and a 48 logical-pixel safety pad (DPI-scaled) is added to the AppList
    ///     right edge so icon containers are never clipped.
    ///
    ///   Pillar B-2 (DWM Ghost Border):
    ///     After SetWindowRgn, StripBorderAndGhost() strips WS_BORDER | WS_THICKFRAME
    ///     and sets DWMWA_BORDER_COLOR = DWMWA_COLOR_NONE so Win11 doesn't draw a
    ///     ghost outline around the clipped region.
    ///
    ///   Pillar B-3 (Clean Exit / Reset):
    ///     ResetTaskbar() follows SetWindowRgn with SetWindowPos(SWP_FRAMECHANGED)
    ///     to force a full DWM cache flush, preventing frozen grey/blue artifacts.
    ///
    ///   GDI Leak Fix:
    ///     All temporary region handles (trayHrgn, mainRegion before combining) are
    ///     freed with DeleteObject after CombineRgn. The final region is owned by
    ///     the window after SetWindowRgn and must NOT be freed by the caller.
    ///
    ///   SnapshotSettings:
    ///     A shallow copy of Settings is taken at the start of each update call so
    ///     the background thread never mutates the shared ActiveSettings object.
    /// </summary>
    static class Taskbar
    {
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Taskbar discovery
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public static List<Types.Taskbar> GenerateTaskbarInfo()
        {
            var list = new List<Types.Taskbar>();

            IntPtr hwndMain = LocalPInvoke.FindWindowExA(
                IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null);
            if (hwndMain == IntPtr.Zero) return list; // Explorer not ready

            LocalPInvoke.GetWindowRect(hwndMain, out LocalPInvoke.RECT rectMain);

            IntPtr hwndTray = LocalPInvoke.FindWindowExA(
                hwndMain, IntPtr.Zero, "TrayNotifyWnd", null);
            LocalPInvoke.GetWindowRect(hwndTray, out LocalPInvoke.RECT rectTray);

            IntPtr hwndRebar = LocalPInvoke.FindWindowExA(
                hwndMain, IntPtr.Zero, "ReBarWindow32", null);
            IntPtr hwndAppList = hwndRebar != IntPtr.Zero
                ? LocalPInvoke.FindWindowExA(hwndRebar, IntPtr.Zero, "MSTaskSwWClass", null)
                : IntPtr.Zero;
            LocalPInvoke.GetWindowRect(hwndAppList, out LocalPInvoke.RECT rectAppList);
            rectAppList = GetVisibleAppButtonBounds(hwndAppList, rectAppList, rectMain);

            IntPtr hwndClock = LocalPInvoke.FindWindowExA(hwndTray, IntPtr.Zero, "TrayClockWClass", null);
            LocalPInvoke.GetWindowRect(hwndClock, out LocalPInvoke.RECT rectClock);

            // Widgets button is usually the first TrayButton or similar on the left in Win11
            IntPtr hwndWidgets = LocalPInvoke.FindWindowExA(hwndMain, IntPtr.Zero, "TrayButton", null);
            LocalPInvoke.GetWindowRect(hwndWidgets, out LocalPInvoke.RECT rectWidgets);

            list.Add(new Types.Taskbar
            {
                TaskbarHwnd  = hwndMain,
                TrayHwnd     = hwndTray,
                AppListHwnd  = hwndAppList,
                TaskbarRect  = rectMain,
                TrayRect     = rectTray,
                AppListRect  = rectAppList,
                ClockHwnd    = hwndClock,
                WidgetsHwnd  = hwndWidgets,
                ClockRect    = rectClock,
                WidgetsRect  = rectWidgets,
                RecoveryHrgn = IntPtr.Zero,
                ScaleFactor  = (double)LocalPInvoke.GetDpiForWindow(hwndMain) / 96.0,
                TaskbarRes   = $"{rectMain.Right - rectMain.Left} x {rectMain.Bottom - rectMain.Top}",
                Ignored      = false
            });

            IntPtr hwndPrev = IntPtr.Zero;
            while (true)
            {
                IntPtr hwndSec = LocalPInvoke.FindWindowExA(
                    IntPtr.Zero, hwndPrev, "Shell_SecondaryTrayWnd", null);
                if (hwndSec == IntPtr.Zero) break;
                hwndPrev = hwndSec;

                LocalPInvoke.GetWindowRect(hwndSec, out LocalPInvoke.RECT rectSec);

                IntPtr hwndSecTray = LocalPInvoke.FindWindowExA(
                    hwndSec, IntPtr.Zero, "TrayNotifyWnd", null);
                LocalPInvoke.GetWindowRect(hwndSecTray, out LocalPInvoke.RECT rectSecTray);

                IntPtr hwndWorkerW = LocalPInvoke.FindWindowExA(
                    hwndSec, IntPtr.Zero, "WorkerW", null);
                IntPtr hwndSecAppList = hwndWorkerW != IntPtr.Zero
                    ? LocalPInvoke.FindWindowExA(hwndWorkerW, IntPtr.Zero, "MSTaskListWClass", null)
                    : IntPtr.Zero;
                LocalPInvoke.GetWindowRect(hwndSecAppList, out LocalPInvoke.RECT rectSecAppList);
                rectSecAppList = GetVisibleAppButtonBounds(hwndSecAppList, rectSecAppList, rectSec);

                IntPtr hwndSecClock = LocalPInvoke.FindWindowExA(hwndSecTray, IntPtr.Zero, "TrayClockWClass", null);
                LocalPInvoke.GetWindowRect(hwndSecClock, out LocalPInvoke.RECT rectSecClock);

                list.Add(new Types.Taskbar
                {
                    TaskbarHwnd  = hwndSec,
                    TrayHwnd     = hwndSecTray,
                    AppListHwnd  = hwndSecAppList,
                    TaskbarRect  = rectSec,
                    TrayRect     = rectSecTray,
                    AppListRect  = rectSecAppList,
                    ClockHwnd    = hwndSecClock,
                    WidgetsHwnd  = IntPtr.Zero,
                    ClockRect    = rectSecClock,
                    WidgetsRect  = new LocalPInvoke.RECT(),
                    // FluentTB owns and removes its own region during shutdown.
                    // Do not cache a foreign HRGN here: the old code then skipped
                    // every update for that secondary taskbar.
                    RecoveryHrgn = IntPtr.Zero,
                    ScaleFactor  = (double)LocalPInvoke.GetDpiForWindow(hwndSec) / 96.0,
                    TaskbarRes   = $"{rectSec.Right - rectSec.Left} x {rectSec.Bottom - rectSec.Top}",
                    Ignored      = false
                });
            }

            foreach (Types.Taskbar taskbar in list)
            {
                var fresh = GetQuickTaskbarRects(taskbar.TaskbarHwnd, taskbar.TrayHwnd, taskbar.AppListHwnd);
                taskbar.AppListRect = fresh.AppListRect;
                taskbar.TrayRect = fresh.TrayRect;
                taskbar.WidgetsRect = fresh.WidgetsRect;
            }
            return list;
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Quick rect snapshot
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public static Types.Taskbar GetQuickTaskbarRects(
            IntPtr taskbarHwnd, IntPtr trayHwnd, IntPtr appListHwnd)
        {
            LocalPInvoke.GetWindowRect(taskbarHwnd, out LocalPInvoke.RECT tbRect);
            LocalPInvoke.GetWindowRect(trayHwnd,    out LocalPInvoke.RECT trayRect);
            LocalPInvoke.GetWindowRect(appListHwnd, out LocalPInvoke.RECT appRect);
            var widgetsRect = new LocalPInvoke.RECT();
            if (LocalPInvoke.FindWindowExA(taskbarHwnd, IntPtr.Zero,
                "Windows.UI.Composition.DesktopWindowContentBridge", null) != IntPtr.Zero)
            {
                var content = TaskbarContentReader.Read(taskbarHwnd, tbRect);
                // Empty on failure: CanUseDynamicTaskbar chooses the full region.
                // Never fall back to the misleading legacy MSTask* width here.
                appRect = content.Apps;
                trayRect = content.Tray;
                widgetsRect = content.Widgets;
            }
            else
            {
                appRect = GetVisibleAppButtonBounds(appListHwnd, appRect, tbRect);
                IntPtr widgetsHwnd = LocalPInvoke.FindWindowExA(taskbarHwnd, IntPtr.Zero, "TrayButton", null);
                LocalPInvoke.GetWindowRect(widgetsHwnd, out widgetsRect);
            }

            return new Types.Taskbar
            {
                TaskbarHwnd = taskbarHwnd,
                TrayHwnd    = trayHwnd,
                AppListHwnd = appListHwnd,
                TaskbarRect = tbRect,
                TrayRect    = trayRect,
                AppListRect = appRect,
                WidgetsRect = widgetsRect,
                ScaleFactor = (double)LocalPInvoke.GetDpiForWindow(taskbarHwnd) / 96.0
            };
        }

        private static LocalPInvoke.RECT GetVisibleAppButtonBounds(
            IntPtr appListHwnd, LocalPInvoke.RECT fallback, LocalPInvoke.RECT taskbarRect)
        {
            if (appListHwnd == IntPtr.Zero)
                return fallback;

            var state = new AppButtonBoundsState
            {
                Fallback = fallback,
                TaskbarRect = taskbarRect,
                MinLeft = int.MaxValue,
                MinTop = int.MaxValue,
                MaxRight = int.MinValue,
                MaxBottom = int.MinValue
            };

            GCHandle handle = GCHandle.Alloc(state);
            try
            {
                LocalPInvoke.EnumChildWindows(
                    appListHwnd,
                    CollectAppButtonBounds,
                    GCHandle.ToIntPtr(handle));
            }
            finally
            {
                if (handle.IsAllocated)
                    handle.Free();
            }

            if (!state.Found)
                return fallback;

            return new LocalPInvoke.RECT
            {
                Left = state.MinLeft,
                Top = state.MinTop,
                Right = state.MaxRight,
                Bottom = state.MaxBottom
            };
        }

        private static bool CollectAppButtonBounds(IntPtr hwnd, IntPtr lParam)
        {
            var state = (AppButtonBoundsState)GCHandle.FromIntPtr(lParam).Target;
            if (!LocalPInvoke.IsWindowVisible(hwnd))
                return true;

            if (!LocalPInvoke.GetWindowRect(hwnd, out LocalPInvoke.RECT rect))
                return true;

            int width = rect.Right - rect.Left;
            int height = rect.Bottom - rect.Top;
            int taskbarHeight = Math.Max(1, state.TaskbarRect.Bottom - state.TaskbarRect.Top);
            int fallbackWidth = Math.Max(1, state.Fallback.Right - state.Fallback.Left);

            if (width < 8 || height < 8)
                return true;

            // Skip children that are nearly as wide as the entire AppList container —
            // these are container/overlay elements, not individual icon buttons.
            // Use a 10% threshold instead of the old hard-coded 2px margin.
            if (width >= fallbackWidth * 0.90)
                return true;

            if (height > taskbarHeight + 8)
                return true;

            if (rect.Right <= state.Fallback.Left || rect.Left >= state.Fallback.Right)
                return true;

            state.Found = true;
            state.MinLeft = Math.Min(state.MinLeft, Math.Max(rect.Left, state.Fallback.Left));
            state.MinTop = Math.Min(state.MinTop, Math.Max(rect.Top, state.Fallback.Top));
            state.MaxRight = Math.Max(state.MaxRight, Math.Min(rect.Right, state.Fallback.Right));
            state.MaxBottom = Math.Max(state.MaxBottom, Math.Min(rect.Bottom, state.Fallback.Bottom));
            return true;
        }

        private sealed class AppButtonBoundsState
        {
            public LocalPInvoke.RECT Fallback;
            public LocalPInvoke.RECT TaskbarRect;
            public bool Found;
            public int MinLeft;
            public int MinTop;
            public int MaxRight;
            public int MaxBottom;
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Change detection
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public static bool TaskbarCountOrHandleChanged(int knownCount, IntPtr knownMain)
        {
            int count = 0;
            IntPtr hwndPrev = IntPtr.Zero;

            IntPtr hwndMain = LocalPInvoke.FindWindowExA(
                IntPtr.Zero, IntPtr.Zero, "Shell_TrayWnd", null);
            if (hwndMain == IntPtr.Zero) return false;
            if (hwndMain != knownMain)  return true;
            count++;

            while (true)
            {
                IntPtr hwndSec = LocalPInvoke.FindWindowExA(
                    IntPtr.Zero, hwndPrev, "Shell_SecondaryTrayWnd", null);
                if (hwndSec == IntPtr.Zero) break;
                hwndPrev = hwndSec;
                count++;
            }

            return count != knownCount;
        }

        public static bool TaskbarRefreshRequired(
            Types.Taskbar current, Types.Taskbar next, bool isDynamic)
        {
            bool tbChanged   = !RectsEqual(current.TaskbarRect, next.TaskbarRect);
            bool appChanged  = !RectsEqual(current.AppListRect, next.AppListRect);
            bool trayChanged = !RectsEqual(current.TrayRect,    next.TrayRect);
            bool widgetsChanged = !RectsEqual(current.WidgetsRect, next.WidgetsRect);

            return isDynamic
                ? tbChanged || appChanged || trayChanged || widgetsChanged
                : tbChanged;
        }

        private static bool RectsEqual(LocalPInvoke.RECT a, LocalPInvoke.RECT b)
            => a.Left == b.Left && a.Top == b.Top &&
               a.Right == b.Right && a.Bottom == b.Bottom;

        /// <summary>
        /// Dynamic mode changes the horizontal extent of a taskbar and is therefore
        /// only safe for a horizontal taskbar (top or bottom of a monitor).
        /// </summary>
        public static bool IsHorizontal(Types.Taskbar taskbar)
        {
            if (taskbar == null)
                return false;

            int width = taskbar.TaskbarRect.Right - taskbar.TaskbarRect.Left;
            int height = taskbar.TaskbarRect.Bottom - taskbar.TaskbarRect.Top;
            return width > 0 && height > 0 && width > height;
        }

        /// <summary>
        /// Returns whether Explorer has supplied a usable geometry snapshot for a
        /// dynamic region. Callers fall back to the safe full taskbar region when
        /// this returns false.
        /// </summary>
        public static bool CanUseDynamicTaskbar(Types.Taskbar taskbar)
        {
            if (!IsHorizontal(taskbar))
                return false;

            bool trayVisible = HasTrayBounds(taskbar);
            int gap = taskbar.TrayRect.Left - taskbar.AppListRect.Right;
            if (trayVisible && gap > 0 && gap <= taskbar.ScaleFactor * 25)
                return false;

            return CheckDynamicUpdateIsValid(taskbar, taskbar);
        }

        public static bool CheckDynamicUpdateIsValid(
            Types.Taskbar current, Types.Taskbar next)
        {
            if (current == null || next == null) return false;
            if (current.TaskbarHwnd != next.TaskbarHwnd) return false;
            if (!IsHorizontal(next)) return false;

            int newW    = next.AppListRect.Right - next.AppListRect.Left;
            int tbWidth = next.TaskbarRect.Right - next.TaskbarRect.Left;

            if (tbWidth <= 0) return false;

            // An empty rectangle means there is no tray geometry. A real tray
            // may start at screen coordinate zero on a secondary monitor.
            bool trayVisible = HasTrayBounds(next);
            if (trayVisible && next.AppListRect.Right >= next.TrayRect.Left) return false;


            // Explorer can transiently report an empty AppList while it is
            // rebuilding after startup or an alignment switch. Applying a region
            // from that empty rectangle causes the classic one-sided/stretched pill.
            if (newW <= 20 * current.ScaleFactor) return false;

            // AppList must not fill the entire taskbar width (would be simple mode).
            // Allow up to 85% before treating it as simple — on a left-aligned taskbar
            // the MSTaskSwWClass rect can be close to full width even when icons are few.
            if (newW >= tbWidth * 0.85) return false;

            return true;
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Pillar B-3: Clean Reset
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        /// <summary>
        /// Removes the custom region and forces a full DWM cache flush via
        /// SetWindowPos(SWP_FRAMECHANGED) â€” prevents frozen grey/blue artifacts.
        /// </summary>
        public static void ResetTaskbar(Types.Taskbar taskbar, Types.Settings settings)
        {
            IntPtr hwnd = taskbar.TaskbarHwnd;

            LocalPInvoke.SetWindowRgn(hwnd, IntPtr.Zero, true);

            // Pillar B-3: SWP_FRAMECHANGED â€” DWM Non-Client-Geometry-Cache leeren
            LocalPInvoke.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                LocalPInvoke.SetWindowPosFlags.FrameChanged      |
                LocalPInvoke.SetWindowPosFlags.DoNotActivate     |
                LocalPInvoke.SetWindowPosFlags.IgnoreMove        |
                LocalPInvoke.SetWindowPosFlags.IgnoreResize      |
                LocalPInvoke.SetWindowPosFlags.IgnoreZOrder);

            if (settings.CompositionCompat)
                Interaction.UpdateTranslucentTB(hwnd);
        }


        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Simple taskbar update
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        // In der UpdateSimpleTaskbar oder UpdateDynamicTaskbar Methode
        public static bool UpdateSimpleTaskbar(Types.Taskbar tb, Types.Settings s,
                                            bool forceAccentReset = false)
        {
            try
            {
                Types.Settings snap = SnapshotSettings(s);
                double sf = tb.ScaleFactor;

                // #region agent log
                DebugLog.Write("Taskbar.cs:UpdateSimpleTaskbar", "simple update called",
                    new { snap.IsCentred, snap.IsDynamic, HasRecoveryRgn = tb.RecoveryHrgn != IntPtr.Zero },
                    "A");
                // #endregion

                int left   = Convert.ToInt32(snap.MarginLeft   * sf);
                int top    = Convert.ToInt32(snap.MarginTop    * sf);
                int right  = Convert.ToInt32((tb.TaskbarRect.Right  - tb.TaskbarRect.Left) - snap.MarginRight  * sf) + 1;
                int bottom = Convert.ToInt32((tb.TaskbarRect.Bottom - tb.TaskbarRect.Top)  - snap.MarginBottom * sf) + 1;
                int cr     = Convert.ToInt32(snap.CornerRadius * sf);

                IntPtr rgn = LocalPInvoke.CreateRoundRectRgn(left, top, right, bottom, cr, cr);
                LocalPInvoke.SetWindowRgn(tb.TaskbarHwnd, rgn, true);
                tb.EffectiveRegion = new Types.EffectiveRegion
                {
                    CornerRadius = cr,
                    Top = top,
                    Left = left,
                    Width = Math.Max(0, right - left),
                    Height = Math.Max(0, bottom - top)
                };

                StripBorderAndGhost(tb.TaskbarHwnd);
                
                // Weniger aggressive Redraws
                if (snap.CompositionCompat)
                    Interaction.UpdateTranslucentTB(tb.TaskbarHwnd);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateSimpleTaskbar: {ex.Message}");
                TaskbarDiagnostics.Log($"UpdateSimpleTaskbar error: {ex.Message}");
                return false;
            }
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Dynamic taskbar update  (Pillar B-1 fully applied)
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        // Pure geometry: screen coordinates are converted separately for each monitor.
        internal static bool TryGetDynamicAppBounds(Types.Taskbar tb, Types.Settings settings,
                                                   out double mainLeft, out double mainRight)
        {
            mainLeft = mainRight = 0;
            if (tb == null || settings == null || !IsHorizontal(tb) ||
                tb.ScaleFactor <= 0 || double.IsNaN(tb.ScaleFactor) || double.IsInfinity(tb.ScaleFactor))
                return false;

            Types.Settings snap = SnapshotSettings(settings);
            double sf = tb.ScaleFactor;
            double tbW = (double)tb.TaskbarRect.Right - tb.TaskbarRect.Left;
            double appLeft = (double)tb.AppListRect.Left - tb.TaskbarRect.Left;
            double appRight = (double)tb.AppListRect.Right - tb.TaskbarRect.Left;
            if (appLeft < 0 || appRight > tbW || appRight <= appLeft ||
                tb.AppListRect.Bottom <= tb.AppListRect.Top ||
                tb.AppListRect.Top < tb.TaskbarRect.Top || tb.AppListRect.Bottom > tb.TaskbarRect.Bottom)
                return false;

            double marginLeft = snap.MarginLeft * sf;
            double marginRight = snap.MarginRight * sf;
            double maxRight = HasTrayBounds(tb)
                ? (double)tb.TrayRect.Left - tb.TaskbarRect.Left - sf
                : tbW - marginRight;

            // Explorer's group can move independently of the monitor midpoint.
            // Follow its measured edges; do not mirror them or guess an icon count.
            // UIA already reports the full button containers, including their
            // internal icon padding. An extra 48px would add an empty icon slot.
            mainLeft = snap.IsCentred ? Math.Max(marginLeft, appLeft - Math.Max(0, marginLeft)) : marginLeft;
            mainRight = Math.Min(maxRight, appRight + Math.Max(0, marginRight));
            return mainRight > mainLeft && mainRight >= appRight &&
                (!snap.IsCentred || mainLeft <= appLeft);
        }

        private static bool HasTrayBounds(Types.Taskbar tb)
        {
            return tb.TrayRect.Right > tb.TrayRect.Left &&
                tb.TrayRect.Bottom > tb.TrayRect.Top &&
                tb.TrayRect.Left >= tb.TaskbarRect.Left && tb.TrayRect.Right <= tb.TaskbarRect.Right &&
                tb.TrayRect.Top >= tb.TaskbarRect.Top && tb.TrayRect.Bottom <= tb.TaskbarRect.Bottom;
        }

        public static bool UpdateDynamicTaskbar(Types.Taskbar tb, Types.Settings s,
                                                bool forceAccentReset = false)
        {
            try
            {
                // Reject stale Explorer snapshots before any region arithmetic.
                // The caller will use UpdateSimpleTaskbar as a safe fallback.
                if (!CanUseDynamicTaskbar(tb))
                    return false;

                Types.Settings snap = SnapshotSettings(s);
                double sf = tb.ScaleFactor;

                double cr           = snap.CornerRadius * sf;
                double marginTop    = snap.MarginTop    * sf;
                double marginLeft   = snap.MarginLeft   * sf;
                double marginRight  = snap.MarginRight  * sf;
                double marginBottom = snap.MarginBottom * sf;

                double tbW = tb.TaskbarRect.Right  - tb.TaskbarRect.Left;
                double tbH = tb.TaskbarRect.Bottom - tb.TaskbarRect.Top;

                // Convert tray rect to taskbar-window-relative coordinates
                double trayRelLeft  = tb.TrayRect.Left  - tb.TaskbarRect.Left;
                double trayRelRight = tb.TrayRect.Right - tb.TaskbarRect.Left;

                // Taskbar-relative AppList coords
                double appRelLeft = tb.AppListRect.Left - tb.TaskbarRect.Left;
                double appRelRight = tb.AppListRect.Right - tb.TaskbarRect.Left;

                double mainTop    = marginTop;
                double mainBottom = tbH - marginBottom;
                double mainLeft, mainRight;

                if (!TryGetDynamicAppBounds(tb, snap, out mainLeft, out mainRight))
                    return false;

                // #region agent log
                DebugLog.Write("Taskbar.cs:UpdateDynamicTaskbar", "dynamic geometry",
                    new
                    {
                        snap.IsCentred,
                        appRelLeft,
                        appRelRight,
                        mainLeft,
                        mainRight,
                        trayRelLeft,
                        appPadding = Math.Max(0, marginRight),
                        appListRight = tb.AppListRect.Right,
                        taskbarRight = tb.TaskbarRect.Right
                    },
                    "B");
                // #endregion

                IntPtr mainRegion = LocalPInvoke.CreateRoundRectRgn(
                    Convert.ToInt32(mainLeft),  Convert.ToInt32(mainTop),
                    Convert.ToInt32(mainRight), Convert.ToInt32(mainBottom),
                    Convert.ToInt32(cr),        Convert.ToInt32(cr));

                IntPtr finalRegion = mainRegion;

                if (snap.ShowTray && HasTrayBounds(tb))
                {
                    double trayPillLeft  = trayRelLeft  - sf;
                    double trayPillRight = Math.Min(trayRelRight + marginRight, tbW - marginRight);

                    IntPtr trayRegion = LocalPInvoke.CreateRoundRectRgn(
                        Convert.ToInt32(trayPillLeft),  Convert.ToInt32(mainTop),
                        Convert.ToInt32(trayPillRight), Convert.ToInt32(mainBottom),
                        Convert.ToInt32(cr),            Convert.ToInt32(cr));

                    IntPtr combined = LocalPInvoke.CreateRectRgn(0, 0, 0, 0);
                    LocalPInvoke.CombineRgn(combined, trayRegion, finalRegion, 2 /*RGN_OR*/);
                    LocalPInvoke.DeleteObject(trayRegion);
                    LocalPInvoke.DeleteObject(finalRegion);
                    finalRegion = combined;
                }
                
                if (snap.ShowClock && tb.ClockHwnd != IntPtr.Zero)
                {
                    double clockRelLeft = tb.ClockRect.Left - tb.TaskbarRect.Left;
                    double clockRelRight = tb.ClockRect.Right - tb.TaskbarRect.Left;
                    if (clockRelLeft > 1.0)
                    {
                        IntPtr clockRegion = LocalPInvoke.CreateRoundRectRgn(
                            Convert.ToInt32(clockRelLeft - sf), Convert.ToInt32(mainTop),
                            Convert.ToInt32(clockRelRight + marginRight), Convert.ToInt32(mainBottom),
                            Convert.ToInt32(cr), Convert.ToInt32(cr));
                            
                        IntPtr combined = LocalPInvoke.CreateRectRgn(0, 0, 0, 0);
                        LocalPInvoke.CombineRgn(combined, clockRegion, finalRegion, 2 /*RGN_OR*/);
                        LocalPInvoke.DeleteObject(clockRegion);
                        LocalPInvoke.DeleteObject(finalRegion);
                        finalRegion = combined;
                    }
                }
                
                if (snap.ShowWidgets && tb.WidgetsRect.Right > tb.WidgetsRect.Left &&
                    tb.WidgetsRect.Bottom > tb.WidgetsRect.Top)
                {
                    double widgetsRelLeft = tb.WidgetsRect.Left - tb.TaskbarRect.Left;
                    double widgetsRelRight = tb.WidgetsRect.Right - tb.TaskbarRect.Left;
                    if (widgetsRelRight > 1.0)
                    {
                        IntPtr widgetsRegion = LocalPInvoke.CreateRoundRectRgn(
                            Convert.ToInt32(Math.Max(marginLeft, widgetsRelLeft - Math.Max(0, marginLeft))), Convert.ToInt32(mainTop),
                            Convert.ToInt32(Math.Min(tbW - marginRight, widgetsRelRight + Math.Max(0, marginRight))), Convert.ToInt32(mainBottom),
                            Convert.ToInt32(cr), Convert.ToInt32(cr));
                            
                        IntPtr combined = LocalPInvoke.CreateRectRgn(0, 0, 0, 0);
                        LocalPInvoke.CombineRgn(combined, widgetsRegion, finalRegion, 2 /*RGN_OR*/);
                        LocalPInvoke.DeleteObject(widgetsRegion);
                        LocalPInvoke.DeleteObject(finalRegion);
                        finalRegion = combined;
                    }
                }

                ExternalTaskbarContent.Include(tb.TaskbarHwnd, tb.TaskbarRect, finalRegion);
                LocalPInvoke.SetWindowRgn(tb.TaskbarHwnd, finalRegion, true);
                tb.EffectiveRegion = new Types.EffectiveRegion
                {
                    CornerRadius = Convert.ToInt32(cr),
                    Top = Convert.ToInt32(mainTop),
                    Left = Convert.ToInt32(mainLeft),
                    Width = Math.Max(0, Convert.ToInt32(mainRight - mainLeft)),
                    Height = Math.Max(0, Convert.ToInt32(mainBottom - mainTop))
                };

                StripBorderAndGhost(tb.TaskbarHwnd);
                ForceRedraw(tb.TaskbarHwnd, snap, forceAccentReset);

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"UpdateDynamicTaskbar: {ex.Message}");
                TaskbarDiagnostics.Log($"UpdateDynamicTaskbar error: {ex.Message}");
                return false;
            }
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Pillar B-2: Strip DWM ghost border (Windows 11 only)
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        private static void StripBorderAndGhost(IntPtr hwnd)
        {
            // Strip WS_BORDER and WS_THICKFRAME
            uint style = LocalPInvoke.GetWindowLong(hwnd, LocalPInvoke.GWL_STYLE);
            style &= ~(LocalPInvoke.WS_BORDER | LocalPInvoke.WS_THICKFRAME);
            LocalPInvoke.SetWindowLong(hwnd, LocalPInvoke.GWL_STYLE, style);

            // Tell DWM not to draw any border (transparent sentinel)
            uint colourNone = LocalPInvoke.DWMWA_COLOR_NONE;
            LocalPInvoke.DwmSetWindowAttribute(
                hwnd,
                (LocalPInvoke.DWMWINDOWATTRIBUTE)LocalPInvoke.DWMWA_BORDER_COLOR,
                ref colourNone,
                sizeof(uint));
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  ForceRedraw â€” macht Region-Ã„nderungen sichtbar
        //
        //  Windows 11: SWP_FRAMECHANGED + RedrawWindow is enough to make
        //  SetWindowRgn updates visible without accent-policy workarounds.
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        private static void ForceRedraw(IntPtr hwnd, Types.Settings s,
                                        bool forceAccentReset = false)
        {
            if (s.CompositionCompat)
                Interaction.UpdateTranslucentTB(hwnd);

            // Pillar B-3: SWP_FRAMECHANGED refreshes DWM non-client geometry.
            LocalPInvoke.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                LocalPInvoke.SetWindowPosFlags.FrameChanged  |
                LocalPInvoke.SetWindowPosFlags.DoNotActivate |
                LocalPInvoke.SetWindowPosFlags.IgnoreMove    |
                LocalPInvoke.SetWindowPosFlags.IgnoreResize  |
                LocalPInvoke.SetWindowPosFlags.IgnoreZOrder);

            LocalPInvoke.RedrawWindow(hwnd, IntPtr.Zero, IntPtr.Zero,
                LocalPInvoke.RedrawWindowFlags.Invalidate  |
                LocalPInvoke.RedrawWindowFlags.Frame       |
                LocalPInvoke.RedrawWindowFlags.UpdateNow   |
                LocalPInvoke.RedrawWindowFlags.AllChildren);
        }


        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Fill-on-maximise
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public static bool TaskbarShouldBeFilled(IntPtr taskbarHwnd, Types.Settings settings)
        {
            if (!settings.FillOnMaximise) return false;

            // Windows 11: Task Switcher / Alt+Tab overlay
            if (settings.FillOnTaskSwitch)
            {
                IntPtr topHwnd = LocalPInvoke.WindowFromPoint(
                    new LocalPInvoke.POINT { x = 0, y = 0 });
                var cls = new StringBuilder(1024);
                try
                {
                    LocalPInvoke.GetClassName(topHwnd, cls, 1024);
                    if (cls.ToString() == "XamlExplorerHostIslandWindow") return true;
                }
                catch { }
            }

            IntPtr taskbarMonitor = LocalPInvoke.MonitorFromWindow(taskbarHwnd, 2);
            foreach (IntPtr hwnd in Interaction.GetTopLevelWindows())
            {
                if (!LocalPInvoke.IsWindowVisible(hwnd)) continue;
                if (LocalPInvoke.MonitorFromWindow(hwnd, 2) != taskbarMonitor) continue;

                // Fenster-Klasse lesen â€” Shell-, Desktop- und UWP-System-Fenster ausfiltern.
                // "Progman"                  = Desktop-Worker-Fenster (immer sichtbar)
                // "WorkerW"                  = Wallpaper-Container hinter dem Desktop
                // "Shell_TrayWnd"            = primÃ¤re Taskleiste
                // "Shell_SecondaryTrayWnd"   = sekundÃ¤re Taskleisten
                // "Button"                   = Desktop-Show-Button (untere rechte Ecke)
                // "ApplicationFrameWindow"   = UWP-App-Container (Edge, Einstellungen etc.)
                //                             Das Container-Fenster meldet sich als
                //                             maximiert, auch wenn die UWP-App selbst
                //                             nur im Vordergrund ist â†’ false-positive Fill.
                // "Windows.UI.Core.CoreWindow" = direktes UWP-Rendering-Fenster (StartmenÃ¼,
                //                             Action Center, sonstige Overlays). Kann als
                //                             maximiert erscheinen, sollte nie Fill auslÃ¶sen.
                // "ForegroundStaging"        = Shell-internes Staging-Fenster wÃ¤hrend
                //                             Fenster-ÃœbergÃ¤ngen â€” kurzzeitig sichtbar,
                //                             triggert sonst fÃ¤lschlicherweise einen Fill.
                var wcls = new StringBuilder(256);
                try { LocalPInvoke.GetClassName(hwnd, wcls, 256); } catch { continue; }
                string wclsStr = wcls.ToString();
                if (wclsStr == "Progman"                    ||
                    wclsStr == "WorkerW"                    ||
                    wclsStr == "Shell_TrayWnd"              ||
                    wclsStr == "Shell_SecondaryTrayWnd"     ||
                    wclsStr == "Button"                     ||
                    wclsStr == "ApplicationFrameWindow"     ||  // UWP-Container (false positive)
                    wclsStr == "Windows.UI.Core.CoreWindow" ||  // UWP-Rendering / StartmenÃ¼
                    wclsStr == "ForegroundStaging")             // Shell-Ãœbergangs-Fenster
                    continue;

                LocalPInvoke.DwmGetWindowAttribute(hwnd,
                    LocalPInvoke.DWMWINDOWATTRIBUTE.Cloaked,
                    out bool cloaked, sizeof(bool));
                if (cloaked) continue;

                var placement = LocalPInvoke.WINDOWPLACEMENT.Default;
                LocalPInvoke.GetWindowPlacement(hwnd, ref placement);
                if (placement.ShowCmd == LocalPInvoke.ShowWindowCommands.ShowMaximized)
                    return true;
            }

            return false;
        }

        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€
        //  Registry: centred taskbar
        // â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€â”€

        public static bool CheckIfCentred()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced"))
                {
                    bool centred = key != null && (int)(key.GetValue("TaskbarAl") ?? 0) == 1;
                    // #region agent log
                    DebugLog.Write("Taskbar.cs:CheckIfCentred", "registry centred check",
                        new { centred, taskbarAl = key?.GetValue("TaskbarAl") },
                        "D");
                    // #endregion
                    return centred;
                }
            }
            catch { return false; }
        }

        // ──────────────────────────────────────────────────────────────────────
        //  SnapshotSettings — avoids mutating shared ActiveSettings from bg thread
        // ──────────────────────────────────────────────────────────────────────

        private static Types.Settings SnapshotSettings(Types.Settings s)
        {
            var snap = new Types.Settings
            {
                Version           = s.Version,
                CornerRadius      = s.CornerRadius,
                MarginBasic       = s.MarginBasic,
                MarginBottom      = s.MarginBottom,
                MarginLeft        = s.MarginLeft,
                MarginRight       = s.MarginRight,
                MarginTop         = s.MarginTop,
                IsDynamic         = s.IsDynamic,
                IsCentred         = s.IsCentred,
                IsWindows11       = s.IsWindows11,
                ShowTray          = s.ShowTray,
                CompositionCompat = s.CompositionCompat,
                IsNotFirstLaunch  = s.IsNotFirstLaunch,
                FillOnMaximise    = s.FillOnMaximise,
                FillOnTaskSwitch  = s.FillOnTaskSwitch,
                ShowTrayOnHover   = s.ShowTrayOnHover,
                ShowTaskbarShadow = s.ShowTaskbarShadow,
                AutoHideMode      = s.AutoHideMode,
                ShowWidgets       = s.ShowWidgets,
                ShowClock         = s.ShowClock,
                ShowSegmentsOnHover = s.ShowSegmentsOnHover,
                MusicWidgetEnabled = s.MusicWidgetEnabled,
            };
            if (snap.MarginBasic != -384)
            {
                snap.MarginLeft   = snap.MarginBasic;
                snap.MarginTop    = snap.MarginBasic;
                snap.MarginRight  = snap.MarginBasic;
                snap.MarginBottom = snap.MarginBasic;
            }
            return snap;
        }

        /// <summary>
        /// Resets the taskbar to clear any cached DWM state that might cause flickering or rendering issues.
        /// </summary>
        public static void ResetTaskbar(IntPtr hwnd)
        {
            // Force a full DWM cache flush to prevent frozen grey/blue artifacts
            LocalPInvoke.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0,
                LocalPInvoke.SetWindowPosFlags.IgnoreMove |
                      LocalPInvoke.SetWindowPosFlags.IgnoreResize |
                      LocalPInvoke.SetWindowPosFlags.IgnoreZOrder |
                      LocalPInvoke.SetWindowPosFlags.FrameChanged);
        }
    }
}
