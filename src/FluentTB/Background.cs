using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Threading;
using System.Windows;

namespace FluentTB
{
    public class Background
    {
        public MainWindow mw;
        private int _infrequentCount = 0;

        public Background()
        {
            mw = (MainWindow)Application.Current.MainWindow;
        }

        public void DoWork(object sender, DoWorkEventArgs e)
        {
            mw.interaction.AddLog("Background worker started.");
            var worker = (BackgroundWorker)sender;

            while (!worker.CancellationPending)
            {
                try
                {
                    RunInfrequentTasks();
                    RunPerTickTasks();
                }
                catch (TypeInitializationException ex)
                {
                    mw.interaction.AddLog($"TypeInitializationException: {ex.Message}\n{ex.InnerException?.Message}");
                    App.LogCrash("Background.DoWork TypeInitializationException", ex);
                    throw;
                }
                catch (Exception ex)
                {
                    mw.interaction.AddLog($"Background tick error: {ex.Message}");
                    App.Log($"Background tick error: {ex.GetType().Name}: {ex.Message}");
                }

                Thread.Sleep(100);
            }

            e.Cancel = true;
            mw.interaction.AddLog("Background worker cancelled.");
        }

        private void RunInfrequentTasks()
        {
            if (++_infrequentCount < 10) return;
            _infrequentCount = 0;

            foreach (IntPtr hwnd in Interaction.GetTopLevelWindows())
            {
                var cls = new StringBuilder(1024);
                var title = new StringBuilder(1024);

                try
                {
                    LocalPInvoke.GetClassName(hwnd, cls, 1024);
                    LocalPInvoke.GetWindowText(hwnd, title, 1024);

                    if (cls.ToString().Contains("HwndWrapper[FluentTB.exe") &&
                        title.ToString() == "FluentTB_SettingsRequest")
                    {
                        mw.Dispatcher.Invoke(() =>
                        {
                            if (mw.Visibility != Visibility.Visible)
                                mw.ShowMenuItem_Click(null, null);
                        });

                        LocalPInvoke.SetWindowText(hwnd, "FluentTB");
                    }
                }
                catch
                {
                }
            }

            mw.Dispatcher.Invoke(() => mw.TrayIconCheck());
        }

        private void RunPerTickTasks()
        {
            List<Types.Taskbar> taskbars;
            Types.Settings settings;
            bool shellDirty;

            lock (mw.TaskbarStateLock)
            {
                if (mw.activeSettings == null)
                    return;

                settings = mw.activeSettings.Clone();
                taskbars = CloneTaskbarList(mw.taskbarDetails);

                shellDirty = mw.maximisedStateDirty;
                if (shellDirty)
                    mw.maximisedStateDirty = false;
            }

            if (taskbars.Count == 0)
                return;

            bool showTrayBefore = settings.ShowTray;
            bool centredBefore = settings.IsCentred;
            settings.IsCentred = Taskbar.CheckIfCentred();
            bool centredChanged = settings.IsCentred != centredBefore;
            bool anyChange = false;

            if (Taskbar.TaskbarCountOrHandleChanged(taskbars.Count, taskbars[0].TaskbarHwnd))
            {
                taskbars = Taskbar.GenerateTaskbarInfo();
                mw.interaction.AddLog("Taskbar layout changed; regenerated info.");
                anyChange = true;
            }

            for (int i = 0; i < taskbars.Count; i++)
            {
                Types.Taskbar tb = taskbars[i];

                if (tb.TaskbarHwnd == IntPtr.Zero || tb.AppListHwnd == IntPtr.Zero)
                {
                    taskbars = Taskbar.GenerateTaskbarInfo();
                    mw.interaction.AddLog("Stale taskbar handle; regenerated info.");
                    anyChange = true;
                    break;
                }

                Types.Taskbar fresh;
                try
                {
                    fresh = Taskbar.GetQuickTaskbarRects(
                        tb.TaskbarHwnd, tb.TrayHwnd, tb.AppListHwnd);
                }
                catch
                {
                    continue;
                }

                bool wasIgnored = tb.Ignored;
                bool geometryChanged = Taskbar.TaskbarRefreshRequired(tb, fresh, settings.IsDynamic);

                // Only enumerate top-level windows when the fill state can have changed.
                // When already filled and no shell/geometry event arrived, keep the
                // current filled state instead of treating "not checked" as "not filled".
                bool shouldFill = Taskbar.TaskbarShouldBeFilled(tb.TaskbarHwnd, settings);

                // #region agent log
                if (shellDirty || wasIgnored)
                {
                    DebugLog.Write("Background.cs:fill", "fill-state check",
                        new { i, shellDirty, wasIgnored, shouldFill, settings.FillOnMaximise },
                        "E");
                }
                // #endregion

                if (shouldFill)
                {
                    if (!wasIgnored)
                    {
                        Taskbar.ResetTaskbar(tb, settings);
                        tb.Ignored = true;
                        anyChange = true;
                    }

                    taskbars[i] = tb;
                    continue;
                }

                if (wasIgnored)
                {
                    tb.Ignored = false;
                    anyChange = true;
                }

                if (settings.ShowTrayOnHover && tb.TrayRect.Left != 0)
                {
                    LocalPInvoke.GetCursorPos(out LocalPInvoke.POINT cursor);
                    LocalPInvoke.RECT trayRect = tb.TrayRect;
                    bool hovering = LocalPInvoke.PtInRect(ref trayRect, cursor);

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

                // TaskbarAl can change before Explorer has reported a new task-list
                // rectangle.  Treat the alignment transition itself as a redraw
                // trigger, otherwise FluentTB can keep the old left-aligned region
                // until a separate icon-layout change happens.
                bool needsRedraw = geometryChanged || centredChanged || tb.Ignored || shellDirty || wasIgnored;
                if (!needsRedraw)
                {
                    taskbars[i] = tb;
                    continue;
                }

                tb.Ignored = false;
                tb.TaskbarRect = fresh.TaskbarRect;
                tb.AppListRect = fresh.AppListRect;
                tb.TrayRect = fresh.TrayRect;

                int gap = fresh.TrayRect.Left - fresh.AppListRect.Right;
                bool isSimple = !settings.IsDynamic
                    || (gap <= tb.ScaleFactor * 25 && gap > 0 && fresh.TrayRect.Left != 0);

                // #region agent log
                if (needsRedraw)
                {
                    bool dynamicValid = Taskbar.CheckDynamicUpdateIsValid(tb, fresh);
                    DebugLog.Write("Background.cs:route", "taskbar update routing",
                        new
                        {
                            i,
                            settings.IsDynamic,
                            settings.IsCentred,
                            gap,
                            gapThreshold = tb.ScaleFactor * 25,
                            isSimple,
                            dynamicValid,
                            appListRight = fresh.AppListRect.Right,
                            trayLeft = fresh.TrayRect.Left,
                            taskbarWidth = fresh.TaskbarRect.Right - fresh.TaskbarRect.Left
                        },
                        isSimple ? "A" : (dynamicValid ? "B" : "C"));
                }
                // #endregion

                if (isSimple)
                {
                    Taskbar.UpdateSimpleTaskbar(tb, settings, forceAccentReset: wasIgnored);
                    if (_infrequentCount == 0)
                        mw.interaction.AddLog($"Taskbar[{i}] updated (simple). Gap={gap}, forceReset={wasIgnored}");
                }
                else if (Taskbar.CheckDynamicUpdateIsValid(tb, fresh))
                {
                    Taskbar.UpdateDynamicTaskbar(tb, settings, forceAccentReset: wasIgnored);
                    if (_infrequentCount == 0)
                        mw.interaction.AddLog($"Taskbar[{i}] updated (dynamic). Gap={gap}, forceReset={wasIgnored}");
                }

                taskbars[i] = tb;
                anyChange = true;
            }

            bool showTrayChanged = settings.ShowTray != showTrayBefore;
            if (!anyChange && !showTrayChanged && !centredChanged)
                return;

            mw.SyncTaskbarShadows(taskbars, settings);

            lock (mw.TaskbarStateLock)
            {
                if (anyChange)
                    mw.taskbarDetails = taskbars;

                if (showTrayChanged && mw.activeSettings != null)
                    mw.activeSettings.ShowTray = settings.ShowTray;

                if (centredChanged && mw.activeSettings != null)
                    mw.activeSettings.IsCentred = settings.IsCentred;
            }
        }

        private static List<Types.Taskbar> CloneTaskbarList(IEnumerable<Types.Taskbar> source)
        {
            var result = new List<Types.Taskbar>();
            if (source == null)
                return result;

            foreach (Types.Taskbar taskbar in source)
            {
                Types.Taskbar clone = CloneTaskbar(taskbar);
                if (clone != null)
                    result.Add(clone);
            }

            return result;
        }

        private static Types.Taskbar CloneTaskbar(Types.Taskbar taskbar)
        {
            if (taskbar == null)
                return null;

            return new Types.Taskbar
            {
                TaskbarHwnd = taskbar.TaskbarHwnd,
                TrayHwnd = taskbar.TrayHwnd,
                AppListHwnd = taskbar.AppListHwnd,
                TaskbarRect = taskbar.TaskbarRect,
                TrayRect = taskbar.TrayRect,
                AppListRect = taskbar.AppListRect,
                RecoveryHrgn = taskbar.RecoveryHrgn,
                ScaleFactor = taskbar.ScaleFactor,
                TaskbarRes = taskbar.TaskbarRes,
                Ignored = taskbar.Ignored,
                TrayHidden = taskbar.TrayHidden,
                AppListWidth = taskbar.AppListWidth,
                TaskbarEffectWindow = taskbar.TaskbarEffectWindow,
                EffectiveRegion = taskbar.EffectiveRegion
            };
        }
    }
}
