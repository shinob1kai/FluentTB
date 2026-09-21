// Adapted from FluentFlyout's lock-key dispatch. Copyright (c) 2024-2026 The FluentFlyout Authors.
// SPDX-License-Identifier: GPL-3.0-or-later
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
namespace FluentTB.Public;
internal sealed class KeyboardHook : IDisposable
{
    private delegate nint HookProc(int code, nint message, nint data);
    private readonly HookProc callback;
    private nint handle;
    public KeyboardHook(Action<string, bool> show)
    {
        callback = (code, message, data) =>
        {
            if (code >= 0 && (message == 0x101 || message == 0x105) && App.Settings.Enabled)
            {
                var vk = Marshal.ReadInt32(data);
                var name = FluentTB.Input.LockKeyNotification.Resolve(vk, App.Settings.Caps, App.Settings.Num, App.Settings.Scroll, App.Settings.Insert);
                if (name != null)
                    Application.Current.Dispatcher.BeginInvoke(() => show(name, FluentTB.Input.LockKeyNotification.IndicatorActive(name, (GetKeyState(vk) & 1) != 0)));
            }
            return CallNextHookEx(handle, code, message, data);
        };
        handle = SetWindowsHookEx(13, callback, GetModuleHandle(null), 0);
        if (handle == 0) throw new Win32Exception(Marshal.GetLastWin32Error());
    }
    public void Dispose() { if (handle != 0) { UnhookWindowsHookEx(handle); handle = 0; } }
    [DllImport("user32.dll", SetLastError = true)] private static extern nint SetWindowsHookEx(int id, HookProc callback, nint module, uint thread);
    [DllImport("user32.dll")] private static extern bool UnhookWindowsHookEx(nint handle);
    [DllImport("user32.dll")] private static extern nint CallNextHookEx(nint handle, int code, nint message, nint data);
    [DllImport("user32.dll")] private static extern short GetKeyState(int key);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] private static extern nint GetModuleHandle(string? module);
}
