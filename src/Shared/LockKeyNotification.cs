#nullable enable
namespace FluentTB.Input;

// Windows resolves dedicated Insert and NumPad Insert to VK_INSERT.
// VK_NUMPAD0 / VK_DELETE must not be interpreted as Insert.
internal static class LockKeyNotification
{
    internal static string? Resolve(int key, bool caps, bool num, bool scroll, bool insert) => key switch
    {
        0x14 when caps => "CapsLock",
        0x90 when num => "NumLock",
        0x91 when scroll => "ScrollLock",
        0x2D when insert => "Insert",
        _ => null
    };

    // Insert reports a press, not an application-specific overwrite state.
    internal static bool IndicatorActive(string key, bool toggled) => key == "Insert" || toggled;
}
