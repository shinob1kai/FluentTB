using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace FluentTB
{
    // The shared shell registers its widget HWNDs. Their own regions stay owned
    // by their windows; only a copied, translated region is added to Explorer.
    public static class ExternalTaskbarContent
    {
        private static readonly HashSet<IntPtr> Windows = new HashSet<IntPtr>();
        private static readonly object Sync = new object();
        [DllImport("user32.dll")] private static extern IntPtr GetParent(IntPtr window);
        [DllImport("user32.dll")] private static extern int GetWindowRgn(IntPtr window, IntPtr region);
        [DllImport("gdi32.dll")] private static extern int OffsetRgn(IntPtr region, int x, int y);

        public static void Register(IntPtr window)
        {
            lock (Sync) Windows.Add(window);
            if (MainWindow.Current != null) MainWindow.Current.maximisedStateDirty = true;
        }

        public static void Unregister(IntPtr window)
        {
            lock (Sync) Windows.Remove(window);
            if (MainWindow.Current != null) MainWindow.Current.maximisedStateDirty = true;
        }

        internal static void Include(IntPtr taskbar, LocalPInvoke.RECT taskbarRect, IntPtr target)
        {
            lock (Sync)
            {
                Windows.RemoveWhere(w => !LocalPInvoke.IsWindow(w));
                foreach (var window in Windows)
                {
                    if (GetParent(window) != taskbar || !LocalPInvoke.IsWindowVisible(window)) continue;
                    LocalPInvoke.RECT rect;
                    if (!LocalPInvoke.GetWindowRect(window, out rect)) continue;
                    IntPtr copy = LocalPInvoke.CreateRectRgn(0, 0, 0, 0);
                    try
                    {
                        if (GetWindowRgn(window, copy) <= 0) continue;
                        OffsetRgn(copy, rect.Left - taskbarRect.Left, rect.Top - taskbarRect.Top);
                        LocalPInvoke.CombineRgn(target, target, copy, 2);
                    }
                    finally { LocalPInvoke.DeleteObject(copy); }
                }
            }
        }
    }
}
