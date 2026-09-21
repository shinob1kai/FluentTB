// Copyright (c) 2024-2026 The FluentFlyout Authors
// SPDX-License-Identifier: GPL-3.0-or-later

using FluentFlyout.Classes.Settings;
using FluentFlyoutWPF.Classes;
using System.Windows.Controls;

namespace FluentFlyout.Controls;

// Rendered inside TaskbarWidgetControl: no independent background, hit area or position.
public partial class TaskbarVisualizerControl : UserControl
{
    private static readonly Visualizer visualizer = new();
    private static bool mediaPlaying;

    public TaskbarVisualizerControl()
    {
        InitializeComponent();
        DataContext = SettingsManager.Current;
        VisualizerContainer.Source = visualizer.Bitmap;
    }

    public static void SetMediaPlaying(bool playing)
    {
        mediaPlaying = playing;
        RefreshCapture();
    }

    public static void RefreshCapture() => visualizer.SetEnabled(mediaPlaying &&
        SettingsManager.Current.TaskbarWidgetEnabled && SettingsManager.Current.TaskbarVisualizerEnabled);

    public static void OnTaskbarVisualizerEnabledChanged(bool value) => RefreshCapture();
    public static void RefreshDevice() => visualizer.RefreshDevice();
    public static void DisposeVisualizer() => visualizer.Dispose();
}
