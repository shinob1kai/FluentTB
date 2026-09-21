using System;

namespace FluentTB
{
    public partial class MainWindow
    {
        public void SetTaskbarShapeEnabled(bool enabled)
        {
            var settings = activeSettings.Clone();
            settings.TaskbarShapeEnabled = enabled;
            ApplyHostedSettings(settings);
        }

        public void ApplyHostedSettings(Types.Settings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            pendingSettings = settings.Clone();
            bool independent = settings.MarginBasic == -384;
            marginSlider.IsEnabled = marginInput.IsEnabled = !independent;
            mTopInput.IsEnabled = mLeftInput.IsEnabled = mBottomInput.IsEnabled = mRightInput.IsEnabled = independent;
            marginInput.Text = independent ? "Advanced" : settings.MarginBasic.ToString();
            mTopInput.Text = settings.MarginTop.ToString();
            mBottomInput.Text = settings.MarginBottom.ToString();
            mLeftInput.Text = settings.MarginLeft.ToString();
            mRightInput.Text = settings.MarginRight.ToString();
            cornerRadiusInput.Text = settings.CornerRadius.ToString();
            dynamicCheckBox.IsChecked = settings.IsDynamic;
            showTrayCheckBox.IsChecked = settings.ShowTray;
            showTrayOnHoverCheckBox.IsChecked = settings.ShowTrayOnHover;
            fillMaximisedCheckBox.IsChecked = settings.FillOnMaximise;
            fillAltTabCheckBox.IsChecked = settings.FillOnTaskSwitch;
            compositionFixCheckBox.IsChecked = settings.CompositionCompat;
            widgetsCheckBox.IsChecked = settings.ShowWidgets;
            ApplyButton_Click(null, null);
        }
    }
}
