using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace FluentTB
{
    /// <summary>
    /// Custom Auto-Hide manager for FluentTB.
    ///
    /// Bypasses Windows' native SHAppBarMessage(ABM.SetAutoHideBar) entirely because
    /// the native implementation flickers heavily when combined with SetWindowRgn.
    ///
    /// Instead it:
    ///   1. Hides the taskbar via ShowWindow(SW_HIDE).
    ///   2. Polls cursor position every 50 ms on a background thread.
    ///   3. Shows the taskbar via ShowWindow(SW_SHOWNOACTIVATE) when the cursor
    ///      enters a 4 px hover strip at the screen edge.
    ///   4. Re-hides after a 700 ms delay once the cursor leaves the taskbar.
    /// </summary>
    public sealed class AutoHideManager : IDisposable
    {
        // ── Configuration ────────────────────────────────────────────────────
        private const int HoverStripThickness = 4;   // px from screen edge
        private const int HideDelayMs         = 700; // ms before re-hide
        private const int PollIntervalMs      = 50;  // ms between cursor polls

        // ── State ────────────────────────────────────────────────────────────
        private List<Types.Taskbar>         _taskbars = new List<Types.Taskbar>();
        private CancellationTokenSource     _cts      = new CancellationTokenSource();
        private bool                        _running;
        private bool                        _disposed;

        private readonly Dictionary<IntPtr, bool>     _isShown   = new Dictionary<IntPtr, bool>();
        private readonly Dictionary<IntPtr, DateTime> _hideAfter = new Dictionary<IntPtr, DateTime>();

        // ── Public API ────────────────────────────────────────────────────────
        public AutoHideMode Mode { get; private set; } = AutoHideMode.AlwaysShow;

        /// <summary>
        /// Starts (or restarts) the auto-hide monitor.
        /// Safe to call on the UI thread; polling runs on a background thread.
        /// </summary>
        public void Start(List<Types.Taskbar> taskbars, AutoHideMode mode)
        {
            Stop();

            Mode      = mode;
            _taskbars = taskbars;

            if (mode == AutoHideMode.AlwaysShow)
            {
                foreach (var tb in _taskbars)
                    ShowTaskbar(tb.TaskbarHwnd);
                return;
            }

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

        /// <summary>Stops the monitor and restores all taskbars to full visibility.</summary>
        public void Stop()
        {
            if (!_running) return;
            _running = false;
            _cts.Cancel();
            _cts.Dispose();
            _cts = new CancellationTokenSource();

            foreach (var tb in _taskbars)
                ShowTaskbar(tb.TaskbarHwnd);

            _isShown.Clear();
            _hideAfter.Clear();
        }

        // ── Core polling loop ────────────────────────────────────────────────
        private void PollLoop(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    LocalPInvoke.GetCursorPos(out LocalPInvoke.POINT cursor);
                    var nativeCursor = new NativeMethods.POINT { x = cursor.x, y = cursor.y };

                    foreach (var tb in _taskbars)
                    {
                        IntPtr hwnd = tb.TaskbarHwnd;
                        if (!LocalPInvoke.IsWindow(hwnd)) continue;

                        LocalPInvoke.GetWindowRect(hwnd, out LocalPInvoke.RECT lRect);
                        var tbRect = new NativeMethods.RECT
                        {
                            Left   = lRect.Left,
                            Top    = lRect.Top,
                            Right  = lRect.Right,
                            Bottom = lRect.Bottom
                        };

                        bool inStrip  = IsInHoverStrip(nativeCursor, tbRect);
                        bool inTaskbar= IsInsideRect(nativeCursor, tbRect);

                        bool shown = _isShown.TryGetValue(hwnd, out bool s) && s;

                        if (!shown && inStrip)
                        {
                            ShowTaskbar(hwnd);
                            _isShown[hwnd]   = true;
                            _hideAfter[hwnd] = DateTime.MaxValue;
                        }
                        else if (shown && !inTaskbar)
                        {
                            if (!_hideAfter.TryGetValue(hwnd, out DateTime d)
                                || d == DateTime.MaxValue)
                            {
                                _hideAfter[hwnd] = DateTime.UtcNow.AddMilliseconds(HideDelayMs);
                            }
                            else if (DateTime.UtcNow >= d)
                            {
                                HideTaskbar(hwnd);
                                _isShown[hwnd]   = false;
                                _hideAfter[hwnd] = DateTime.MinValue;
                            }
                        }
                        else if (shown && inTaskbar)
                        {
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

        // ── Win32 helpers ─────────────────────────────────────────────────────
        private static void HideTaskbar(IntPtr hwnd)
            => LocalPInvoke.ShowWindow(hwnd, LocalPInvoke.SW_HIDE);

        private static void ShowTaskbar(IntPtr hwnd)
            => LocalPInvoke.ShowWindow(hwnd, LocalPInvoke.SW_SHOWNOACTIVATE);

        private static bool IsInHoverStrip(NativeMethods.POINT cursor, NativeMethods.RECT tbRect)
        {
            // Bottom-docked (most common case): bottom HoverStripThickness px of the rect
            return cursor.x >= tbRect.Left
                && cursor.x <= tbRect.Right
                && cursor.y >= tbRect.Bottom - HoverStripThickness
                && cursor.y <= tbRect.Bottom;
        }

        private static bool IsInsideRect(NativeMethods.POINT cursor, NativeMethods.RECT r)
            => cursor.x >= r.Left && cursor.x <= r.Right
            && cursor.y >= r.Top  && cursor.y <= r.Bottom;

        // ── IDisposable ───────────────────────────────────────────────────────
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            Stop();
        }
    }

    public enum AutoHideMode
    {
        AlwaysShow = 0,
        AlwaysHide = 1,
    }
}
