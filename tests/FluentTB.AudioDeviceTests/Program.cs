using FluentFlyoutWPF.Classes;
using FluentFlyoutWPF.ViewModels;
using System.IO;
using System.Xml.Serialization;

namespace AudioDeviceTests;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        int checks = 0;
        void Check(bool value, string message)
        {
            if (!value) throw new Exception(message);
            checks++;
        }
        var serializer = new XmlSerializer(typeof(UserSettings));
        using var legacyXml = new StringReader("<UserSettings><TaskbarVisualizerEnabled>true</TaskbarVisualizerEnabled></UserSettings>");
        var settings = (UserSettings)serializer.Deserialize(legacyXml)!;
        Check(settings.TaskbarVisualizerDeviceId == null, "Old settings must follow Windows default.");
        Check(settings.TaskbarVisualizerEnabled, "Existing enabled state must remain intact.");
        foreach (string? id in new[] { "{0.0.0.00000000}.{endpoint-test-id}", null })
        {
            settings.TaskbarVisualizerDeviceId = id;
            using var writer = new StringWriter();
            serializer.Serialize(writer, settings);
            using var reader = new StringReader(writer.ToString());
            var restored = (UserSettings)serializer.Deserialize(reader)!;
            Check(restored.TaskbarVisualizerDeviceId == id, "Device selection did not survive XML round-trip.");
        }
        // Read-only endpoint enumeration: no capture and no Windows audio routing changes.
        var devices = AudioDeviceMonitor.GetActiveRenderDevices();
        Check(devices.All(device => !string.IsNullOrWhiteSpace(device.Id) && !string.IsNullOrWhiteSpace(device.Name)), "An output has no ID or label.");
        Check(devices.Select(device => device.Id).Distinct().Count() == devices.Count, "Duplicate endpoint IDs.");
        foreach (var device in devices)
        {
            using var endpoint = AudioDeviceMonitor.Instance.GetDeviceById(device.Id!);
            Check(endpoint != null && endpoint.ID == device.Id, "Cannot resolve an enumerated endpoint by its saved ID.");
        }
        using var missing = AudioDeviceMonitor.Instance.GetDeviceById("missing-test-output");
        Check(missing == null, "An unavailable selected endpoint must not silently become the default.");
        AudioDeviceMonitor.Instance.Dispose();
        Console.WriteLine($"PASS: {checks} audio-device checks; XML compatibility and {devices.Count} read-only output endpoints checked. No capture or routing change.");
    }
}
