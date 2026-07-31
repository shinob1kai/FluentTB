namespace FluentTB
{
    /// <summary>
    /// Thin alias so AutoHideManager (ported from old FluentTB code) can use
    /// NativeMethods.RECT / NativeMethods.POINT without duplicating the structs.
    /// All real P/Invoke declarations live in LocalPInvoke.cs.
    /// </summary>
    internal static class NativeMethods
    {
        // Forward struct aliases
        public struct RECT
        {
            public int Left, Top, Right, Bottom;
        }

        public struct POINT
        {
            public int x, y;
        }
    }
}
