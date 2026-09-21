using System;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;

namespace FluentTB.Tests
{
    internal static class Program
    {
        private static int checks;
        private static readonly BindingFlags PrivateStatic = BindingFlags.NonPublic | BindingFlags.Static;
        private static LocalPInvoke.RECT Rect(double l, double t, double r, double b) => new LocalPInvoke.RECT
        { Left = (int)l, Top = (int)t, Right = (int)r, Bottom = (int)b };

        [STAThread]
        private static int Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--snapshot")
            {
                typeof(TaskbarSnapshot).GetMethod("Main", PrivateStatic).Invoke(null, null);
                return 0;
            }
            try
            {
                // A completely fresh profile: no FluentTB directory or settings/log files.
                string storageRoot = Path.Combine(Path.GetTempPath(), "FluentTB-first-run-" + Guid.NewGuid());
                foreach (string profile in new[] { "Local", "Roaming" })
                {
                    string config = Path.Combine(storageRoot, profile, "FluentTB", "fluent-tb.json");
                    string log = Path.Combine(storageRoot, profile, "FluentTB", "fluent-tb.log");
                    Assert(!Directory.Exists(Path.GetDirectoryName(config)), "Fresh profile starts with no app directory");
                    bool oldStartupFailed = false;
                    try { using (File.Create(log)) { } }
                    catch (DirectoryNotFoundException) { oldStartupFailed = true; }
                    Assert(oldStartupFailed, "Reproduce original first-run log failure");
                    var defaults = SettingsStorage.Initialize(config, log);
                    Assert(File.Exists(config) && File.Exists(log), "First startup creates both files and parent directory");
                    var saved = JsonConvert.DeserializeObject<Types.Settings>(File.ReadAllText(config));
                    Assert(defaults != null && saved.MarginBasic == 3 && saved.CornerRadius == 7, "First-run defaults are persisted");
                    const string existing = "{\"MarginBasic\":9,\"TaskbarShapeEnabled\":false,\"futureSetting\":true}";
                    File.WriteAllText(config, existing);
                    Assert(SettingsStorage.Initialize(config, log) == null && File.ReadAllText(config) == existing, "Existing settings remain byte-for-byte unchanged");
                    File.WriteAllText(config, "  ");
                    Assert(SettingsStorage.Initialize(config, log) != null, "Empty settings recover defaults");
                    using (File.Open(log, FileMode.Open, FileAccess.ReadWrite, FileShare.None))
                        Assert(SettingsStorage.Initialize(config, log) == null, "Locked diagnostic log cannot prevent startup");
                }
                foreach (var key in new[] { 0x14, 0x90, 0x91, 0x2D })
                {
                    Assert(FluentTB.Input.LockKeyNotification.Resolve(key, true, true, true, true) != null, "Recognized lock/Insert key");
                    Assert(FluentTB.Input.LockKeyNotification.Resolve(key, false, false, false, false) == null, "Disabled key stays silent");
                }
                foreach (var key in new[] { 0x60, 0x2E, 0x6E, 0x41 })
                    Assert(FluentTB.Input.LockKeyNotification.Resolve(key, true, true, true, true) == null, "Digits/Delete/decimal/letters stay silent");
                Assert(FluentTB.Input.LockKeyNotification.Resolve(0x2D, false, false, false, true) == "Insert", "Insert independent of other switches");
                Assert(FluentTB.Input.LockKeyNotification.Resolve(0x90, false, true, false, false) == "NumLock", "NumLock independent of Insert switch");
                Assert(FluentTB.Input.LockKeyNotification.IndicatorActive("Insert", false), "Insert press has active indicator without a toggle state");
                Assert(!FluentTB.Input.LockKeyNotification.IndicatorActive("NumLock", false), "NumLock off");
                Assert(FluentTB.Input.LockKeyNotification.IndicatorActive("NumLock", true), "NumLock on");
                var assembly = typeof(Types).Assembly;
                foreach (double dpi in new[] { 1.0, 1.5, 2.0 })
                {
                    var shape = new TaskbarWidgetShape(new Types.Settings { MarginBasic = 3, CornerRadius = 7 }, dpi, (int)(48 * dpi));
                    Assert(shape.Top == Convert.ToInt32(3 * dpi) && shape.Height == Convert.ToInt32(45 * dpi) - Convert.ToInt32(3 * dpi), "Widget matches taskbar insets at " + dpi);
                    Assert(shape.CornerDiameter == Convert.ToInt32(7 * dpi), "Widget matches GDI corners at " + dpi);
                }
                var asymmetric = new Types.Settings { MarginBasic = -384, MarginTop = 2, MarginBottom = 8, MarginLeft = 4, MarginRight = 6, CornerRadius = 7 };
                var horizontal = new TaskbarWidgetShape(asymmetric, 1, 48);
                var vertical = new TaskbarWidgetShape(asymmetric, 1, 48, true);
                Assert(horizontal.Top == 2 && horizontal.Height == 38 && vertical.Top == 4 && vertical.Height == 38, "Widget independent margins and orientation");
                Assert(new TaskbarWidgetShape(new Types.Settings { MarginBasic = 40 }, 1, 48).Height == 0, "No inverted widget with excessive margins");
                var bounds = assembly.GetType("FluentTB.Taskbar").GetMethod("TryGetDynamicAppBounds", PrivateStatic);
                foreach (int origin in new[] { -2560, 0, 1920 })
                foreach (double scale in new[] { 1.0, 1.5, 2.0 })
                foreach (bool centred in new[] { false, true })
                foreach (bool tray in new[] { false, true })
                {
                    double left = (centred ? 750 : 0) * scale;
                    double right = (centred ? 1100 : 450) * scale;
                    var bar = new Types.Taskbar
                    {
                        ScaleFactor = scale, TaskbarRect = Rect(origin, 1000, origin + 1920 * scale, 1000 + 48 * scale),
                        AppListRect = Rect(origin + left, 1000, origin + right, 1000 + 48 * scale),
                        TrayRect = tray ? Rect(origin + 1700 * scale, 1000, origin + 1920 * scale, 1000 + 48 * scale) : default
                    };
                    object[] values = { bar, new Types.Settings { MarginBasic = 3, IsCentred = centred }, 0d, 0d };
                    Assert((bool)bounds.Invoke(null, values) && (double)values[2] == (centred ? left - 3 * scale : 3 * scale) &&
                        (double)values[3] == right + 3 * scale, "Geometry " + origin + "/" + scale + "/" + centred + "/" + tray);
                }
                for (int invalid = 0; invalid < 6; invalid++)
                {
                    var bar = new Types.Taskbar { ScaleFactor = 1, TaskbarRect = Rect(0, 1000, 1920, 1048),
                        AppListRect = Rect(750, 1000, 1100, 1048), TrayRect = Rect(1700, 1000, 1920, 1048) };
                    switch (invalid)
                    {
                        case 0: bar.AppListRect = default; break;
                        case 1: bar.AppListRect = Rect(-1, 1000, 1100, 1048); break;
                        case 2: bar.AppListRect = Rect(750, 1000, 2000, 1048); break;
                        case 3: bar.AppListRect = Rect(750, 950, 1100, 998); break;
                        case 4: bar.AppListRect = Rect(750, 1000, 1800, 1048); break;
                        case 5: bar.ScaleFactor = double.NaN; break;
                    }
                    Assert(!(bool)bounds.Invoke(null, new object[] { bar, new Types.Settings { MarginBasic = 3, IsCentred = true }, 0d, 0d }), "Invalid geometry " + invalid);
                }
                var legacy = JsonConvert.DeserializeObject<Types.Settings>("{\"MarginBasic\":3,\"CornerRadius\":7,\"ShowWidgets\":false}");
                Assert(legacy.MarginBasic == 3 && legacy.CornerRadius == 7 && !legacy.ShowWidgets && !legacy.MusicWidgetEnabled, "Legacy settings");
                Assert(legacy.TaskbarShapeEnabled, "Old settings retain taskbar shaping");
                legacy.TaskbarShapeEnabled = false;
                legacy.MusicWidgetEnabled = true;
                legacy.MusicWidgetControls = false;
                legacy.MusicWidgetHideWhenIdle = false;
                legacy.MusicWidgetAllMonitors = false;
                var copies = new[] { legacy.Clone(), (Types.Settings)typeof(MainWindow).GetMethod("CopySettings", PrivateStatic).Invoke(null, new object[] { legacy }),
                    JsonConvert.DeserializeObject<Types.Settings>(JsonConvert.SerializeObject(legacy)) };
                foreach (var copy in copies)
                    foreach (var property in typeof(Types.Settings).GetProperties())
                        Assert(Equals(property.GetValue(copy), property.GetValue(legacy)), "Preserve " + property.Name);
                var original = new Types.Taskbar { WidgetsRect = Rect(-1900, 970, -1748, 1018) };
                var cloned = (Types.Taskbar)assembly.GetType("FluentTB.Background").GetMethod("CloneTaskbar", PrivateStatic).Invoke(null, new object[] { original });
                Assert(cloned.WidgetsRect.Left == -1900 && cloned.WidgetsRect.Right == -1748, "Widget coordinates on secondary monitor");
                Console.WriteLine("PASS: " + checks + " assertions (.NET 10 geometry, migration, copying and JSON persistence).");
                return 0;
            }
            catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        }
        private static void Assert(bool condition, string name)
        {
            checks++;
            if (!condition) throw new InvalidOperationException(name);
        }
    }
}
