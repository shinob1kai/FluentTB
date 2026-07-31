using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace FluentTB
{
    /// <summary>
    /// Background monitoring loop for FluentTB.
    ///
    /// Design:
    ///   - RunLoop() is called via Task.Run() and runs on a thread-pool thread.
    ///   - It never touches WPF UI objects directly.
    ///   - Any UI interaction (tray icon refresh, show-settings) goes through
    ///     Dispatcher.BeginInvoke so the UI thread remains free.
    ///   - No BackgroundWorker, no DoEvents() — those were the root cause of the
    ///     frozen/white window bug.
    /// </summary>
    public class BackgroundEngine
    {
        private readonly MainWindow _mw;
        private int _infrequentCount;

        public BackgroundEngine()
        {
            _mw = (MainWindow)Application.Current.MainWindow;
        }

        /// <summary>
        /// Main monitor loop. Call via Task.Run(). Returns when token is cancelled.
        /// </summary>
        public void RunLoop(CancellationToken ct)
        {
            _mw.Interaction.AddLog("Background engine started.");

            while (!ct.IsCancellationRequested)
            {
                try
                {
                    Tick();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (TypeInitializationException ex)
                {
                    _mw.Interaction.AddLog(ex.Message);
                    if (ex.InnerException != null)
                        _mw.Interaction.AddLog(ex.InnerException.Message);
                    // Fatal — re-throw so Task framework captures it
                    throw;
                }
                catch (Exception ex)
                {
                    _mw.Interaction.AddLog($"Background loop error: {ex.Message}");
                }

                // 100 ms tick — use cancellable sleep so exit is instant
                try { Task.Delay(100, ct).Wait(ct); }
                catch (OperationCanceledException) { break; }
            }

            _mw.Interaction.AddLog("Background engine stopped.");
        }

        // -------------------------------------------------------------------------
        // One tick of work
        // -------------------------------------------------------------------------

        private void Tick()
        {
            // ----- Infrequent tasks (every ~1 s = 10 × 100 ms) -----
            _infrequentCount++;
            if (_infrequentCount >= 10)
            {
                _infrequentCount = 0;
                CheckForSettingsRequest();
                // Update tray icon on the UI thread (fire-and-forget)
                _mw.Dispatcher.BeginInvoke(_mw.TrayIconCheck);
            }

            // ----- Main loop -----
            bool isCentred = TaskbarManager.CheckIfCentred();
            _mw.ActiveSettings.IsCentred = isCentred;

            List<Types.Taskbar> taskbars = _mw.TaskbarDetails;
            Types.Settings settings = _mw.ActiveSettings;

            if (taskbars.Count == 0) return;

            // Regenerate if monitors changed
            if (TaskbarManager.TaskbarCountOrHandleChanged(
                    taskbars.Count, taskbars[0].TaskbarHwnd))
            {
                taskbars = TaskbarManager.GenerateTaskbarInfo();
                _mw.TaskbarDetails = taskbars;
                Debug.WriteLine("Regenerating taskbar info.");
            }

            for (int i = 0; i < taskbars.Count; i++)
            {
                var tb = taskbars[i];
                if (tb.TaskbarHwnd == IntPtr.Zero || tb.AppListHwnd == IntPtr.Zero)
                {
                    taskbars = TaskbarManager.GenerateTaskbarInfo();
                    _mw.TaskbarDetails = taskbars;
                    break;
                }

                Types.Taskbar fresh = TaskbarManager.GetQuickTaskbarRects(
                    tb.TaskbarHwnd, tb.TrayHwnd, tb.AppListHwnd);

                // Fill on maximise / task switch?
                if (TaskbarManager.TaskbarShouldBeFilled(tb.TaskbarHwnd, settings))
                {
                    if (!tb.Ignored)
                    {
                        TaskbarManager.ResetTaskbar(tb, settings);
                        tb.Ignored = true;
                    }
                    continue;
                }

                // ShowTrayOnHover
                if (settings.ShowTrayOnHover && tb.TrayRect.Left != 0)
                {
                    NativeMethods.GetCursorPos(out NativeMethods.POINT cursor);
                    NativeMethods.RECT trayRect = tb.TrayRect;
                    bool hovering = NativeMethods.PtInRect(ref trayRect, cursor);
                    if (hovering && !settings.ShowTray)
                    {
                        settings.ShowTray = true;
                        tb.Ignored = true;
                    }
                    else if (!hovering && settings.ShowTray)
                    {
                        settings.ShowTray = false;
                        tb.Ignored = true;
                    }
                }

                if (!TaskbarManager.TaskbarRefreshRequired(tb, fresh, settings.IsDynamic)
                    && !tb.Ignored)
                    continue;

                tb.Ignored = false;
                int gap = fresh.TrayRect.Left - fresh.AppListRect.Right;
                _mw.Interaction.AddLog(
                    $"TB[{i}] gap={gap} appListR={fresh.AppListRect.Right} trayL={fresh.TrayRect.Left}");

                bool isFull = !settings.IsDynamic
                    || (gap <= tb.ScaleFactor * 25 && gap > 0 && fresh.TrayRect.Left != 0);

                tb.TaskbarRect  = fresh.TaskbarRect;
                tb.AppListRect  = fresh.AppListRect;
                tb.TrayRect     = fresh.TrayRect;

                if (isFull)
                {
                    TaskbarManager.UpdateSimpleTaskbar(tb, settings);
                    _mw.Interaction.AddLog($"TB[{i}] updated simply.");
                }
                else if (TaskbarManager.CheckDynamicUpdateIsValid(tb, fresh))
                {
                    TaskbarManager.UpdateDynamicTaskbar(tb, settings);
                    _mw.Interaction.AddLog($"TB[{i}] updated dynamically.");
                }
            }

            _mw.TaskbarDetails = taskbars;
        }

        // -------------------------------------------------------------------------
        // Multi-instance IPC
        // -------------------------------------------------------------------------

        private void CheckForSettingsRequest()
        {
            List<IntPtr> windows = Interaction.GetTopLevelWindows();
            foreach (IntPtr hwnd in windows)
            {
                var cls   = new StringBuilder(1024);
                var title = new StringBuilder(1024);
                try
                {
                    NativeMethods.GetClassName(hwnd, cls, 1024);
                    NativeMethods.GetWindowText(hwnd, title, 1024);

                    if (cls.ToString().Contains("HwndWrapper[FluentTB.exe")
                        && title.ToString() == "FluentTB_SettingsRequest")
                    {
                        _mw.Dispatcher.BeginInvoke(() =>
                        {
                            if (_mw.Visibility != Visibility.Visible)
                                _mw.ShowMenuItem_Click(null, null);
                        });
                        NativeMethods.SetWindowText(hwnd, "FluentTB");
                    }
                }
                catch { /* window may have been destroyed */ }
            }
        }
    }
}
