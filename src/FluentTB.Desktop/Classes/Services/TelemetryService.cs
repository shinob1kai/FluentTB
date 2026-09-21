// Copyright (c) 2024-2026 The FluentFlyout Authors
// SPDX-License-Identifier: GPL-3.0-or-later
// FluentTB integration: upstream telemetry is disabled in this independent fork.
namespace FluentFlyoutWPF.Classes.Services;
public static class TelemetryService
{
    public static Task SendTelemetryEventAsync(string eventName, string? experimentId = null) => Task.CompletedTask;
}