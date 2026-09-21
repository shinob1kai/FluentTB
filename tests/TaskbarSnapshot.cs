// Read-only diagnostic, run in the interactive desktop session.
using System;
using System.IO;
using System.Reflection;
using System.Text;
using FluentTB;
using System.Windows.Automation;
using System.Runtime.InteropServices;

internal static class TaskbarSnapshot
{
    [DllImport("gdi32.dll")] private static extern IntPtr CreateRectRgn(int left, int top, int right, int bottom);
    [DllImport("user32.dll")] private static extern int GetWindowRgn(IntPtr hwnd, IntPtr region);
    [DllImport("gdi32.dll")] private static extern bool PtInRegion(IntPtr region, int x, int y);
    [DllImport("gdi32.dll")] private static extern bool DeleteObject(IntPtr region);
    [DllImport("gdi32.dll")] private static extern int GetRgnBox(IntPtr region, out LocalPInvoke.RECT bounds);
    [STAThread]
    private static void Main()
    {
        var text = new StringBuilder();
        try
        {
            var engine = typeof(Types).Assembly.GetType("FluentTB.Taskbar");
            var bars = (System.Collections.Generic.List<Types.Taskbar>)engine
                .GetMethod("GenerateTaskbarInfo").Invoke(null, null);
            foreach (var bar in bars)
            {
                // Warm up the bounded asynchronous reader and record its final result.
                for (int sample = 0; sample < 5; sample++)
                {
                    System.Threading.Thread.Sleep(100);
                    var timer = System.Diagnostics.Stopwatch.StartNew();
                    var fresh = (Types.Taskbar)engine.GetMethod("GetQuickTaskbarRects").Invoke(null,
                        new object[] { bar.TaskbarHwnd, bar.TrayHwnd, bar.AppListHwnd });
                    bar.AppListRect = fresh.AppListRect;
                    bar.TrayRect = fresh.TrayRect;
                    text.AppendLine("Sample " + sample + " elapsed=" + timer.ElapsedMilliseconds +
                        "ms apps=" + Format(bar.AppListRect) + " tray=" + Format(bar.TrayRect));
                }
                text.AppendLine("Taskbar " + Format(bar.TaskbarRect) + " selected=" + Format(bar.AppListRect));
                LocalPInvoke.EnumChildWindows(bar.TaskbarHwnd, (hwnd, data) =>
                {
                    var name = new StringBuilder(256);
                    LocalPInvoke.GetClassName(hwnd, name, name.Capacity);
                    LocalPInvoke.RECT rect;
                    LocalPInvoke.GetWindowRect(hwnd, out rect);
                    text.AppendLine(name + " visible=" + LocalPInvoke.IsWindowVisible(hwnd) + " " + Format(rect));
                    if (name.ToString().StartsWith("HwndWrapper[FluentTB"))
                    {
                        IntPtr childRegion = CreateRectRgn(0, 0, 0, 0);
                        try
                        {
                            int kind = GetWindowRgn(hwnd, childRegion);
                            GetRgnBox(childRegion, out var bounds);
                            text.AppendLine("WIDGET_REGION type=" + kind + " bounds=" + Format(bounds) +
                                " cornerIncluded=" + PtInRegion(childRegion, bounds.Left, bounds.Top));
                        }
                        finally { DeleteObject(childRegion); }
                    }
                    return true;
                }, IntPtr.Zero);
                text.AppendLine("Accessibility descendants:");
                var root = AutomationElement.FromHandle(bar.TaskbarHwnd);
                IntPtr region = CreateRectRgn(0, 0, 0, 0);
                int regionType = GetWindowRgn(bar.TaskbarHwnd, region);
                int checkedButtons = 0, clippedButtons = 0, appButtons = 0, clippedApps = 0;
                try
                {
                foreach (AutomationElement element in root.FindAll(TreeScope.Descendants, Condition.TrueCondition))
                {
                    var current = element.Current;
                    text.AppendLine(current.ControlType.ProgrammaticName + " id=" + current.AutomationId +
                        " class=" + current.ClassName + " rect=" + current.BoundingRectangle +
                        " offscreen=" + current.IsOffscreen);
                    var r = current.BoundingRectangle;
                    if (current.ControlType == ControlType.Button && !r.IsEmpty && r.Width > 0 && r.Height > 0)
                    {
                        checkedButtons++;
                        int x = (int)(r.Left + r.Width / 2) - bar.TaskbarRect.Left;
                        int y = (int)(r.Top + r.Height / 2) - bar.TaskbarRect.Top;
                        if (regionType > 0 && !PtInRegion(region, x, y)) clippedButtons++;
                        if (current.AutomationId == "StartButton" ||
                            current.ClassName == "Taskbar.TaskListButtonAutomationPeer")
                        {
                            appButtons++;
                            if (regionType > 0 && !PtInRegion(region, x, y)) clippedApps++;
                        }
                    }
                }
                text.AppendLine("REGION type=" + regionType + " checked=" + checkedButtons + " clipped=" + clippedButtons);
                int gapX = (bar.AppListRect.Right + bar.TrayRect.Left) / 2 - bar.TaskbarRect.Left;
                int gapY = (bar.TaskbarRect.Bottom - bar.TaskbarRect.Top) / 2;
                bool gapIncluded = regionType <= 0 || PtInRegion(region, gapX, gapY);
                text.AppendLine("DYNAMIC apps=" + appButtons + " clippedApps=" + clippedApps +
                    " gapIncluded=" + gapIncluded);
                int appStart = bar.AppListRect.Left - bar.TaskbarRect.Left;
                int appEnd = bar.AppListRect.Right - bar.TaskbarRect.Left;
                int segmentLeft = (appStart + appEnd) / 2;
                int segmentRight = segmentLeft;
                int width = bar.TaskbarRect.Right - bar.TaskbarRect.Left;
                while (segmentLeft > 0 && PtInRegion(region, segmentLeft - 1, gapY)) segmentLeft--;
                while (segmentRight < width && PtInRegion(region, segmentRight, gapY)) segmentRight++;
                text.AppendLine("APP_SEGMENT left=" + segmentLeft + " right=" + segmentRight +
                    " leftPadding=" + (appStart - segmentLeft) + " rightPadding=" + (segmentRight - appEnd));
                }
                finally { DeleteObject(region); }
            }
            text.AppendLine("Taskbars: " + bars.Count);
        }
        catch (Exception ex) { text.AppendLine(ex.ToString()); }
        File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "taskbar-snapshot.txt"), text.ToString());
    }

    private static string Format(LocalPInvoke.RECT r)
    {
        return r.Left + "," + r.Top + "-" + r.Right + "," + r.Bottom;
    }
}
