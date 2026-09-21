using System.Windows.Interop;
using FluentFlyout.Classes.Settings;

namespace FluentTB.Integration;

internal static class TaskbarIntegration
{
    private static FluentTB.MainWindow? engine;
    internal static FluentTB.MainWindow Engine
    {
        get
        {
            if (engine == null)
            {
                engine = new FluentTB.MainWindow(hosted: true);
                new WindowInteropHelper(engine).EnsureHandle();
            }
            return engine;
        }
    }

    internal static void Start()
    {
        _ = Engine;
        var flyouts = SettingsManager.Current;
        if (flyouts.FluentTBMigrationVersion < 1)
        {
            var old = Engine.activeSettings;
            flyouts.TaskbarWidgetEnabled = old.MusicWidgetEnabled;
            flyouts.TaskbarWidgetControlsEnabled = old.MusicWidgetControls;
            flyouts.TaskbarWidgetHideCompletely = old.MusicWidgetHideWhenIdle;
            flyouts.FluentTBMigrationVersion = 1;
            SettingsManager.SaveSettings();
        }
    }

    internal static void Stop()
    {
        if (engine == null) return;
        engine.shouldReallyDieNoReally = true;
        engine.Close();
        engine = null;
    }
}
