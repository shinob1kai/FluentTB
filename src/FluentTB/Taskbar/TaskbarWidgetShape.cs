using System;

namespace FluentTB
{
    // Physical pixels, using the same rounding and GDI ellipse diameter as Taskbar.
    public readonly struct TaskbarWidgetShape
    {
        public int Top { get; }
        public int Height { get; }
        public int CornerDiameter { get; }

        public TaskbarWidgetShape(Types.Settings settings, double scale, int crossSize, bool vertical = false)
        {
            if (scale <= 0 || double.IsNaN(scale) || double.IsInfinity(scale))
                throw new ArgumentOutOfRangeException(nameof(scale));
            int leading = settings.MarginBasic == -384
                ? (vertical ? settings.MarginLeft : settings.MarginTop) : settings.MarginBasic;
            int trailing = settings.MarginBasic == -384
                ? (vertical ? settings.MarginRight : settings.MarginBottom) : settings.MarginBasic;
            Top = Math.Clamp(Convert.ToInt32(leading * scale), 0, Math.Max(0, crossSize));
            int bottom = Math.Clamp(Convert.ToInt32(crossSize - trailing * scale), Top, Math.Max(Top, crossSize));
            Height = bottom - Top;
            CornerDiameter = Math.Clamp(Convert.ToInt32(settings.CornerRadius * scale), 0, Height);
        }
    }
}
