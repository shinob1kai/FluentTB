using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace FluentTB
{
    /// <summary>
    /// Experimental click-through shadow overlay for the floating taskbar region.
    /// </summary>
    public partial class TaskbarEffect : Window
    {
        public TaskbarEffect()
        {
            InitializeComponent();
            Show();
            Hide();
        }

        public void UpdateShadow(LocalPInvoke.RECT taskbarRect, Types.EffectiveRegion region)
        {
            if (region == null || region.Width <= 0 || region.Height <= 0)
            {
                Hide();
                return;
            }

            const int padding = 28;
            Left = taskbarRect.Left + region.Left - padding;
            Top = taskbarRect.Top + region.Top - padding;
            Width = region.Width + (padding * 2);
            Height = region.Height + (padding * 2);

            RootCanvas.Width = Width;
            RootCanvas.Height = Height;

            ShadowHost.Width = region.Width;
            ShadowHost.Height = region.Height;
            ShadowHost.CornerRadius = new CornerRadius(Math.Max(0, region.CornerRadius / 2.0));
            System.Windows.Controls.Canvas.SetLeft(ShadowHost, padding);
            System.Windows.Controls.Canvas.SetTop(ShadowHost, padding);

            if (!IsVisible)
                Show();
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            uint style = LocalPInvoke.GetWindowLong(hwnd, LocalPInvoke.GWL_EXSTYLE);
            style |= LocalPInvoke.WS_EX_TRANSPARENT |
                     LocalPInvoke.WS_EX_TOOLWINDOW |
                     LocalPInvoke.WS_EX_NOACTIVATE;
            LocalPInvoke.SetWindowLong(hwnd, LocalPInvoke.GWL_EXSTYLE, style);
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            ShadowHost.Effect = null;
        }
    }
}
