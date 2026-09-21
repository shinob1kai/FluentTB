using System.Windows.Interop;
namespace FluentTB.Integration;
internal static class TaskbarIntegration
{
    private static FluentTB.MainWindow? engine;
    internal static FluentTB.MainWindow Engine => engine ?? throw new InvalidOperationException("Taskbar engine has not started.");
    internal static void Start() { engine = new FluentTB.MainWindow(hosted: true); new WindowInteropHelper(engine).EnsureHandle(); }
    internal static void Stop() { if (engine == null) return; engine.shouldReallyDieNoReally = true; engine.Close(); engine = null; }
}
