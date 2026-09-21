// Copyright (c) 2024-2026 The FluentFlyout Authors
// SPDX-License-Identifier: GPL-3.0-or-later

using FluentFlyout.Classes.Settings;
using System.Windows.Controls;

namespace FluentFlyoutWPF.Pages;

public partial class MediaFlyoutPage : Page
{
    private bool populatingMonitors;
    private void PopulateMonitors()
    {
        populatingMonitors = true;
        try
        {
            MediaMonitorComboBox.Items.Clear();
            MediaMonitorComboBox.Items.Add(TryFindResource("DefaultLockLayoutPosition"));
            var monitors = FluentFlyoutWPF.Classes.Utils.MonitorUtil.GetMonitors();
            for (int i = 0; i < monitors.Count; i++)
                MediaMonitorComboBox.Items.Add($"{i + 1} ({monitors[i].deviceName})");
            var selected = SettingsManager.Current.MediaFlyoutSelectedMonitor;
            MediaMonitorComboBox.SelectedIndex = selected is >= 0 && selected < monitors.Count ? selected.Value + 1 : 0;
        }
        finally { populatingMonitors = false; }
    }
    private void MediaMonitor_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (populatingMonitors || MediaMonitorComboBox.SelectedIndex < 0) return;
        SettingsManager.Current.MediaFlyoutSelectedMonitor = MediaMonitorComboBox.SelectedIndex == 0 ? null : MediaMonitorComboBox.SelectedIndex - 1;
    }

    public MediaFlyoutPage()
    {
        InitializeComponent();
        DataContext = SettingsManager.Current;
        Loaded += (_, _) => PopulateMonitors();
    }
}