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
    internal const string ReleasesUrl = "https://github.com/shinob1kai/FluentTB/tree/Dev-Edition";
    internal static void OpenReleases() => Process.Start(new ProcessStartInfo(ReleasesUrl) { UseShellExecute = true });

    internal static Task<string> CheckAsync(bool german) => Task.FromResult(german
        ? "Dev-Edition: Neue Stände sind als Quellcode im Dev-Edition-Branch verfügbar. Bitte selbst kompilieren."
        : "Dev edition: updates are available as source in the Dev-Edition branch. Please build locally.");
}
