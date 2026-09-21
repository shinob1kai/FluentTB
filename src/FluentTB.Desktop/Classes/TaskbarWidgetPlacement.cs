namespace FluentFlyoutWPF.Classes;

internal static class TaskbarWidgetPlacement
{
    // 0 = start, 1 = middle, 2 = end. Keep manual positioning for vertical taskbars.
    internal static int Resolve(bool automatic, bool centered, int manualPosition, bool vertical)
        => automatic && !vertical ? (centered ? 0 : 2) : manualPosition;
}
