// Copyright (c) 2024-2026 The FluentFlyout Authors
// SPDX-License-Identifier: GPL-3.0-or-later

using FluentFlyoutWPF.Classes.Clients;
using NLog;
using System.Net.Http;
using System.Text.Json;

namespace FluentFlyoutWPF.Classes.Services;

/// <summary>
/// Handles checking for application updates from the API
/// </summary>
public static class UpdateCheckerService
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();
    private const string ApiEndpoint = "newest-version";

    /// <summary>
    /// Result of an update check
    /// </summary>
    public class UpdateCheckResult
    {
        public bool IsUpdateAvailable { get; set; }
        public string NewestVersion { get; set; } = string.Empty;
        public string UpdateUrl { get; set; } = string.Empty;
        public DateTime CheckedAt { get; set; }
        public bool Success { get; set; }
    }

    /// <summary>
    /// Check for updates from the API
    /// </summary>
    /// <param name="currentVersion">The current app version (e.g., "v2.5.0")</param>
    /// <returns>UpdateCheckResult with update information</returns>
    // The merged application must never install the standalone upstream binary.
    public static Task<UpdateCheckResult> CheckForUpdatesAsync(string currentVersion)
    {
        return Task.FromResult(new UpdateCheckResult { Success = false, CheckedAt = DateTime.Now });
    }

    public static void OpenUpdateUrl(string url)
    {
        if (string.IsNullOrEmpty(url)) return;

        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Failed to open update URL");
        }
    }

    private static bool IsNewerVersion(string currentVersion, string newestVersion)
    {
        try
        {
            var current = Version.Parse(currentVersion.TrimStart('v'));
            var newest = Version.Parse(newestVersion.TrimStart('v'));
            return newest > current;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, $"Failed to compare versions: {currentVersion} vs {newestVersion}");
            return false;
        }
    }
}