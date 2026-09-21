using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using FluentTB.Integration;

namespace FluentFlyoutWPF.Pages;

public partial class FluentTaskbarPage : Page, INotifyPropertyChanged
{
    public FluentTB.Types.Settings Settings { get; }
    public bool ShapeEnabled
    {
        get => Settings.TaskbarShapeEnabled;
        set
        {
            if (Settings.TaskbarShapeEnabled == value) return;
            TaskbarIntegration.Engine.SetTaskbarShapeEnabled(value);
            Settings.TaskbarShapeEnabled = value;
            PropertyChanged?.Invoke(this, new(nameof(ShapeEnabled)));
        }
    }
    private int margin;
    public double MarginValue { get => margin; set { margin = (int)Math.Round(value); PropertyChanged?.Invoke(this, new(nameof(MarginValue))); } }
    public double Radius { get => Settings.CornerRadius; set { Settings.CornerRadius = (int)Math.Round(value); PropertyChanged?.Invoke(this, new(nameof(Radius))); } }
    public bool IndependentMargins { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;

    public FluentTaskbarPage()
    {
        Settings = TaskbarIntegration.Engine.activeSettings.Clone();
        IndependentMargins = Settings.MarginBasic == -384;
        margin = IndependentMargins ? Settings.MarginTop : Settings.MarginBasic;
        InitializeComponent();
        DataContext = this;
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        Settings.MarginBasic = IndependentMargins ? -384 : margin;
        TaskbarIntegration.Engine.ApplyHostedSettings(Settings);
        SaveStatus.SetResourceReference(TextBlock.TextProperty, "FtbSaved");
    }
}
