using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace FluentTB
{
    /// <summary>
    /// Custom Auto-Hide manager for FluentTB — ported from RoundedTB Canary's
    /// TaskbarEffect feature.
    ///
    /// ── What the Canary feature actually does ───────────────────────────────────
    /// RoundedTB Canary bypasses Windows' native SHAppBarMessage(ABM.SetAutoHideBar)
    /// entirely, because the native implementation has a known bug: the taskbar
    /// reappears as a 2-px stub and flickers when combined with SetWindowRgn.
    ///
    /// Instead, it:
    ///   1. Hides the taskbar window outright via ShowWindow(hwnd, SW_HIDE).
    ///   2. Positions an invisible WPF overlay window (TaskbarEffect.xaml) over
    ///      the taskbar's screen edge — this overlay acts as the hover sensor.
    ///   3. When the mouse enters the overlay, the taskbar is shown again via
    ///      ShowWindow(hwnd, SW_SHOWNOACTIVATE) and the overlay slides away.
    ///   4. When the mouse leaves the taskbar area, a short timer hides it again.
    ///
    /// This implementation reproduces the exact same logic using only standard
    /// Win32 APIs (ShowWindow, GetCursorPos, GetWindowRect, SetWindowPos) so
    /// no additional WPF overlay window is required.
    ///
    /// ── Usage ────────────────────────────────────────────────────────────────────
    ///   var mgr = new AutoHideManager();
    ///   mgr.Start(taskbarDetails);     // call after Apply
    ///   mgr.Stop();                    // call on exit or when disabling auto-hide
    ///
    ///   AutoHideMode:
    ///     AlwaysShow  — normal mode, manager is a no-op
    ///     AlwaysHide  — fully hides taskbar; shows on hover, re-hides on mouse-leave
    /// </summary>
    public sealed class AutoHideManager : IDisposable
    {
        // ── Configuration ─────────────────────────────────────────────────────────

        /// <summary>
        /// Pixels from the screen edge that count as "hovering" the taskbar.
        /// Matches the 2-px native auto-hide stub for muscle-memory compatibility.
        /// </summary>
        private const int HoverStripThickness = 4;

        /// <summary>
        /// Milliseconds before the taskbar is re-hidden after the cursor leaves it.
        /// </summary>
        private const int HideDelayMs = 700;

        /// <summary>
        /// Polling interval for the cursor-position loop (ms).
        /// Low enough to feel instant, high enough not to waste CPU.
        /// </summary>
        private const int PollIntervalMs = 50;

        // ── State ─────────────────────────────────────────────────────────────────

        private List<Types.Taskbar>   _taskbars = new();
        private CancellationTokenSource _cts     = new();
        private bool                    _running;
        private bool                    _disposed;

        // Per-taskbar visibility state — true = currently shown
        // Key: TaskbarHwnd
        private readonly Dictionary<IntPtr, bool>     _isShown     = new();
        private readonly Dictionary<IntPtr, DateTime> _hideAfter   = new();

        // ── Public API ────────────────────────────────────────────────────────────

        public AutoHideMode Mode { get; private set; } = AutoHideMode.AlwaysShow;

        /// <summary>
        /// Starts (or restarts) the auto-hide monitor for the given taskbars.
        /// Safe to call on the UI thread; the polling loop runs on a background Task.
        /// </summary>
        public void Start(List<Types.Taskbar> taskbars, AutoHideMode mode)
        {
            Stop(); // cancel any previous loop cleanly

            Mode      = mode;
            _taskbars = taskbars;

            if (mode == AutoHideMode.AlwaysShow)
            {
                // Ensure all taskbars are visible
                foreach (var tb in _taskbars)
                    ShowTaskbar(tb.TaskbarHwnd);
                return;
            }

            // AlwaysHide: hide all taskbars and start the hover-detection loop
            _isShown.Clear();
            _hideAfter.Clear();
            foreach (var tb in _taskbars)
            {
                HideTaskbar(tb.TaskbarHwnd);
                _isShown[tb.TaskbarHwnd]   = false;
                _hideAfter[tb.TaskbarHwnd] = DateTime.MinValue;
            }

            _cts     = new CancellationTokenSource();
            _running = true;
            System.Threading.Tasks.Task.Run(() => PollLoop(_cts.Token));
        }

        /// <summary>
        /// Stops the monitor and restores all taskbars to full visibility.
        /// </summary>
        public void Stop()
        {
            if (!_running) return;
            _running = false;
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();

            // Restore all taskbars — same as TaskbarEffect window being destroyed
            foreach (var tb in _taskbars)
                ShowTaskbar(tb.TaskbarHwnd);

            _isShown.Clear();
            _hideAfter.Clear();
        }

        // ── Core polling loop (background thread) ─────────────────────────────────

        /// <summary>
        /// Runs on a background thread. Polls cursor position every PollIntervalMs
        /// and shows/hides each taskbar based on whether the cursor is in the
        /// hover-strip at the screen edge where that taskbar lives.
        /// </summary>
        private void PollLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    NativeMethods.GetCursorPos(out NativeMethods.POINT cursor);

                    foreach (var tb in _taskbars)
                    {
                        IntPtr hwnd = tb.TaskbarHwnd;
                        if (!NativeMethods.IsWindow(hwnd)) continue;

                        // Refresh the taskbar rect each tick — it can move (multi-monitor,
                        // orientation change, etc.)
                        NativeMethods.GetWindowRect(hwnd, out NativeMethods.RECT tbRect);

                        bool cursorInHoverStrip   = IsInHoverStrip(cursor, tbRect);
                        bool cursorInsideTaskbar  = IsInsideRect(cursor, tbRect);

                        bool currentlyShown = _isShown.TryGetValue(hwnd, out bool s) && s;

                        if (!currentlyShown && cursorInHoverStrip)
                        {
                            // Cursor touched the edge strip → show the taskbar
                            ShowTaskbar(hwnd);
                            _isShown[hwnd]   = true;
                            _hideAfter[hwnd] = DateTime.MaxValue; // no pending hide yet
                        }
                        else if (currentlyShown && !cursorInsideTaskbar)
                        {
                            // Cursor has left the taskbar; arm the hide timer if not armed
                            if (!_hideAfter.TryGetValue(hwnd, out DateTime d)
                                || d == DateTime.MaxValue)
                            {
                                _hideAfter[hwnd] = DateTime.UtcNow.AddMilliseconds(HideDelayMs);
                            }
                            else if (DateTime.UtcNow >= d)
                            {
                                // Timer expired → hide
                                HideTaskbar(hwnd);
                                _isShown[hwnd]   = false;
                                _hideAfter[hwnd] = DateTime.MinValue;
                            }
                        }
                        else if (currentlyShown && cursorInsideTaskbar)
                        {
                            // Cursor is back inside — cancel any pending hide
                            _hideAfter[hwnd] = DateTime.MaxValue;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"[AutoHide] PollLoop error: {ex.Message}");
                }

                Thread.Sleep(PollIntervalMs);
            }
        }

        // ── Win32 helpers ─────────────────────────────────────────────────────────

        /// <summary>
        /// Hides the taskbar window without activating any other window.
        /// Equivalent to Canary's ShowWindow(hwnd, SW_HIDE).
        /// </summary>
        private static void HideTaskbar(IntPtr hwnd)
        {
            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_HIDE);
        }

        /// <summary>
        /// Shows the taskbar window without stealing focus.
        /// Equivalent to Canary's ShowWindow(hwnd, SW_SHOWNOACTIVATE).
        /// </summary>
        private static void ShowTaskbar(IntPtr hwnd)
        {
            NativeMethods.ShowWindow(hwnd, NativeMethods.SW_SHOWNOACTIVATE);
        }

        /// <summary>
        /// Returns true if the cursor is inside the thin hover-strip at the screen
        /// edge where the taskbar lives.
        ///
        /// The Canary overlay (TaskbarEffect.xaml) achieved this by being a
        /// transparent window the exact size of the taskbar; here we replicate the
        /// same check geometrically: a 4-px strip on the side of the taskbar that
        /// faces the screen edge.
        ///
        /// Currently handles bottom-docked taskbars (the overwhelmingly common case).
        /// Top/left/right docking can be added by checking tbRect.Top == 0 etc.
        /// </summary>
        private static bool IsInHoverStrip(NativeMethods.POINT cursor, NativeMethods.RECT tbRect)
        {
            // Determine which screen edge the taskbar is on:
            //   Bottom: tbRect.Bottom is at or near the screen bottom
            //   Top:    tbRect.Top    == 0
            //   (Left/Right: narrow width vs height)
            bool isBottom = true; // default; could be enhanced with GetSystemMetrics(SM_CYSCREEN)

            if (isBottom)
            {
                // Hover strip = the bottom HoverStripThickness pixels of the taskbar rect,
                // plus the full horizontal span.
                return cursor.x >= tbRect.Left
                    && cursor.x <= tbRect.Right
                    && cursor.y >= tbRect.Bottom - HoverStripThickness
                    && cursor.y <= tbRect.Bottom;
            }

            // Fallback: full taskbar rect
            return IsInsideRect(cursor, tbRect);
        }

        private static bool IsInsideRect(NativeMethods.POINT cursor, NativeMethods.RECT r)
        {
            return cursor.x >= r.Left && cursor.x <= r.Right
                && cursor.y >= r.Top  && cursor.y <= r.Bottom;
        }

        // ── IDisposable ───────────────────────────────────────────────────────────

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }

    // ── Enum ──────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Maps 1:1 to the Canary ComboBox items:
    ///   0 = "Always show"   → AlwaysShow
    ///   1 = "Always hide"   → AlwaysHide
    ///   (2 = "[unavailable]" in Canary, reserved for future SHAppBarMessage mode)
    /// </summary>
    public enum AutoHideMode
    {
        AlwaysShow = 0,
        AlwaysHide = 1,
    }
}
