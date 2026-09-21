using System.Text.Json;
namespace FluentTB.Public;
public sealed class PublicSettings
{
    public string Language { get; set; } = "system";
    public bool Enabled { get; set; } = true;
    public bool Caps { get; set; } = true;
    public bool Num { get; set; } = true;
    public bool Scroll { get; set; } = true;
    public bool Insert { get; set; } = true;
    public bool Animated { get; set; } = true;
    public bool Bold { get; set; } = true;
    public int Monitor { get; set; } = 1;
    public double Duration { get; set; } = 2000;
    private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FluentTB", "public.json");
    public static PublicSettings Load()
    {
        try { return JsonSerializer.Deserialize<PublicSettings>(File.ReadAllText(SettingsPath)) ?? new(); }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException) { return new(); }
    }
    public void Save()
    {
        Duration = double.IsFinite(Duration) ? Math.Clamp(Duration, 250, 15000) : 2000;
        Monitor = Math.Clamp(Monitor, 0, 2);
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        var temporary = SettingsPath + ".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        File.Move(temporary, SettingsPath, true);
    }
}
