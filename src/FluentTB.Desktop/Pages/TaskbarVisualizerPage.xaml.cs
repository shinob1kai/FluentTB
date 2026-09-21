// Copyright (c) 2024-2026 The FluentFlyout Authors
// SPDX-License-Identifier: GPL-3.0-or-later

using FluentFlyout.Classes.Settings;
using FluentFlyoutWPF.Classes;
using System.Windows;
using System.Windows.Controls;

namespace FluentFlyoutWPF.Pages;

public partial class TaskbarVisualizerPage : Page
{
    private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();
    private bool _populatingDevices;
    private int _deviceRequest;

    public TaskbarVisualizerPage()
    {
        InitializeComponent();
        DataContext = SettingsManager.Current;
        Loaded += async (_, _) => await RefreshDevicesAsync();
        Unloaded += (_, _) => _deviceRequest++;
    }

    private async Task RefreshDevicesAsync()
    {
        int request = ++_deviceRequest;
        IReadOnlyList<AudioOutputDevice> devices;
        try { devices = await Task.Run(AudioDeviceMonitor.GetActiveRenderDevices); }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Could not enumerate visualization output devices");
            devices = Array.Empty<AudioOutputDevice>();
        }
        if (!IsLoaded || request != _deviceRequest) return;

        string? selectedId = SettingsManager.Current.TaskbarVisualizerDeviceId;
        var choices = new List<AudioOutputDevice>
        {
            new(null, (string)FindResource("VisualizerDefaultDevice"))
        };
        choices.AddRange(devices);
        if (!string.IsNullOrWhiteSpace(selectedId) && !choices.Any(device => device.Id == selectedId))
            choices.Add(new(selectedId, (string)FindResource("VisualizerUnavailableDevice")));
        _populatingDevices = true;
        try
        {
            OutputDeviceComboBox.ItemsSource = choices;
            OutputDeviceComboBox.SelectedItem = choices.FirstOrDefault(device => device.Id == selectedId) ?? choices[0];
        }
        finally { _populatingDevices = false; }
    }

    private async void OutputDevice_DropDownOpened(object? sender, EventArgs e) => await RefreshDevicesAsync();

    private void OutputDevice_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_populatingDevices || OutputDeviceComboBox.SelectedItem is not AudioOutputDevice device) return;
        SettingsManager.Current.TaskbarVisualizerDeviceId = device.Id;
        SettingsManager.SaveSettings();
    }
}
