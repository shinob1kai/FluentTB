using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Runtime.InteropServices;

namespace FluentTB
{
    // The legacy MSTask* windows no longer describe the Windows 11 XAML buttons.
    // Keep UIA objects on an MTA worker and return only value-type geometry.
    internal static class TaskbarContentReader
    {
        [DllImport("user32.dll")]
        private static extern uint GetWindowThreadProcessId(IntPtr window, out uint processId);
        private sealed class Request
        {
            public DateTime Started;
            public Task<Bounds> Task;
        }

        internal struct Bounds
        {
            public LocalPInvoke.RECT Taskbar;
            public LocalPInvoke.RECT Apps;
            public LocalPInvoke.RECT Tray;
            public LocalPInvoke.RECT Widgets;
        }

        private static readonly object Sync = new object();
        private static readonly Dictionary<IntPtr, Request> Requests = new Dictionary<IntPtr, Request>();

        internal static Bounds Read(IntPtr hwnd, LocalPInvoke.RECT expected)
        {
            Request request;
            lock (Sync)
            {
                // Explorer recreation must not retain abandoned requests indefinitely.
                foreach (var key in new List<IntPtr>(Requests.Keys))
                    if (!LocalPInvoke.IsWindow(key)) Requests.Remove(key);

                if (!Requests.TryGetValue(hwnd, out request))
                {
                    request = new Request { Started = DateTime.UtcNow, Task = Task.Run(() => Query(hwnd)) };
                    Requests.Add(hwnd, request);
                }
            }

            // One in-flight query per taskbar; a stuck provider cannot cause an
            // unbounded stream of workers or block Apply/the background loop.
            if (!request.Task.Wait(150)) return default(Bounds);
            lock (Sync)
            {
                Request current;
                if (Requests.TryGetValue(hwnd, out current) && ReferenceEquals(current, request))
                    Requests.Remove(hwnd);
            }
            var result = request.Task.Result;
            if ((DateTime.UtcNow - request.Started).TotalMilliseconds > 500 ||
                !Equal(result.Taskbar, expected)) return default(Bounds);
            return result;
        }

        private static Bounds Query(IntPtr hwnd)
        {
            try
            {
                LocalPInvoke.RECT before;
                if (!LocalPInvoke.GetWindowRect(hwnd, out before)) return default(Bounds);
                var root = AutomationElement.FromHandle(hwnd);
                uint explorerProcess;
                GetWindowThreadProcessId(hwnd, out explorerProcess);
                var frame = root.FindFirst(TreeScope.Descendants,
                    new PropertyCondition(AutomationElement.AutomationIdProperty, "TaskbarFrame"));
                if (frame == null) return default(Bounds);

                var cache = new CacheRequest { AutomationElementMode = AutomationElementMode.None };
                cache.Add(AutomationElement.AutomationIdProperty);
                cache.Add(AutomationElement.ClassNameProperty);
                cache.Add(AutomationElement.ProcessIdProperty);
                cache.Add(AutomationElement.BoundingRectangleProperty);
                Rect apps = Rect.Empty, tray = Rect.Empty, widgets = Rect.Empty;
                bool hasStart = false;
                using (cache.Activate())
                {
                    // System-tray peers are siblings of TaskbarFrame on current
                    // Explorer versions, including the clock on secondary monitors.
                    var buttons = root.FindAll(TreeScope.Descendants,
                        new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
                    foreach (AutomationElement button in buttons)
                    {
                        var info = button.Cached;
                        // Child widgets from this app or another process are not Explorer buttons.
                        if (info.ProcessId != explorerProcess) continue;
                        Rect rect = info.BoundingRectangle;
                        if (rect.IsEmpty || rect.Width <= 0 || rect.Height <= 0) continue;
                        if (!Inside(rect, before)) return default(Bounds);
                        if (info.AutomationId == "WidgetsButton")
                            widgets.Union(rect);
                        else if (info.AutomationId == "SystemTrayIcon" ||
                            info.ClassName.StartsWith("SystemTray.", StringComparison.Ordinal))
                            tray.Union(rect);
                        else
                        {
                            // Include Start/Search and other real taskbar buttons,
                            // not just running applications. Never use localized names.
                            apps.Union(rect);
                            hasStart |= info.AutomationId == "StartButton";
                        }
                    }
                }

                LocalPInvoke.RECT after;
                if (!hasStart || apps.IsEmpty || !LocalPInvoke.GetWindowRect(hwnd, out after) ||
                    !Equal(before, after)) return default(Bounds);
                return new Bounds { Taskbar = after, Apps = ToRect(apps), Tray = ToRect(tray), Widgets = ToRect(widgets) };
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Taskbar content unavailable: " + ex.Message);
                return default(Bounds);
            }
        }

        private static bool Inside(Rect r, LocalPInvoke.RECT outer)
        {
            return !double.IsNaN(r.X) && !double.IsNaN(r.Y) &&
                r.Left >= outer.Left && r.Right <= outer.Right &&
                r.Top >= outer.Top && r.Bottom <= outer.Bottom;
        }

        private static LocalPInvoke.RECT ToRect(Rect r)
        {
            if (r.IsEmpty) return default(LocalPInvoke.RECT);
            return new LocalPInvoke.RECT
            {
                Left = (int)Math.Floor(r.Left), Top = (int)Math.Floor(r.Top),
                Right = (int)Math.Ceiling(r.Right), Bottom = (int)Math.Ceiling(r.Bottom)
            };
        }

        private static bool Equal(LocalPInvoke.RECT a, LocalPInvoke.RECT b)
        {
            return a.Left == b.Left && a.Top == b.Top && a.Right == b.Right && a.Bottom == b.Bottom;
        }
    }
}
