using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace FluentTB.Release;

internal static class AppRelease
{
    internal static string Version => Assembly.GetEntryAssembly()!.GetName().Version!.ToString(4);
    internal static string Configuration => Assembly.GetEntryAssembly()!.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration ?? "Release";
    internal const string ReleasesUrl = "https://github.com/shinob1kai/FluentTB/releases";
    internal static void OpenReleases() => Process.Start(new ProcessStartInfo(ReleasesUrl) { UseShellExecute = true });

    // An unsuccessful request must never be reported as an up-to-date installation.
    internal static async Task<string> CheckAsync(bool german)
    {
        try
        {
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("FluentTB/" + Version);
            using var response = await client.GetAsync("https://api.github.com/repos/shinob1kai/FluentTB/releases/latest");
            if (response.StatusCode == HttpStatusCode.NotFound)
                return german ? "Keine öffentliche Veröffentlichung gefunden." : "No public release found.";
            response.EnsureSuccessStatusCode();
            using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            string tag = json.RootElement.GetProperty("tag_name").GetString() ?? "";
            if (!System.Version.TryParse(tag.TrimStart('v', 'V'), out var available))
                return german ? "Veröffentlichte Version konnte nicht verglichen werden." : "Unable to compare the published version.";
            var normalized = new System.Version(available.Major, available.Minor, Math.Max(0, available.Build), Math.Max(0, available.Revision));
            return normalized > System.Version.Parse(Version)
                ? (german ? "Neue öffentliche Version: " : "New public version: ") + tag
                : (german ? "Keine neuere öffentliche Version verfügbar." : "No newer public version available.");
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException or KeyNotFoundException)
        {
            return german ? "Aktualisierung konnte nicht geprüft werden. Bitte später erneut versuchen." : "Unable to check for updates. Please try again later.";
        }
    }
}
